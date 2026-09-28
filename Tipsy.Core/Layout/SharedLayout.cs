using Tipsy.Core.Text;
using Tipsy.Core.Tooltips;

namespace Tipsy.Core.Layout;

/// <summary>The parts every tooltip layout ends the same way: the plain-text fallback and the extras section.</summary>
public static class SharedLayout
{
    public const string MismatchWarning = "Tipsy doesn't recognize this tooltip's layout yet, so it's shown as plain text.";

    /// <summary>The visible text of <paramref name="slot"/>, or empty when that slot was not shown.</summary>
    public static string Plain(IReadOnlyDictionary<string, SlotValue> slots, string slot) =>
        slots.TryGetValue(slot, out var value) ? SeStringText.Plain(value.Text) : string.Empty;

    public static List<TooltipBlock> Fallback(TooltipSnapshot snapshot)
    {
        List<TooltipBlock> blocks = [new WarningBlock(MismatchWarning)];
        blocks.AddRange(snapshot.Extras.Select(extra => new ParagraphBlock(extra.Text, false)));
        return blocks;
    }

    /// <summary>
    /// Folds the text tooltip the game opens next to an item or action into that tooltip. When it is the same name with
    /// a bracketed key, as in "Interject [`]", the key becomes the header's keybind. When it repeats the name or a header
    /// line, such as the "Legs" the Character window shows, it adds nothing. Any other text, and any line other plugins
    /// added to the text tooltip, is kept as an extra line, so hiding the text tooltip never loses what it said.
    /// </summary>
    public static List<TooltipBlock> WithTextTooltip(List<TooltipBlock> blocks, TooltipSnapshot textTooltip)
    {
        if (blocks.Count == 0 || blocks[0] is not HeaderBlock header)
            return blocks;

        var folded = header;
        List<byte[]> extras = [];
        if (textTooltip.Slots.TryGetValue(TextTooltipMap.Text, out var text))
        {
            var plain = SeStringText.Plain(text.Text).Trim();
            var name = Comparable(SeStringText.Plain(header.Name));
            var comparable = Comparable(plain);
            var repeats = plain.Length == 0 || comparable == name || header.Lines.Any(line => Comparable(SeStringText.Plain(line)) == comparable);
            var key = repeats ? null : KeyAfterName(plain, name);
            if (key is { Length: > 0 })
                folded = header with { Keybind = key };
            else if (!repeats && key is null)
                extras.Add(text.Text);
        }

        extras.AddRange(textTooltip.Extras.Select(extra => extra.Text));
        if (ReferenceEquals(folded, header) && extras.Count == 0)
            return blocks;

        List<TooltipBlock> combined = [folded, .. blocks.Skip(1)];
        if (extras.Count > 0 && combined[^1] is not ExtraBlock)
            combined.Add(new DividerBlock());
        combined.AddRange(extras.Select(extra => new ExtraBlock(extra)));
        return combined;
    }

    private static string? KeyAfterName(string plain, string name)
    {
        for (var open = plain.IndexOf('['); open >= 0; open = plain.IndexOf('[', open + 1))
        {
            if (Comparable(plain[..open]) == name && KeyOf(plain[open..]) is { } key)
                return key;
        }

        return null;
    }

    private static string? KeyOf(string rest)
    {
        var bracketed = rest.Trim();
        if (bracketed.Length < 2 || bracketed[0] != '[' || bracketed[^1] != ']')
            return null;
        return bracketed[1..^1].Trim();
    }

    private static string Comparable(string text) =>
        new string(text.Where(character => character is < '\uE000' or > '\uF8FF').ToArray()).Trim();

    public static void AppendExtras(List<TooltipBlock> blocks, TooltipSnapshot snapshot)
    {
        if (snapshot.Extras.Count == 0)
            return;
        blocks.Add(new DividerBlock());
        blocks.AddRange(snapshot.Extras.Select(extra => new ExtraBlock(extra.Text)));
    }
}
