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

    private readonly TooltipReader reader;
    private readonly IGameGui gameGui;
    private readonly ITextureProvider textures;
    private readonly TooltipFonts fonts;
    private readonly ItemIcons itemIcons;
    private readonly LayoutTokens baseTokens = new();
    private readonly ThemeColors theme = ThemeColors.Native;
    private readonly AnchorPreset anchor = AnchorPreset.TopRight;

    private LayoutTokens tokens = new();
    private TooltipSnapshot? laidOut;
    private List<TooltipBlock> blocks = [];
    private float lastContentHeight;
    private WindowPlacement placement;
    private int pushedStyles;
    private int pushedColors;

    internal TooltipOverlay(TooltipReader reader, IGameGui gameGui, ITextureProvider textures, TooltipFonts fonts, ItemIcons itemIcons)
        : base("Tipsy tooltip##overlay", ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.reader = reader;
        this.gameGui = gameGui;
        this.textures = textures;
        this.fonts = fonts;
        this.itemIcons = itemIcons;
        IsOpen = true;
        RespectCloseHotkey = false;
        DisableWindowSounds = true;
    }

    public override bool DrawConditions()
    {
        var unit = (AtkUnitBase*)gameGui.GetAddonByName(reader.Addon).Address;
        if (unit == null || !unit->IsVisible || reader.Current is not { } snapshot)
            return false;
        if (!ReferenceEquals(snapshot, laidOut))
        {
            laidOut = snapshot;
            blocks = ItemTooltipLayout.Build(snapshot);
        }

        return blocks.Count > 0;
    }

    public override void PreDraw()
    {
        tokens = baseTokens.Scaled(ImGuiHelpers.GlobalScale);
        var viewport = ImGui.GetMainViewport();
        placement = Placement.Place(anchor, viewport.WorkPos, viewport.WorkPos + viewport.WorkSize, tokens.ViewportInset, tokens.Width, lastContentHeight, Vector2.Zero, ImGui.GetMousePos(), Vector2.Zero);
        ImGui.SetNextWindowPos(placement.Position, ImGuiCond.Always, placement.Pivot);
        ImGui.SetNextWindowSizeConstraints(new Vector2(tokens.Width, 0), new Vector2(tokens.Width, placement.MaxHeight));

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(tokens.Padding));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, tokens.Rounding);
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
        foreach (var block in blocks)
        {
            Gap(previous, block);
            DrawBlock(block);
            previous = block;
        }

        lastContentHeight = ImGui.GetCursorPosY() + tokens.Padding;
        if (placement.Overflowing)
            DrawOverflowFade();
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
        ImGui.BeginGroup();
        using (fonts.Title.Push())
            SeString(header.Name, textWidth, theme.PrimaryText);
        using (fonts.Small.Push())
        {
            foreach (var line in header.Lines)
                SeString(line, textWidth, theme.SecondaryText);
            if (header.Flags.Count > 0)
                SeString(Encoding.UTF8.GetBytes(string.Join(FlagSeparator, header.Flags.Select(flag => SeStringText.Plain(flag)))), textWidth, theme.SecondaryText);
        }

        ImGui.EndGroup();
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
