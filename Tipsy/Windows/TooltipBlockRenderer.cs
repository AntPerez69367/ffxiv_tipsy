using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using Tipsy.Core.Layout;
using Tipsy.Core.Text;
using Tipsy.Game;

namespace Tipsy.Windows;

/// <summary>
/// Draws tooltip blocks top to bottom into the current ImGui window. Text measurements and joined lines are kept until
/// the blocks or the tokens change.
/// </summary>
internal sealed class TooltipBlockRenderer
{
    private const string WidestValue = "+9999";
    private const string FlagSeparator = "   ";
    private const string Ellipsis = "…";

    private readonly ITextureProvider textures;
    private readonly TooltipFonts fonts;
    private readonly ItemIcons itemIcons;
    private readonly Dictionary<(string Text, float Width), string> ellipsized = [];
    private readonly Dictionary<HeaderBlock, SeText> flagLines = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, GameIcon?> icons = [];

    private IReadOnlyList<TooltipBlock> cachedBlocks = [];
    private LayoutTokens? cachedTokens;
    private (IReadOnlyList<TooltipBlock> Blocks, float Scale, float Pixels)? widest;
    private LayoutTokens tokens = new();
    private ThemeColors theme = ThemeColors.Minimal;

    public TooltipBlockRenderer(ITextureProvider textures, TooltipFonts fonts, ItemIcons itemIcons)
    {
        this.textures = textures;
        this.fonts = fonts;
        this.itemIcons = itemIcons;
    }

    public void Draw(IReadOnlyList<TooltipBlock> blocks, LayoutTokens scaledTokens, ThemeColors colours)
    {
        if (!ReferenceEquals(blocks, cachedBlocks) || scaledTokens != cachedTokens)
        {
            ellipsized.Clear();
            flagLines.Clear();
            icons.Clear();
            cachedBlocks = blocks;
            cachedTokens = scaledTokens;
        }

        tokens = scaledTokens;
        theme = colours;
        TooltipBlock? previous = null;
        foreach (var block in blocks)
        {
            var gap = BlockGap.Before(previous, block, tokens);
            if (gap > 0)
                ImGui.Dummy(new Vector2(0, gap));
            DrawBlock(block);
            previous = block;
        }
    }

    /// <summary>The unscaled window width that fits the longest line of <paramref name="blocks"/>, capped at <paramref name="maxWidth"/>.</summary>
    public float FittedWidth(IReadOnlyList<TooltipBlock> blocks, float maxWidth, float padding, float scale)
    {
        if (widest is not { } cached || !ReferenceEquals(cached.Blocks, blocks) || cached.Scale != scale)
        {
            cached = (blocks, scale, WidestLine(blocks));
            widest = cached;
        }

        return Math.Min(maxWidth, MathF.Ceiling(cached.Pixels / scale) + (2 * padding) + 1);
    }

#if TIPSY_PROBE
    /// <summary>The stat labels that would be clipped in the stat table at <paramref name="width"/> unscaled pixels.</summary>
    public List<string> ClippedStatLabels(IEnumerable<string> labels, float width)
    {
        var scaled = (new LayoutTokens() with { Width = width }).Scaled(ImGuiHelpers.GlobalScale);
        using (fonts.Body.Push())
        {
            var labelWidth = StatLabelWidth(scaled);
            return labels.Where(label => ImGui.CalcTextSize(label).X > labelWidth).ToList();
        }
    }
#endif

    private float WidestLine(IReadOnlyList<TooltipBlock> blocks)
    {
        var pixels = 0f;
        using (fonts.Body.Push())
        {
            foreach (var block in blocks)
            {
                var text = block switch
                {
                    ParagraphBlock paragraph => SeStringText.Plain(paragraph.Text),
                    ExtraBlock extra => SeStringText.Plain(extra.Text),
                    WarningBlock warning => warning.Text,
                    CaptionBlock caption => caption.Text,
                    _ => string.Empty,
                };
                foreach (var line in text.Split('\n'))
                    pixels = Math.Max(pixels, ImGui.CalcTextSize(line).X);
            }
        }

        return pixels;
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
                    ImGui.TextColored(Rgb.ToVector4(theme.Accent), caption.Text);
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
            case ExtraBlock extra:
                using (fonts.Body.Push())
                    SeString(extra.Text, tokens.WrapWidth, theme.PrimaryText);
                break;
            case WarningBlock warning:
                using (fonts.Small.Push())
                {
                    ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + tokens.WrapWidth);
                    ImGui.TextColored(Rgb.ToVector4(theme.SecondaryText), warning.Text);
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
                SeString(FlagLine(header), textWidth, theme.SecondaryText);
        }

        ImGui.EndGroup();
    }

    private SeText FlagLine(HeaderBlock header)
    {
        if (!flagLines.TryGetValue(header, out var line))
        {
            line = SeText.Utf8(string.Join(FlagSeparator, header.Flags.Select(flag => SeStringText.Plain(flag))));
            flagLines[header] = line;
        }

        return line;
    }

    private GameIcon? IconOf(string texture)
    {
        if (!icons.TryGetValue(texture, out var icon))
        {
            icon = GameIcon.FromTexture(texture);
            icons[texture] = icon;
        }

        return icon;
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
        drawList.AddRectFilled(min, min + size, ImGui.GetColorU32(Rgb.ToVector4(theme.Border, 0.6f)), tokens.BarRounding);
        drawList.AddRect(min, min + size, ImGui.GetColorU32(Rgb.ToVector4(theme.Border)), tokens.BarRounding);
        drawList.AddText(min + padding, ImGui.GetColorU32(Rgb.ToVector4(theme.PrimaryText)), keybind);
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
            drawList.AddText(min + ((max - min - size) / 2), ImGui.GetColorU32(Rgb.ToVector4(theme.PrimaryText)), cooldown);
        }
    }

    private void DrawDivider()
    {
        ImGui.Dummy(new Vector2(0, tokens.DividerMargin));
        var start = ImGui.GetCursorScreenPos();
        ImGui.GetWindowDrawList().AddLine(start, start + new Vector2(tokens.WrapWidth, 0), ImGui.GetColorU32(Rgb.ToVector4(theme.Divider, theme.DividerAlpha)));
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
                ImGui.TextColored(Rgb.ToVector4(theme.SecondaryText), parameter.Label);
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
                    ImGui.TextColored(Rgb.ToVector4(parameter.Delta.Contains('-') ? theme.Worse : theme.Better), parameter.Delta);
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
            ImGui.TextColored(Rgb.ToVector4(theme.SecondaryText), Ellipsize(stat.Label, labelWidth));
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
                ImGui.GetWindowDrawList().AddCircle(ImGui.GetCursorScreenPos() + (size / 2), (size.X / 2) - 1, ImGui.GetColorU32(Rgb.ToVector4(theme.Divider)));
                ImGui.Dummy(size);
            }
            ImGui.SameLine(0, tokens.InlineGap);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ((size.Y - ImGui.GetTextLineHeight()) / 2));
            var effectWidth = effect.Length == 0 ? 0 : ImGui.CalcTextSize(effect).X + tokens.InlineGap;
            ImGui.TextUnformatted(Ellipsize(name, tokens.WrapWidth - size.X - tokens.InlineGap - effectWidth));
            if (effect.Length == 0)
                continue;
            ImGui.SameLine(left + tokens.WrapWidth - ImGui.CalcTextSize(effect).X);
            ImGui.TextColored(Rgb.ToVector4(theme.SecondaryText), effect);
        }
    }

    private void DrawBar(BarBlock bar)
    {
        using (fonts.Body.Push())
        {
            ImGui.TextColored(Rgb.ToVector4(theme.SecondaryText), bar.Label);
            ImGui.SameLine(ImGui.GetCursorPosX() + tokens.WrapWidth - ImGui.CalcTextSize(bar.Value).X);
            ImGui.TextUnformatted(bar.Value);
        }

        var lineHeight = ImGui.GetTextLineHeight();
        var start = ImGui.GetCursorScreenPos();
        ImGui.Dummy(new Vector2(tokens.WrapWidth, lineHeight));
        var top = start + new Vector2(0, (lineHeight - tokens.BarHeight) / 2);
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(top, top + new Vector2(tokens.WrapWidth, tokens.BarHeight), ImGui.GetColorU32(Rgb.ToVector4(theme.Divider, theme.DividerAlpha)), tokens.BarRounding);
        if (bar.Fraction > 0)
            drawList.AddRectFilled(top, top + new Vector2(tokens.WrapWidth * bar.Fraction, tokens.BarHeight), ImGui.GetColorU32(Rgb.ToVector4(theme.Accent)), tokens.BarRounding);
    }

    private void DrawKeyValue(KeyValueBlock row)
    {
        using var font = fonts.Body.Push();
        var keyWidth = tokens.WrapWidth * 0.4f;
        var left = ImGui.GetCursorPosX();
        ImGui.TextColored(Rgb.ToVector4(theme.SecondaryText), Ellipsize(row.Key, keyWidth - tokens.InlineGap));
        ImGui.SameLine(left + keyWidth);
        SeString(row.Value, tokens.WrapWidth - keyWidth, theme.PrimaryText);
    }

    private static float StatLabelWidth(LayoutTokens scaled)
    {
        var valueWidth = ImGui.CalcTextSize(WidestValue).X;
        return (scaled.WrapWidth - (2 * valueWidth) - (3 * scaled.InlineGap)) / 2;
    }

    private string Ellipsize(string text, float width)
    {
        if (ellipsized.TryGetValue((text, width), out var fitted))
            return fitted;
        fitted = Shorten(text, width);
        ellipsized[(text, width)] = fitted;
        return fitted;
    }

    private static string Shorten(string text, float width)
    {
        if (ImGui.CalcTextSize(text).X <= width)
            return text;
        for (var length = text.Length - 1; length > 0; length--)
        {
            var candidate = text[..length].TrimEnd() + Ellipsis;
            if (ImGui.CalcTextSize(candidate).X <= width)
                return candidate;
        }

        return Ellipsis;
    }

    private static void SeString(SeText text, float wrapWidth, uint colour)
    {
        ImGuiHelpers.SeStringWrapped(text, new SeStringDrawParams { WrapWidth = wrapWidth, Color = ImGui.GetColorU32(Rgb.ToVector4(colour)) });
    }
}
