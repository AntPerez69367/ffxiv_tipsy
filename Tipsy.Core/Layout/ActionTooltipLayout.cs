using System.Text;
using Tipsy.Core.Text;
using Tipsy.Core.Tooltips;

namespace Tipsy.Core.Layout;

/// <summary>Arranges an ActionDetail snapshot: header with range and radius, cast and recast, description, acquired level and affinity, extras.</summary>
public static class ActionTooltipLayout
{
    private const string Separator = "   ";

    public static List<TooltipBlock> Build(TooltipSnapshot snapshot)
    {
        if (snapshot.Status == SnapshotStatus.SchemaMismatch)
            return SharedLayout.Fallback(snapshot);

        var slots = snapshot.Slots;
        if (!slots.TryGetValue(ActionDetailMap.Name, out var name))
            return [];

        var lines = new List<byte[]>();
        if (slots.TryGetValue(ActionDetailMap.Category, out var category))
            lines.Add(category.Text);
        var reach = string.Join(Separator, new[] { Pair(slots, ActionDetailMap.RangeLabel, ActionDetailMap.RangeValue), Pair(slots, ActionDetailMap.RadiusLabel, ActionDetailMap.RadiusValue) }.Where(part => part.Length > 0));
        if (reach.Length > 0)
            lines.Add(Encoding.UTF8.GetBytes(reach));

        var blocks = new List<TooltipBlock>
        {
            new HeaderBlock(
                slots.GetValueOrDefault(ActionDetailMap.Icon)?.Texture,
                slots.ContainsKey(ActionDetailMap.IconCooldown) ? Plain(slots, ActionDetailMap.IconCooldown).Trim() : string.Empty,
                SeStringText.SingleLine(name.Text),
                lines,
                []),
            new DividerBlock(),
        };

        var parameters = Enumerable.Range(0, ActionDetailMap.ParamCount)
            .Where(i => slots.ContainsKey(ActionDetailMap.ParamLabel(i)) && slots.ContainsKey(ActionDetailMap.ParamValue(i)))
            .Select(i => new ParamValue(Plain(slots, ActionDetailMap.ParamLabel(i)), Plain(slots, ActionDetailMap.ParamValue(i)), string.Empty))
            .ToList();
        if (parameters.Count > 0)
            blocks.Add(new ParamsBlock(parameters));

        if (slots.TryGetValue(ActionDetailMap.Description, out var description) && description.Text.Length > 0)
            blocks.Add(new ParagraphBlock(description.Text, false));

        KeyValue(blocks, slots, ActionDetailMap.AcquiredLabel, ActionDetailMap.AcquiredValue);
        KeyValue(blocks, slots, ActionDetailMap.AffinityLabel, ActionDetailMap.AffinityValue);

        SharedLayout.AppendExtras(blocks, snapshot);
        return blocks;
    }

    private static string Pair(IReadOnlyDictionary<string, SlotValue> slots, string label, string value) =>
        slots.ContainsKey(label) && slots.ContainsKey(value) ? $"{Plain(slots, label).Trim()} {Plain(slots, value).Trim()}" : string.Empty;

    private static void KeyValue(List<TooltipBlock> blocks, IReadOnlyDictionary<string, SlotValue> slots, string label, string value)
    {
        if (slots.ContainsKey(label) && slots.TryGetValue(value, out var shown))
            blocks.Add(new KeyValueBlock(Plain(slots, label), shown.Text));
    }

    private static string Plain(IReadOnlyDictionary<string, SlotValue> slots, string slot) => SeStringText.Plain(slots[slot].Text);
}
