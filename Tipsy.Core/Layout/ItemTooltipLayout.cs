using System.Globalization;
using Tipsy.Core.Text;
using Tipsy.Core.Tooltips;

namespace Tipsy.Core.Layout;

/// <summary>Arranges an ItemDetail snapshot into blocks: header, stats, materia, repairs, description, sale info, extras.</summary>
public static class ItemTooltipLayout
{
    public static List<TooltipBlock> Build(TooltipSnapshot snapshot)
    {
        if (snapshot.Status == SnapshotStatus.SchemaMismatch)
            return SharedLayout.Fallback(snapshot);

        var blocks = new List<TooltipBlock>();

        var slots = snapshot.Slots;
        if (!slots.TryGetValue(ItemDetailMap.Name, out var name))
            return blocks;

        blocks.Add(new HeaderBlock(
            slots.GetValueOrDefault(ItemDetailMap.Icon)?.Texture,
            slots.ContainsKey(ItemDetailMap.IconCooldown) ? Plain(slots, ItemDetailMap.IconCooldown).Trim() : string.Empty,
            name.Text,
            Texts(slots, ItemDetailMap.Category, ItemDetailMap.ItemLevel, ItemDetailMap.Level, ItemDetailMap.Classes),
            Texts(slots, ItemDetailMap.Unique, ItemDetailMap.Untradable, ItemDetailMap.Binding, ItemDetailMap.Owned)));
        blocks.Add(new DividerBlock());

        var parameters = Enumerable.Range(0, ItemDetailMap.ParamCount)
            .Where(i => slots.ContainsKey(ItemDetailMap.ParamLabel(i)) && slots.ContainsKey(ItemDetailMap.ParamValue(i)))
            .Select(i => new ParamValue(
                Plain(slots, ItemDetailMap.ParamLabel(i)),
                Plain(slots, ItemDetailMap.ParamValue(i)),
                slots.ContainsKey(ItemDetailMap.ParamDelta(i)) ? Plain(slots, ItemDetailMap.ParamDelta(i)).Trim() : string.Empty))
            .ToList();
        if (parameters.Count > 0)
            blocks.Add(new ParamsBlock(parameters));

        if (slots.TryGetValue(ItemDetailMap.Effects, out var effects))
        {
            Caption(blocks, slots, ItemDetailMap.EffectsHeader);
            blocks.Add(new ParagraphBlock(effects.Text, false));
        }

        var stats = Enumerable.Range(0, ItemDetailMap.BonusRows)
            .SelectMany(row => new[] { ItemDetailMap.BonusLeft(row), ItemDetailMap.BonusRight(row) })
            .Where(slots.ContainsKey)
            .Select(slot => SplitStat(Plain(slots, slot)))
            .ToList();
        if (stats.Count > 0)
        {
            Caption(blocks, slots, ItemDetailMap.BonusesHeader);
            blocks.Add(new StatTableBlock(stats));
        }

        var materia = Enumerable.Range(0, ItemDetailMap.MateriaSlots)
            .Where(slot => slots.ContainsKey(ItemDetailMap.MateriaSocket(slot)))
            .Select(slot => new LabelledValue(
                slots.ContainsKey(ItemDetailMap.MateriaName(slot)) ? Plain(slots, ItemDetailMap.MateriaName(slot)) : string.Empty,
                slots.ContainsKey(ItemDetailMap.MateriaEffect(slot)) ? Plain(slots, ItemDetailMap.MateriaEffect(slot)).Trim() : string.Empty))
            .ToList();
        if (materia.Count > 0)
        {
            Caption(blocks, slots, ItemDetailMap.MateriaHeader);
            blocks.Add(new MateriaBlock(materia));
        }

        if (slots.ContainsKey(ItemDetailMap.RepairsHeader))
        {
            Caption(blocks, slots, ItemDetailMap.RepairsHeader);
            for (var row = 0; row < ItemDetailMap.RepairRows.Count; row++)
            {
                var (label, value) = ItemDetailMap.RepairRows[row];
                if (!slots.ContainsKey(label) || !slots.TryGetValue(value, out var shown))
                    continue;
                if (row < 2 && Percent(Plain(slots, value)) is { } fraction)
                    blocks.Add(new BarBlock(Plain(slots, label), Plain(slots, value), fraction));
                else
                    blocks.Add(new KeyValueBlock(Plain(slots, label), shown.Text));
            }

            Paragraph(blocks, slots, ItemDetailMap.RepairsFlags, true);
        }

        if (slots.ContainsKey(ItemDetailMap.RequirementsHeader))
        {
            Caption(blocks, slots, ItemDetailMap.RequirementsHeader);
            foreach (var (label, value) in ItemDetailMap.RequirementRows)
            {
                if (slots.ContainsKey(label) && slots.TryGetValue(value, out var shown))
                    blocks.Add(new KeyValueBlock(Plain(slots, label), shown.Text));
            }
        }

        Paragraph(blocks, slots, ItemDetailMap.Description, false);
        Paragraph(blocks, slots, ItemDetailMap.CraftedBy, true);
        Paragraph(blocks, slots, ItemDetailMap.SellsFor, true);
        Paragraph(blocks, slots, ItemDetailMap.ShopPrice, true);

        SharedLayout.AppendExtras(blocks, snapshot);
        return blocks;
    }

    /// <summary>Splits "Vitality +410" into its name and signed amount; text without a trailing amount stays whole.</summary>
    public static LabelledValue SplitStat(string stat)
    {
        var space = stat.LastIndexOf(' ');
        if (space <= 0 || space == stat.Length - 1)
            return new LabelledValue(stat, string.Empty);
        var amount = stat[(space + 1)..];
        return amount[0] is '+' or '-' || char.IsDigit(amount[0])
            ? new LabelledValue(stat[..space], amount)
            : new LabelledValue(stat, string.Empty);
    }

    private static float? Percent(string value)
    {
        var trimmed = value.TrimEnd('%');
        if (trimmed.Length == value.Length || !float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
            return null;
        return Math.Clamp(percent / 100f, 0f, 1f);
    }

    private static void Caption(List<TooltipBlock> blocks, IReadOnlyDictionary<string, SlotValue> slots, string slot)
    {
        if (slots.ContainsKey(slot))
            blocks.Add(new CaptionBlock(Plain(slots, slot).ToUpperInvariant()));
    }

    private static void Paragraph(List<TooltipBlock> blocks, IReadOnlyDictionary<string, SlotValue> slots, string slot, bool secondary)
    {
        if (slots.TryGetValue(slot, out var value) && value.Text.Length > 0 && Plain(slots, slot).Trim().Length > 0)
            blocks.Add(new ParagraphBlock(value.Text, secondary));
    }

    private static List<byte[]> Texts(IReadOnlyDictionary<string, SlotValue> slots, params string[] wanted) =>
        wanted
            .Where(slot => slots.TryGetValue(slot, out var value) && value.Text.Length > 0 && Plain(slots, slot).Trim().Length > 0)
            .Select(slot => slots[slot].Text)
            .ToList();

    private static string Plain(IReadOnlyDictionary<string, SlotValue> slots, string slot) => SeStringText.Plain(slots[slot].Text);
}
