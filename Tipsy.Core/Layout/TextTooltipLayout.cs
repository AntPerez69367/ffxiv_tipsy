using Tipsy.Core.Tooltips;

namespace Tipsy.Core.Layout;

/// <summary>Lays out the generic Tooltip addon: its text as one paragraph, then any extras.</summary>
public static class TextTooltipLayout
{
    public static List<TooltipBlock> Build(TooltipSnapshot snapshot)
    {
        if (snapshot.Status == SnapshotStatus.SchemaMismatch)
            return SharedLayout.Fallback(snapshot);
        if (!snapshot.Slots.TryGetValue(TextTooltipMap.Text, out var text) || text.Text.Length == 0)
            return [];

        List<TooltipBlock> blocks = [new ParagraphBlock(text.Text, false)];
        SharedLayout.AppendExtras(blocks, snapshot);
        return blocks;
    }
}
