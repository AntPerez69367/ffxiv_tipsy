using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Tipsy.Core.Layout;
using Tipsy.Game;

namespace Tipsy.Windows;

/// <summary>The styled tooltip: a fixed-width, input-free window pinned to an anchor, drawn from the reader's snapshot.</summary>
public sealed class TooltipOverlay : Window
{
    private const ImGuiWindowFlags LiveFlags = ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav;
    private const long SwapTimeoutMs = 150;
    private const ImGuiWindowFlags PlacingFlags = ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoNav;

    private readonly TooltipSelector selector;
    private readonly IKeyState keyState;
    private readonly TooltipBlockRenderer renderer;
    private readonly Configuration configuration;
    private readonly LayoutTokens baseTokens = new();

    private LayoutTokens tokens = new();
    private ThemeColors theme = ThemeColors.Minimal;
    private bool placing;
    private bool placingStarted;
    private Vector2? placedAt;
    private bool showingSample;
    private IReadOnlyList<TooltipBlock> lastBlocks = [];
    private IReadOnlyList<TooltipBlock> live = [];
    private bool liveFits;
    private long? waitingSince;
    private IReadOnlyList<TooltipBlock> visible = [];
    private bool visibleFits;
    private float visibleHeight;
    private IReadOnlyList<TooltipBlock> pending = [];
    private bool pendingFits;
    private IReadOnlyList<TooltipBlock> measured = [];
    private bool measuredFits;
    private float measuredHeight;
    private int measuredFrame = -1;
    private WindowPlacement placement;
    private ImRaii.StyleDisposable? styles;
    private ImRaii.ColorDisposable? colors;

    internal TooltipOverlay(TooltipSelector selector, IKeyState keyState, TooltipBlockRenderer renderer, Configuration configuration)
        : base("Tipsy tooltip##overlay", LiveFlags)
    {
        this.configuration = configuration;
        this.selector = selector;
        this.keyState = keyState;
        this.renderer = renderer;
        IsOpen = true;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
    }

    /// <summary>
    /// While true the window shows even without a tooltip, takes input, and saves where it is dragged to, both when the
    /// drag ends and when placing ends.
    /// </summary>
    public bool Placing
    {
        get => placing;
        set
        {
            if (placing && !value && placedAt is { } position && position != configuration.CustomPosition)
            {
                configuration.CustomPosition = position;
                configuration.Save();
            }

            placedAt = null;
            placing = value;
            placingStarted = value;
            Flags = value ? PlacingFlags : LiveFlags;
        }
    }

    /// <summary>
    /// The settings window's rectangle while it asks for the sample tooltip, or null. The sample is drawn beside that
    /// rectangle rather than at the tooltip's own position, and only while no real tooltip is showing.
    /// </summary>
    public (Vector2 Min, Vector2 Max)? SampleBeside { get; set; }

    /// <summary>
    /// Updates the selection and swaps the tooltip Tipsy draws over to it only once its images have loaded, or after
    /// <see cref="SwapTimeoutMs"/>, so a tooltip is never drawn with an icon missing while the previous one is still
    /// complete. When the game's tooltip closes, nothing is kept.
    /// </summary>
    public override void Update()
    {
        selector.Update();
        if (selector.Blocks.Count > 0)
            lastBlocks = selector.Blocks;
        if (selector.Drawn is not { } drawn)
        {
            live = [];
            waitingSince = null;
            return;
        }

        if (ReferenceEquals(selector.Blocks, live))
        {
            waitingSince = null;
            return;
        }

        var now = Environment.TickCount64;
        waitingSince ??= now;
        if (!renderer.ImagesReady(selector.Blocks) && now - waitingSince < SwapTimeoutMs)
            return;
        live = selector.Blocks;
        liveFits = drawn.FitToContent;
        waitingSince = null;
    }

    /// <summary>
    /// Picks the blocks to show. Blocks whose height is not known yet are measured first: the window keeps showing what it
    /// showed, or nothing, for one more frame while <see cref="Draw"/> lays the new blocks out invisibly, so the window
    /// takes its new size and position on the first frame the new blocks are seen. Blocks measured last frame are shown
    /// even when newer ones have arrived since, so content that changes every frame is never more than a frame behind.
    /// </summary>
    public override bool DrawConditions()
    {
        var (wanted, fits) = Wanted();
        pending = [];
        if (wanted.Count == 0)
        {
            if (visible.Count > 0)
            {
                measured = visible;
                measuredFits = visibleFits;
                measuredHeight = visibleHeight;
                measuredFrame = -1;
            }

            visible = [];
            return false;
        }

        if (ReferenceEquals(wanted, visible))
            return true;
        if (ReferenceEquals(wanted, measured))
        {
            visible = wanted;
            visibleFits = fits;
            visibleHeight = measuredHeight;
            return true;
        }

        if (measuredFrame == ImGui.GetFrameCount() - 1 && !ReferenceEquals(measured, visible))
        {
            visible = measured;
            visibleFits = measuredFits;
            visibleHeight = measuredHeight;
        }

        pending = wanted;
        pendingFits = fits;
        return true;
    }

    public override void PreDraw()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var measuringOnly = visible.Count == 0;
        tokens = measuringOnly ? TokensFor(pending, pendingFits) : TokensFor(visible, visibleFits);
        var height = measuringOnly ? 0 : visibleHeight;
        theme = configuration.Theme();
        var viewport = ImGui.GetMainViewport();
        var workMin = viewport.WorkPos;
        var workMax = viewport.WorkPos + viewport.WorkSize;
        if (showingSample && SampleBeside is var (settingsMin, settingsMax))
        {
            var right = new Vector2(settingsMax.X + tokens.SectionGap, settingsMin.Y);
            var left = new Vector2(settingsMin.X - tokens.SectionGap - tokens.Width, settingsMin.Y);
            var beside = right.X + tokens.Width <= workMax.X - tokens.ViewportInset ? right : left;
            placement = Placement.Place(AnchorPreset.Custom, workMin, workMax, tokens.ViewportInset, tokens.Width, height, beside, Vector2.Zero, Vector2.Zero);
        }
        else
        {
            var anchor = placing ? AnchorPreset.Custom : configuration.Anchor;
            placement = Placement.Place(anchor, workMin, workMax, tokens.ViewportInset, tokens.Width, height, configuration.CustomPosition, ImGui.GetMousePos(), configuration.CursorOffset * scale);
        }

        if (!placing || placingStarted)
            ImGui.SetNextWindowPos(placement.Position, ImGuiCond.Always, placement.Pivot);
        placingStarted = false;
        ImGui.SetNextWindowSize(new Vector2(tokens.Width, Math.Min(height, placement.MaxHeight)), ImGuiCond.Always);

        styles = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 0f, measuringOnly)
            .Push(ImGuiStyleVar.WindowPadding, new Vector2(tokens.Padding))
            .Push(ImGuiStyleVar.WindowRounding, theme.Rounding * scale)
            .Push(ImGuiStyleVar.WindowBorderSize, 1f)
            .Push(ImGuiStyleVar.ItemSpacing, new Vector2(tokens.InlineGap, 0))
            .Push(ImGuiStyleVar.CellPadding, Vector2.Zero);
        colors = ImRaii.PushColor(ImGuiCol.WindowBg, Rgb.ToVector4(theme.Surface, theme.SurfaceAlpha))
            .Push(ImGuiCol.Border, Rgb.ToVector4(theme.Border))
            .Push(ImGuiCol.Text, Rgb.ToVector4(theme.PrimaryText));
    }

    public override void PostDraw()
    {
        colors?.Dispose();
        styles?.Dispose();
    }

    public override void Draw()
    {
        ImGuiP.BringWindowToDisplayFront(ImGuiP.GetCurrentWindow());
        if (visible.Count > 0)
        {
            renderer.Draw(visible, tokens, theme);
            visibleHeight = ImGui.GetCursorPosY() + tokens.Padding;
        }

        if (pending.Count > 0)
            Measure();
        if (placing)
            placedAt = ImGui.GetWindowPos();
        if (placing && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && ImGui.GetWindowPos() != configuration.CustomPosition)
        {
            configuration.CustomPosition = ImGui.GetWindowPos();
            configuration.Save();
        }

        if (placement.Overflowing)
            DrawOverflowFade();
    }

    private (IReadOnlyList<TooltipBlock> Blocks, bool Fits) Wanted()
    {
        showingSample = false;
        if (placing)
            return (lastBlocks.Count > 0 ? lastBlocks : SampleTooltip.Blocks, false);
        if (configuration.ReplaceTooltips && selector.Drawn is not null && !HideKeyHeld())
            return (live, liveFits);
        if (SampleBeside is null)
            return ([], false);
        showingSample = true;
        return (SampleTooltip.Blocks, false);
    }

    private LayoutTokens TokensFor(IReadOnlyList<TooltipBlock> blocks, bool fits)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var width = fits ? renderer.FittedWidth(blocks, configuration.Width, baseTokens.Padding, scale) : configuration.Width;
        return (baseTokens with { Width = width }).Scaled(scale);
    }

    /// <summary>Lays <see cref="pending"/> out below what is shown, fully transparent, and records its window height.</summary>
    private void Measure()
    {
        var pendingTokens = TokensFor(pending, pendingFits);
        var top = ImGui.GetCursorPosY();
        using (ImRaii.PushId("measure"))
        using (ImRaii.PushStyle(ImGuiStyleVar.Alpha, 0f))
            renderer.Draw(pending, pendingTokens, theme);
        measured = pending;
        measuredFits = pendingFits;
        measuredHeight = ImGui.GetCursorPosY() - top + (2 * pendingTokens.Padding);
        measuredFrame = ImGui.GetFrameCount();
    }

    private bool HideKeyHeld()
    {
        var key = configuration.HideKey;
        return key != VirtualKey.NO_KEY && keyState.IsVirtualKeyValid(key) && keyState[key];
    }

    private void DrawOverflowFade()
    {
        var min = ImGui.GetWindowPos();
        var max = min + ImGui.GetWindowSize();
        var bottom = max.Y - tokens.Padding;
        var surface = ImGui.GetColorU32(Rgb.ToVector4(theme.Surface, theme.SurfaceAlpha));
        var clear = ImGui.GetColorU32(Rgb.ToVector4(theme.Surface, 0));
        ImGui.GetWindowDrawList().AddRectFilledMultiColor(new Vector2(min.X, bottom - tokens.OverflowFade), new Vector2(max.X, bottom), clear, clear, surface, surface);
    }
}
