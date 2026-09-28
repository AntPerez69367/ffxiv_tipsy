using Tipsy.Core.Nodes;
using Tipsy.Core.Text;

namespace Tipsy.Core.Tooltips;

public static class SnapshotBuilder
{
    public static TooltipSnapshot Build(TooltipMap map, IReadOnlyList<NodeRecord> nodes)
    {
        var byPath = new Dictionary<string, NodeRecord>(nodes.Count);
        foreach (var node in nodes)
            byPath[node.Path] = node;

        var mismatch = FindMismatch(map, byPath);
        if (mismatch is not null)
            return new TooltipSnapshot(map.Addon, SnapshotStatus.SchemaMismatch, mismatch, new Dictionary<string, SlotValue>(), ExtrasOf(nodes, []));

        var slots = new Dictionary<string, SlotValue>();
        var mapped = new HashSet<string>();
        foreach (var binding in map.Slots)
        {
            mapped.Add(binding.Path);
            var node = byPath[binding.Path];
            if (node.Shown)
                slots[binding.Slot] = new SlotValue(new SeText(node.Text), node.Texture, node.PartId);
        }

        return new TooltipSnapshot(map.Addon, SnapshotStatus.Ok, null, slots, ExtrasOf(nodes, mapped));
    }

    private static string? FindMismatch(TooltipMap map, Dictionary<string, NodeRecord> byPath)
    {
        foreach (var binding in map.Slots)
        {
            if (!byPath.TryGetValue(binding.Path, out var node))
                return $"{binding.Slot}: node {binding.Path} is missing";
            if (node.Type != binding.Type)
                return $"{binding.Slot}: node {binding.Path} is {node.Type}, expected {binding.Type}";
        }

        return null;
    }

    private static List<ExtraLine> ExtrasOf(IReadOnlyList<NodeRecord> nodes, HashSet<string> mapped) =>
        nodes
            .Where(node => node.Type == NodeRecord.TextType
                           && node.Shown
                           && node.Text.Length > 0
                           && node.Width > 0
                           && node.Height > 0
                           && !mapped.Contains(node.Path))
            .OrderBy(node => node.ScreenY)
            .ThenBy(node => node.ScreenX)
            .Select(node => new ExtraLine(node.Path, new SeText(node.Text), node.ScreenX, node.ScreenY))
            .ToList();
}
