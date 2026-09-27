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

    public static void AppendExtras(List<TooltipBlock> blocks, TooltipSnapshot snapshot)
    {
        if (snapshot.Extras.Count == 0)
            return;
        blocks.Add(new DividerBlock());
        blocks.Add(new CaptionBlock(ExtrasCaption));
        blocks.AddRange(snapshot.Extras.Select(extra => new ParagraphBlock(extra.Text, false)));
    }
}
