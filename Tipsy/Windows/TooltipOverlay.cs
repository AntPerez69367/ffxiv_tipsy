using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Tipsy.Core.Layout;
using Tipsy.Core.Text;
using Tipsy.Core.Tooltips;
using Tipsy.Game;

namespace Tipsy.Windows;

/// <summary>The styled tooltip: a fixed-width, input-free window pinned to an anchor, drawn from the reader's snapshot.</summary>
public sealed unsafe partial class TooltipOverlay : Window
{
    private const string WidestValue = "+9999";
    private const string FlagSeparator = "   ";
    private const ImGuiWindowFlags LiveFlags = ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.AlwaysAutoResize;
    private const ImGuiWindowFlags PlacingFlags = ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.AlwaysAutoResize;

    private readonly IReadOnlyList<TooltipSource> sources;
    private readonly IGameGui gameGui;
    private readonly ITextureProvider textures;
    private readonly TooltipFonts fonts;
    private readonly ItemIcons itemIcons;
    private readonly Configuration configuration;
    private readonly LayoutTokens baseTokens = new();

    private LayoutTokens tokens = new();
    private ThemeColors theme = ThemeColors.Minimal;
    private bool placing;
    private bool placingStarted;
    private bool showingSample;
    private bool fitting;
    private IReadOnlyList<TooltipBlock> shown = [];
    private TooltipSnapshot? laidOut;
    private TooltipSnapshot? laidOutText;
    private List<TooltipBlock> blocks = [];
    private float lastContentHeight;
    private WindowPlacement placement;
    private int pushedStyles;
    private int pushedColors;

    internal TooltipOverlay(IReadOnlyList<TooltipSource> sources, IGameGui gameGui, ITextureProvider textures, TooltipFonts fonts, ItemIcons itemIcons, Configuration configuration)
        : base("Tipsy tooltip##overlay", LiveFlags)
    {
        this.configuration = configuration;
        this.sources = sources;
        this.gameGui = gameGui;
        this.textures = textures;
        this.fonts = fonts;
        this.itemIcons = itemIcons;
        IsOpen = true;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
    }

    /// <summary>While true the window shows even without a tooltip, takes input, and saves where it is dragged to.</summary>
    public bool Placing
    {
        get => placing;
        set
        {
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
        var source = OpenSource();
        var live = source is not null;
        var text = source is not null && source.Reader.Addon != TextTooltipMap.Addon ? OpenText() : null;
        if (source is not null && (!ReferenceEquals(source.Reader.Current, laidOut) || !ReferenceEquals(text, laidOutText)))
        {
            laidOut = source.Reader.Current;
            laidOutText = text;
            blocks = source.Layout(source.Reader.Current!);
            if (text is not null)
                blocks = SharedLayout.WithKeybind(blocks, text);
        }

        showingSample = false;
        fitting = false;
        if (placing)
        {
            shown = blocks.Count > 0 ? blocks : SampleTooltip.Blocks;
            return true;
        }

        if (configuration.ReplaceTooltips && live && blocks.Count > 0)
        {
            shown = blocks;
            fitting = source!.FitToContent;
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
        tokens = (baseTokens with { Width = fitting ? FittedWidth(scale) : configuration.Width }).Scaled(scale);
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
        ImGui.PushStyleColor(ImGuiCol.WindowBg, Colour(theme.Surface, theme.SurfaceAlpha));
        ImGui.PushStyleColor(ImGuiCol.Border, Colour(theme.Border));
        ImGui.PushStyleColor(ImGuiCol.Text, Colour(theme.PrimaryText));
        pushedColors = 3;
    }

    public override void PostDraw()
    {
        ImGui.PopStyleColor(pushedColors);
        ImGui.PopStyleVar(pushedStyles);
    }

    public override void Draw()
    {
        TooltipBlock? previous = null;
        foreach (var block in shown)
        {
            Gap(previous, block);
            DrawBlock(block);
            previous = block;
        }

        lastContentHeight = ImGui.GetCursorPosY() + tokens.Padding;
        if (placing && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && ImGui.GetWindowPos() != configuration.CustomPosition)
        {
            configuration.CustomPosition = ImGui.GetWindowPos();
            configuration.Save();
        }

        if (placement.Overflowing)
            DrawOverflowFade();
    }

    private TooltipSource? OpenSource() => sources.FirstOrDefault(IsShowing);

    private TooltipSnapshot? OpenText() =>
        sources.FirstOrDefault(source => source.Reader.Addon == TextTooltipMap.Addon && IsShowing(source))?.Reader.Current;

    private bool IsShowing(TooltipSource source)
    {
        var unit = (AtkUnitBase*)gameGui.GetAddonByName(source.Reader.Addon).Address;
        return unit != null && unit->IsVisible && source.Reader.Current is not null;
    }

    private float FittedWidth(float scale)
    {
        var widest = 0f;
        using (fonts.Body.Push())
        {
            foreach (var block in shown)
            {
                var text = block switch
                {
                    ParagraphBlock paragraph => SeStringText.Plain(paragraph.Text),
                    WarningBlock warning => warning.Text,
                    CaptionBlock caption => caption.Text,
                    _ => string.Empty,
                };
                foreach (var line in text.Split('\n'))
                    widest = Math.Max(widest, ImGui.CalcTextSize(line).X);
            }
        }

        return Math.Min(configuration.Width, MathF.Ceiling(widest / scale) + (2 * baseTokens.Padding) + 1);
    }

    /// <summary>The stat labels that would be clipped in the stat table at <paramref name="width"/> unscaled pixels.</summary>
    public List<string> ClippedStatLabels(IEnumerable<string> labels, float width)
    {
        var scaled = (baseTokens with { Width = width }).Scaled(ImGuiHelpers.GlobalScale);
        using (fonts.Body.Push())
        {
            var labelWidth = StatLabelWidth(scaled);
            return labels.Where(label => ImGui.CalcTextSize(label).X > labelWidth).ToList();
        }
    }

    private void Gap(TooltipBlock? previous, TooltipBlock block)
    {
        if (previous is null)
            return;
        var gap = block switch
        {
            DividerBlock => 0,
            _ when previous is DividerBlock => 0,
            _ when previous is CaptionBlock => tokens.CaptionGap,
            CaptionBlock or ParamsBlock => tokens.SectionGap,
            ParagraphBlock when previous is not ParagraphBlock => tokens.SectionGap,
            _ => tokens.RowGap,
        };
        if (gap > 0)
            ImGui.Dummy(new Vector2(0, gap));
    }

    private void DrawBlock(TooltipBlock block)
    {
        switch (block)
        {
            case HeaderBlock header:
                DrawHeader(header);
                break;
            case DividerBlock:
                DrawDivider();
                break;
            case ParamsBlock parameters:
                DrawParams(parameters);
                break;
            case CaptionBlock caption:
                using (fonts.Small.Push())
                    ImGui.TextColored(Colour(theme.Accent), caption.Text);
                break;
            case StatTableBlock table:
                DrawStats(table);
                break;
            case MateriaBlock materia:
                DrawMateria(materia);
                break;
            case BarBlock bar:
                DrawBar(bar);
                break;
            case KeyValueBlock row:
                DrawKeyValue(row);
                break;
            case ParagraphBlock paragraph:
                using (paragraph.Secondary ? fonts.Small.Push() : fonts.Body.Push())
                    SeString(paragraph.Text, tokens.WrapWidth, paragraph.Secondary ? theme.SecondaryText : theme.PrimaryText);
                break;
            case WarningBlock warning:
                using (fonts.Small.Push())
                {
                    ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + tokens.WrapWidth);
                    ImGui.TextColored(Colour(theme.SecondaryText), warning.Text);
                    ImGui.PopTextWrapPos();
                }

                break;
        }
    }

    private void DrawHeader(HeaderBlock header)
    {
        if (header.IconTexture is { } texture && IconOf(texture) is { } icon)
        {
            var wrap = textures.GetFromGameIcon(new GameIconLookup(icon.Id, icon.HighQuality)).GetWrapOrEmpty();
            ImGui.Image(wrap.Handle, new Vector2(tokens.IconSize));
            if (header.IconCooldown.Length > 0)
                DrawIconCooldown(header.IconCooldown);
            ImGui.SameLine(0, tokens.IconTextGap);
        }

        var textWidth = tokens.WrapWidth - tokens.IconSize - tokens.IconTextGap;
        var nameWidth = textWidth;
        ImGui.BeginGroup();
        if (header.Keybind.Length > 0)
            nameWidth -= DrawKeycap(KeyLabels.Readable(header.Keybind)) + tokens.InlineGap;
        using (fonts.Title.Push())
            SeString(header.Name, nameWidth, theme.PrimaryText);
        using (fonts.Small.Push())
        {
            foreach (var line in header.Lines)
                SeString(line, textWidth, theme.SecondaryText);
            if (header.Flags.Count > 0)
                SeString(Encoding.UTF8.GetBytes(string.Join(FlagSeparator, header.Flags.Select(flag => SeStringText.Plain(flag)))), textWidth, theme.SecondaryText);
        }

        ImGui.EndGroup();
    }

    private float DrawKeycap(string keybind)
    {
        float nameLineHeight;
        using (fonts.Title.Push())
            nameLineHeight = ImGui.GetTextLineHeight();
        using var font = fonts.Body.Push();
        var padding = new Vector2(tokens.InlineGap, tokens.RowGap / 2);
        var size = ImGui.CalcTextSize(keybind) + (padding * 2);
        var right = ImGui.GetWindowPos().X + tokens.Padding + tokens.WrapWidth;
        var min = new Vector2(right - size.X, ImGui.GetCursorScreenPos().Y + ((nameLineHeight - size.Y) / 2));
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(min, min + size, ImGui.GetColorU32(Colour(theme.Border, 0.6f)), tokens.BarRounding);
        drawList.AddRect(min, min + size, ImGui.GetColorU32(Colour(theme.Border)), tokens.BarRounding);
        drawList.AddText(min + padding, ImGui.GetColorU32(Colour(theme.PrimaryText)), keybind);
        return size.X;
    }

    private void DrawIconCooldown(string cooldown)
    {
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(0, 0, 0, 0.55f)));
        using (fonts.Title.Push())
        {
            var size = ImGui.CalcTextSize(cooldown);
            drawList.AddText(min + ((max - min - size) / 2), ImGui.GetColorU32(Colour(theme.PrimaryText)), cooldown);
        }
    }

    private void DrawDivider()
    {
        ImGui.Dummy(new Vector2(0, tokens.DividerMargin));
        var start = ImGui.GetCursorScreenPos();
        ImGui.GetWindowDrawList().AddLine(start, start + new Vector2(tokens.WrapWidth, 0), ImGui.GetColorU32(Colour(theme.Divider, theme.DividerAlpha)));
        ImGui.Dummy(new Vector2(0, tokens.DividerMargin));
    }

    private void DrawParams(ParamsBlock parameters)
    {
        if (!ImGui.BeginTable("params", parameters.Params.Count, ImGuiTableFlags.SizingStretchSame, new Vector2(tokens.WrapWidth, 0)))
            return;
        ImGui.TableNextRow();
        foreach (var parameter in parameters.Params)
        {
            ImGui.TableNextColumn();
            using (fonts.Small.Push())
                ImGui.TextColored(Colour(theme.SecondaryText), parameter.Label);
            float valueHeight;
            using (fonts.Title.Push())
            {
                ImGui.TextUnformatted(parameter.Value);
                valueHeight = ImGui.GetTextLineHeight();
            }

            if (parameter.Delta.Length > 0)
            {
                ImGui.SameLine(0, tokens.InlineGap / 2);
                using (fonts.Body.Push())
                {
                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + valueHeight - ImGui.GetTextLineHeight());
                    ImGui.TextColored(Colour(parameter.Delta.Contains('-') ? theme.Worse : theme.Better), parameter.Delta);
                }
            }
        }

        ImGui.EndTable();
    }

    private void DrawStats(StatTableBlock table)
    {
        using var font = fonts.Body.Push();
        var valueWidth = ImGui.CalcTextSize(WidestValue).X;
        var labelWidth = StatLabelWidth(tokens);
        var gap = tokens.InlineGap;
        if (!ImGui.BeginTable("stats", 4, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoPadInnerX | ImGuiTableFlags.NoPadOuterX))
            return;
        ImGui.TableSetupColumn("label1", ImGuiTableColumnFlags.WidthFixed, labelWidth + gap);
        ImGui.TableSetupColumn("value1", ImGuiTableColumnFlags.WidthFixed, valueWidth);
        ImGui.TableSetupColumn("label2", ImGuiTableColumnFlags.WidthFixed, gap + labelWidth + gap);
        ImGui.TableSetupColumn("value2", ImGuiTableColumnFlags.WidthFixed, valueWidth);
        var rowHeight = ImGui.GetTextLineHeight() + tokens.RowGap;
        for (var i = 0; i < table.Stats.Count; i++)
        {
            if (i % 2 == 0)
                ImGui.TableNextRow(ImGuiTableRowFlags.None, i + 2 < table.Stats.Count ? rowHeight : 0);
            var stat = table.Stats[i];
            ImGui.TableNextColumn();
            if (i % 2 == 1)
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + gap);
            ImGui.TextColored(Colour(theme.SecondaryText), Ellipsize(stat.Label, labelWidth));
            ImGui.TableNextColumn();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + valueWidth - ImGui.CalcTextSize(stat.Value).X);
            ImGui.TextUnformatted(stat.Value);
        }

        ImGui.EndTable();
    }

    private void DrawMateria(MateriaBlock materia)
    {
        using var font = fonts.Body.Push();
        var size = new Vector2(tokens.MateriaIconSize);
        for (var i = 0; i < materia.Materia.Count; i++)
        {
            if (i > 0)
                ImGui.Dummy(new Vector2(0, tokens.RowGap));
            var (name, effect) = materia.Materia[i];
            var left = ImGui.GetCursorPosX();
            if (name.Length > 0 && itemIcons.Find(name) is { } icon)
            {
                ImGui.Image(textures.GetFromGameIcon(new GameIconLookup(icon)).GetWrapOrEmpty().Handle, size);
            }
            else
            {
                ImGui.GetWindowDrawList().AddCircle(ImGui.GetCursorScreenPos() + (size / 2), (size.X / 2) - 1, ImGui.GetColorU32(Colour(theme.Divider, 1f)));
                ImGui.Dummy(size);
            }
            ImGui.SameLine(0, tokens.InlineGap);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ((size.Y - ImGui.GetTextLineHeight()) / 2));
            var effectWidth = effect.Length == 0 ? 0 : ImGui.CalcTextSize(effect).X + tokens.InlineGap;
            ImGui.TextUnformatted(Ellipsize(name, tokens.WrapWidth - size.X - tokens.InlineGap - effectWidth));
            if (effect.Length == 0)
                continue;
            ImGui.SameLine(left + tokens.WrapWidth - ImGui.CalcTextSize(effect).X);
            ImGui.TextColored(Colour(theme.SecondaryText), effect);
        }
    }

    private void DrawBar(BarBlock bar)
    {
        using (fonts.Body.Push())
        {
            ImGui.TextColored(Colour(theme.SecondaryText), bar.Label);
            ImGui.SameLine(ImGui.GetCursorPosX() + tokens.WrapWidth - ImGui.CalcTextSize(bar.Value).X);
            ImGui.TextUnformatted(bar.Value);
        }

        var lineHeight = ImGui.GetTextLineHeight();
        var start = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(tokens.WrapWidth, lineHeight));
        var top = start + new Vector2(0, (lineHeight - tokens.BarHeight) / 2);
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(top, top + new Vector2(tokens.WrapWidth, tokens.BarHeight), ImGui.GetColorU32(Colour(theme.Divider, theme.DividerAlpha)), tokens.BarRounding);
        if (bar.Fraction > 0)
            drawList.AddRectFilled(top, top + new Vector2(tokens.WrapWidth * bar.Fraction, tokens.BarHeight), ImGui.GetColorU32(Colour(theme.Accent)), tokens.BarRounding);
    }

    private void DrawKeyValue(KeyValueBlock row)
    {
        using var font = fonts.Body.Push();
        var keyWidth = tokens.WrapWidth * 0.4f;
        var left = ImGui.GetCursorPosX();
        ImGui.TextColored(Colour(theme.SecondaryText), Ellipsize(row.Key, keyWidth - tokens.InlineGap));
        ImGui.SameLine(left + keyWidth);
        SeString(row.Value, tokens.WrapWidth - keyWidth, theme.PrimaryText);
    }

    private void DrawOverflowFade()
    {
        var min = ImGui.GetWindowPos();
        var max = min + ImGui.GetWindowSize();
        var bottom = max.Y - tokens.Padding;
        var surface = ImGui.GetColorU32(Colour(theme.Surface, theme.SurfaceAlpha));
        var clear = ImGui.GetColorU32(Colour(theme.Surface, 0));
        ImGui.GetWindowDrawList().AddRectFilledMultiColor(new Vector2(min.X, bottom - tokens.OverflowFade), new Vector2(max.X, bottom), clear, clear, surface, surface);
    }

    private float StatLabelWidth(LayoutTokens scaled)
    {
        var valueWidth = ImGui.CalcTextSize(WidestValue).X;
        return (scaled.WrapWidth - (2 * valueWidth) - (3 * scaled.InlineGap)) / 2;
    }

    private static string Ellipsize(string text, float width)
    {
        if (ImGui.CalcTextSize(text).X <= width)
            return text;
        const string ellipsis = "…";
        for (var length = text.Length - 1; length > 0; length--)
        {
            var candidate = text[..length].TrimEnd() + ellipsis;
            if (ImGui.CalcTextSize(candidate).X <= width)
                return candidate;
        }

        return ellipsis;
    }

    private static void SeString(byte[] text, float wrapWidth, uint colour)
    {
        ImGuiHelpers.SeStringWrapped(text, new SeStringDrawParams { WrapWidth = wrapWidth, Color = ImGui.GetColorU32(Colour(colour)) });
    }

    private static (uint Id, bool HighQuality)? IconOf(string texture)
    {
        var match = IconFileName().Match(texture);
        if (!match.Success)
            return null;
        return (uint.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), texture.Contains("/hq/", StringComparison.Ordinal));
    }

    private static Vector4 Colour(uint rgb, float alpha = 1f) =>
        new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);

    [GeneratedRegex(@"(\d{6})(?:_hr1)?\.tex$")]
    private static partial Regex IconFileName();
}
