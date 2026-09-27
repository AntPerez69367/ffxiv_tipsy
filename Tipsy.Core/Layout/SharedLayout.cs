using Tipsy.Core.Text;
using Tipsy.Core.Tooltips;

namespace Tipsy.Core.Layout;

/// <summary>The parts every tooltip layout ends the same way: the plain-text fallback and the extras section.</summary>
public static class SharedLayout
{
    public const string ExtrasCaption = "EXTRAS";
    public const string MismatchWarning = "Tipsy's map for this tooltip is out of date, so it is shown as plain text.";

    public static List<TooltipBlock> Fallback(TooltipSnapshot snapshot)
    {
        List<TooltipBlock> blocks = [new WarningBlock(MismatchWarning)];
        blocks.AddRange(snapshot.Extras.Select(extra => new ParagraphBlock(extra.Text, false)));
        return blocks;
    }

    /// <summary>
    /// Folds the text tooltip the game opens next to a hotbar slot, "Interject [`]", into the header of the item or
    /// action tooltip open with it: the key in brackets becomes the header's keybind. Blocks without a header, or text
    /// without a bracketed key, come back unchanged.
    /// </summary>
    public static List<TooltipBlock> WithKeybind(List<TooltipBlock> blocks, TooltipSnapshot textTooltip)
    {
        if (blocks.Count == 0 || blocks[0] is not HeaderBlock header || !textTooltip.Slots.TryGetValue(TextTooltipMap.Text, out var text))
            return blocks;
        var plain = SeStringText.Plain(text.Text).Trim();
        var open = plain.LastIndexOf('[');
        if (!plain.EndsWith(']') || open < 0 || open == plain.Length - 2)
            return blocks;

        return [header with { Keybind = plain[(open + 1)..^1] }, .. blocks.Skip(1)];
    }

    public static void AppendExtras(List<TooltipBlock> blocks, TooltipSnapshot snapshot)
    {
        if (snapshot.Extras.Count == 0)
            return;
        blocks.Add(new DividerBlock());
        blocks.Add(new CaptionBlock(ExtrasCaption));
        blocks.AddRange(snapshot.Extras.Select(extra => new ParagraphBlock(extra.Text, false)));
    }
}
