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
            SharedLayout.Plain(slots, ItemDetailMap.IconCooldown).Trim(),
            SeStringText.SingleLine(name.Text),
            Texts(slots, ItemDetailMap.Category, ItemDetailMap.ItemLevel, ItemDetailMap.Level, ItemDetailMap.Classes),
            Texts(slots, ItemDetailMap.Unique, ItemDetailMap.Untradable, ItemDetailMap.Binding, ItemDetailMap.Owned)));
        blocks.Add(new DividerBlock());

        var parameters = Enumerable.Range(0, ItemDetailMap.ParamCount)
            .Where(i => slots.ContainsKey(ItemDetailMap.ParamLabel(i)) && slots.ContainsKey(ItemDetailMap.ParamValue(i)))
            .Select(i => new ParamValue(
                SharedLayout.Plain(slots, ItemDetailMap.ParamLabel(i)),
                SharedLayout.Plain(slots, ItemDetailMap.ParamValue(i)),
                SharedLayout.Plain(slots, ItemDetailMap.ParamDelta(i)).Trim()))
            .ToList();
        if (parameters.Count > 0)
            blocks.Add(new ParamsBlock([.. parameters]));

        if (slots.TryGetValue(ItemDetailMap.Effects, out var effects))
        {
            Caption(blocks, slots, ItemDetailMap.EffectsHeader);
            blocks.Add(new ParagraphBlock(effects.Text, false));
        }

        var stats = Enumerable.Range(0, ItemDetailMap.BonusRows)
            .SelectMany(row => new[] { ItemDetailMap.BonusLeft(row), ItemDetailMap.BonusRight(row) })
            .Where(slots.ContainsKey)
            .Select(slot => SplitStat(SharedLayout.Plain(slots, slot)))
            .ToList();
        if (stats.Count > 0)
        {
            Caption(blocks, slots, ItemDetailMap.BonusesHeader);
            blocks.Add(new StatTableBlock([.. stats]));
        }

        var materia = Enumerable.Range(0, ItemDetailMap.MateriaSlots)
            .Where(slot => slots.ContainsKey(ItemDetailMap.MateriaSocket(slot)))
            .Select(slot => new LabelledValue(
                SharedLayout.Plain(slots, ItemDetailMap.MateriaName(slot)),
                SharedLayout.Plain(slots, ItemDetailMap.MateriaEffect(slot)).Trim()))
            .ToList();
        if (materia.Count > 0)
        {
            Caption(blocks, slots, ItemDetailMap.MateriaHeader);
            blocks.Add(new MateriaBlock([.. materia]));
        }

        if (slots.ContainsKey(ItemDetailMap.RepairsHeader))
        {
            Caption(blocks, slots, ItemDetailMap.RepairsHeader);
            for (var row = 0; row < ItemDetailMap.RepairRows.Count; row++)
            {
                var (label, value) = ItemDetailMap.RepairRows[row];
                if (!slots.ContainsKey(label) || !slots.TryGetValue(value, out var shown))
                    continue;
                var plainValue = SeStringText.Plain(shown.Text);
                if (row < 2 && Percent(plainValue) is { } fraction)
                    blocks.Add(new BarBlock(SharedLayout.Plain(slots, label), plainValue, fraction));
                else
                    blocks.Add(new KeyValueBlock(SharedLayout.Plain(slots, label), shown.Text));
            }

            Paragraph(blocks, slots, ItemDetailMap.RepairsFlags, true);
        }

        if (slots.ContainsKey(ItemDetailMap.RequirementsHeader))
        {
            Caption(blocks, slots, ItemDetailMap.RequirementsHeader);
            foreach (var (label, value) in ItemDetailMap.RequirementRows)
            {
                if (slots.ContainsKey(label) && slots.TryGetValue(value, out var shown))
                    blocks.Add(new KeyValueBlock(SharedLayout.Plain(slots, label), shown.Text));
            }
        }

        Paragraph(blocks, slots, ItemDetailMap.Description, false);
        Paragraph(blocks, slots, ItemDetailMap.CraftedBy, true);
        Paragraph(blocks, slots, ItemDetailMap.SellsFor, true);
        Paragraph(blocks, slots, ItemDetailMap.ShopPrice, true);

        SharedLayout.AppendExtras(blocks, snapshot);
        return blocks;
    }

    /// <summary>
    /// Splits "Vitality +410" into its name and signed amount, on any kind of space and with full-width signs and digits
    /// as some clients write them; text without a trailing amount stays whole.
    /// </summary>
    public static LabelledValue SplitStat(string stat)
    {
        var trimmed = stat.TrimEnd();
        var space = trimmed.Length - 1;
        while (space > 0 && !char.IsWhiteSpace(trimmed[space]))
            space--;
        if (space <= 0)
            return new LabelledValue(stat, string.Empty);
        var amount = trimmed[(space + 1)..];
        return amount[0] is '+' or '-' or '\uFF0B' or '\u2212' or '\uFF0D' || char.IsDigit(amount[0])
            ? new LabelledValue(trimmed[..space].TrimEnd(), amount)
            : new LabelledValue(stat, string.Empty);
    }

    public static float? Percent(string value)
    {
        var trimmed = value.Trim().TrimEnd('%', '\uFF05').Trim().Replace(',', '.');
        if ((!value.Contains('%') && !value.Contains('\uFF05')) || !float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
            return null;
        return Math.Clamp(percent / 100f, 0f, 1f);
    }

    private static void Caption(List<TooltipBlock> blocks, IReadOnlyDictionary<string, SlotValue> slots, string slot)
    {
        if (slots.ContainsKey(slot))
            blocks.Add(new CaptionBlock(SharedLayout.Plain(slots, slot).ToUpperInvariant()));
    }

    private static void Paragraph(List<TooltipBlock> blocks, IReadOnlyDictionary<string, SlotValue> slots, string slot, bool secondary)
    {
        if (slots.TryGetValue(slot, out var value) && HasText(value))
            blocks.Add(new ParagraphBlock(value.Text, secondary));
    }

    private static EquatableList<SeText> Texts(IReadOnlyDictionary<string, SlotValue> slots, params string[] wanted) =>
    [
        .. wanted
            .Select(slots.GetValueOrDefault)
            .OfType<SlotValue>()
            .Where(HasText)
            .Select(value => value.Text),
    ];

    private static bool HasText(SlotValue value) => !string.IsNullOrWhiteSpace(SeStringText.Plain(value.Text));
}
