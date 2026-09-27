using Tipsy.Core.Nodes;
using Tipsy.Core.Tooltips;
using Xunit;

namespace Tipsy.Core.Tests.Tooltips;

public class ItemDetailReplayTests
{
    private const string PriceInsightPath = "32612";

    [Fact]
    public void GearFillsHeaderParamsBonusesMateriaAndRepairs()
    {
        var snapshot = Build("gear-grimoire");

        Assert.Equal(SnapshotStatus.Ok, snapshot.Status);
        Assert.Equal("Zormor Grimoire", Text(snapshot, ItemDetailMap.Name));
        Assert.Equal("Arcanist's Grimoire", Text(snapshot, ItemDetailMap.Category));
        Assert.Equal("Item Level 660", Text(snapshot, ItemDetailMap.ItemLevel));
        Assert.Equal("ACN SMN", Text(snapshot, ItemDetailMap.Classes));
        Assert.Equal("Lv. 93", Text(snapshot, ItemDetailMap.Level));
        Assert.Equal(["Magic Damage", "Auto-attack", "Delay"], Enumerable.Range(0, 3).Select(i => Text(snapshot, ItemDetailMap.ParamLabel(i))));
        Assert.Equal(["131", "136.24", "3.12"], Enumerable.Range(0, 3).Select(i => Text(snapshot, ItemDetailMap.ParamValue(i))));
        Assert.Equal("Vitality +410", Text(snapshot, ItemDetailMap.BonusLeft(0)));
        Assert.Equal("Direct Hit Rate +303", Text(snapshot, ItemDetailMap.BonusRight(1)));
        Assert.DoesNotContain(ItemDetailMap.BonusLeft(2), snapshot.Slots.Keys);
        Assert.Contains(ItemDetailMap.MateriaSocket(1), snapshot.Slots.Keys);
        Assert.DoesNotContain(ItemDetailMap.MateriaSocket(2), snapshot.Slots.Keys);
        Assert.Equal("100%", Text(snapshot, ItemDetailMap.RepairRows[0].Value));
        Assert.Equal("ui/icon/037000/037810_hr1.tex", snapshot.Slots[ItemDetailMap.Icon].Texture);
        Assert.Empty(snapshot.Extras);
    }

    [Fact]
    public void TextAnotherPluginAppendsToAGameNodeStaysInThatSlot()
    {
        var snapshot = Build("gear-grimoire");

        Assert.Contains("Owned: 1", Text(snapshot, ItemDetailMap.Description));
    }

    [Fact]
    public void GearWithThreeBonusRowsShowsTheThirdRow()
    {
        var snapshot = Build("gear-wristguards");

        Assert.Equal("Mind +2", Text(snapshot, ItemDetailMap.BonusLeft(2)));
    }

    [Fact]
    public void FoodFillsEffectsAndCrafterAndPutsTheInjectedPriceInExtras()
    {
        var snapshot = Build("food-baklava");

        Assert.Equal("Effects", Text(snapshot, ItemDetailMap.EffectsHeader));
        Assert.StartsWith("Spell Speed +8%", Text(snapshot, ItemDetailMap.Effects));
        Assert.Equal("Alaric Vilerose", Text(snapshot, ItemDetailMap.CraftedBy));
        var extra = Assert.Single(snapshot.Extras);
        Assert.Equal(PriceInsightPath, extra.Path);
        Assert.StartsWith("Marketboard Price:", Fixture.Plain(extra.Text));
    }

    [Fact]
    public void HighQualityNameKeepsItsGlyphAndColourPayloads()
    {
        var snapshot = Build("hq-max-potion");

        var name = snapshot.Slots[ItemDetailMap.Name].Text;
        Assert.Equal(0x02, name[0]);
        Assert.EndsWith("Max-Potion ", Text(snapshot, ItemDetailMap.Name));
    }

    [Fact]
    public void CardHasNoParamsOrRepairs()
    {
        var snapshot = Build("card-chimera");

        Assert.Equal("Chimera Card", Text(snapshot, ItemDetailMap.Name));
        Assert.DoesNotContain(ItemDetailMap.ParamLabel(0), snapshot.Slots.Keys);
        Assert.DoesNotContain(ItemDetailMap.RepairsHeader, snapshot.Slots.Keys);
    }

    [Fact]
    public void TreeWithNothingShownHasNoSlotsOrExtras()
    {
        var snapshot = Build("closed-baklava");

        Assert.Equal(SnapshotStatus.Ok, snapshot.Status);
        Assert.Empty(snapshot.Slots);
        Assert.Empty(snapshot.Extras);
    }

    [Fact]
    public void WrongNodeTypeIsASchemaMismatchWithEveryLineInExtras()
    {
        var map = ItemDetailMap.Map with
        {
            Slots = ItemDetailMap.Map.Slots.Select(binding => binding.Slot == ItemDetailMap.Name ? binding with { Type = "Image" } : binding).ToList(),
        };

        var snapshot = SnapshotBuilder.Build(map, Fixture.Load("food-baklava"));

        Assert.Equal(SnapshotStatus.SchemaMismatch, snapshot.Status);
        Assert.Contains(ItemDetailMap.Name, snapshot.Mismatch);
        Assert.Empty(snapshot.Slots);
        Assert.Contains(snapshot.Extras, extra => Fixture.Plain(extra.Text) == "Baklava");
        Assert.Contains(snapshot.Extras, extra => extra.Path == PriceInsightPath);
    }

    [Fact]
    public void MissingNodeIsASchemaMismatch()
    {
        var nodes = Fixture.Load("food-baklava").Where(node => node.Path != "33").ToList();

        var snapshot = SnapshotBuilder.Build(ItemDetailMap.Map, nodes);

        Assert.Equal(SnapshotStatus.SchemaMismatch, snapshot.Status);
        Assert.Contains("33", snapshot.Mismatch);
    }

    [Fact]
    public void ExtrasAreOrderedTopToBottomThenLeftToRight()
    {
        var nodes = Fixture.Load("food-baklava");
        var template = nodes.Single(node => node.Path == PriceInsightPath);
        nodes.Add(template with { Path = "90001", NodeId = 90001, ScreenX = template.ScreenX + 100 });
        nodes.Add(template with { Path = "90002", NodeId = 90002, ScreenY = template.ScreenY - 10 });

        var snapshot = SnapshotBuilder.Build(ItemDetailMap.Map, nodes);

        Assert.Equal(["90002", PriceInsightPath, "90001"], snapshot.Extras.Select(extra => extra.Path));
    }

    [Fact]
    public void EmptyOrZeroSizeTextIsNotAnExtra()
    {
        var nodes = Fixture.Load("food-baklava");
        var template = nodes.Single(node => node.Path == PriceInsightPath);
        nodes.Add(template with { Path = "90001", NodeId = 90001, Text = [] });
        nodes.Add(template with { Path = "90002", NodeId = 90002, Width = 0 });

        var snapshot = SnapshotBuilder.Build(ItemDetailMap.Map, nodes);

        Assert.Equal([PriceInsightPath], snapshot.Extras.Select(extra => extra.Path));
    }

    private static TooltipSnapshot Build(string fixture) => SnapshotBuilder.Build(ItemDetailMap.Map, Fixture.Load(fixture));

    private static string Text(TooltipSnapshot snapshot, string slot) => Fixture.Plain(snapshot.Slots[slot].Text);
}
