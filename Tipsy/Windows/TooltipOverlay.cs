using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Tipsy.Core.Layout;
using Tipsy.Game;

namespace Tipsy.Windows;

/// <summary>The styled tooltip: a fixed-width, input-free window pinned to an anchor, drawn from the reader's snapshot.</summary>
public sealed class TooltipOverlay : Window
{
    private const ImGuiWindowFlags LiveFlags = ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.AlwaysAutoResize;
    private const ImGuiWindowFlags PlacingFlags = ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.AlwaysAutoResize;

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
    private bool fitting;
    private IReadOnlyList<TooltipBlock> shown = [];
    private IReadOnlyList<TooltipBlock> lastBlocks = [];
    private float lastContentHeight;
    private WindowPlacement placement;
    private int pushedStyles;
    private int pushedColors;

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

    public override bool DrawConditions()
    {
        selector.Update();
        if (selector.Blocks.Count > 0)
            lastBlocks = selector.Blocks;

        showingSample = false;
        fitting = false;
        if (placing)
        {
            shown = lastBlocks.Count > 0 ? lastBlocks : SampleTooltip.Blocks;
            return true;
        }

        if (configuration.ReplaceTooltips && selector.Drawn is { } drawn && !HideKeyHeld())
        {
            shown = selector.Blocks;
            fitting = drawn.FitToContent;
            return true;
        }

        if (SampleBeside is null)
            return false;
        showingSample = true;
        shown = SampleTooltip.Blocks;
        return true;
    }

    public override void PreDraw()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var width = fitting ? renderer.FittedWidth(shown, configuration.Width, baseTokens.Padding, scale) : configuration.Width;
        tokens = (baseTokens with { Width = width }).Scaled(scale);
        theme = configuration.Theme();
        var viewport = ImGui.GetMainViewport();
        var workMin = viewport.WorkPos;
        var workMax = viewport.WorkPos + viewport.WorkSize;
        if (showingSample && SampleBeside is var (settingsMin, settingsMax))
        {
            var right = new Vector2(settingsMax.X + tokens.SectionGap, settingsMin.Y);
            var left = new Vector2(settingsMin.X - tokens.SectionGap - tokens.Width, settingsMin.Y);
            var beside = right.X + tokens.Width <= workMax.X - tokens.ViewportInset ? right : left;
            placement = Placement.Place(AnchorPreset.Custom, workMin, workMax, tokens.ViewportInset, tokens.Width, lastContentHeight, beside, Vector2.Zero, Vector2.Zero);
        }
        else
        {
            var anchor = placing ? AnchorPreset.Custom : configuration.Anchor;
            placement = Placement.Place(anchor, workMin, workMax, tokens.ViewportInset, tokens.Width, lastContentHeight, configuration.CustomPosition, ImGui.GetMousePos(), configuration.CursorOffset * scale);
        }

        if (!placing || placingStarted)
            ImGui.SetNextWindowPos(placement.Position, ImGuiCond.Always, placement.Pivot);
        placingStarted = false;
        ImGui.SetNextWindowSizeConstraints(new Vector2(tokens.Width, 0), new Vector2(tokens.Width, placement.MaxHeight));

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(tokens.Padding));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, theme.Rounding * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(tokens.InlineGap, 0));
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, Vector2.Zero);
        pushedStyles = 5;
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Rgb.ToVector4(theme.Surface, theme.SurfaceAlpha));
        ImGui.PushStyleColor(ImGuiCol.Border, Rgb.ToVector4(theme.Border));
        ImGui.PushStyleColor(ImGuiCol.Text, Rgb.ToVector4(theme.PrimaryText));
        pushedColors = 3;
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor(pushedColors);
        ImGui.PopStyleVar(pushedStyles);
    }

    public override void Draw()
    {
        ImGuiP.BringWindowToDisplayFront(ImGuiP.GetCurrentWindow());
        renderer.Draw(shown, tokens, theme);
        lastContentHeight = ImGui.GetCursorPosY() + tokens.Padding;
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
