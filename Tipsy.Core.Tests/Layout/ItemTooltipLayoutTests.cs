using Tipsy.Core.Layout;
using Tipsy.Core.Tooltips;
using Xunit;

namespace Tipsy.Core.Tests.Layout;

public class ItemTooltipLayoutTests
{
    [Fact]
    public void GearComesOutInTheSpecOrder()
    {
        var blocks = Layout("gear-grimoire");

        Assert.Equal(
            [
                typeof(HeaderBlock), typeof(DividerBlock), typeof(ParamsBlock),
                typeof(CaptionBlock), typeof(StatTableBlock),
                typeof(CaptionBlock), typeof(MateriaBlock),
                typeof(CaptionBlock), typeof(IconTextBlock), typeof(BarBlock), typeof(BarBlock),
                typeof(KeyValueBlock), typeof(KeyValueBlock), typeof(KeyValueBlock), typeof(KeyValueBlock), typeof(ParagraphBlock),
                typeof(ParagraphBlock), typeof(ParagraphBlock), typeof(ParagraphBlock), typeof(ParagraphBlock),
            ],
            blocks.Select(block => block.GetType()));
    }

    [Fact]
    public void TheRepairJobIconAndLevelFollowTheRepairsCaption()
    {
        var blocks = Layout("gear-grimoire");

        var caption = blocks.FindIndex(block => block is CaptionBlock { Text: "CRAFTING & REPAIRS" });
        var job = Assert.IsType<IconTextBlock>(blocks[caption + 1]);
        Assert.Equal("ui/icon/062000/062114_hr1.tex", job.Icon.Texture);
        Assert.Equal("90", Fixture.Plain(job.Text));
    }

    [Fact]
    public void StorageIconsBecomeHeaderBadges()
    {
        var header = Assert.IsType<HeaderBlock>(Layout("gear-compared")[0]);

        Assert.Equal(3, header.Badges.Count);
        Assert.All(header.Badges, badge => Assert.Equal("ui/uld/ItemDetailPutIn_hr1.tex", badge.Texture));
    }

    [Fact]
    public void TheRecipeIconBecomesAHeaderBadge()
    {
        var header = Assert.IsType<HeaderBlock>(Layout("card-chimera")[0]);

        Assert.Contains(header.Badges, badge => badge.Texture == "ui/uld/RecipeNoteBook_hr1.tex");
    }

    [Fact]
    public void TheControlHintIsTheLastParagraphBeforeTheExtras()
    {
        var blocks = Layout("food-baklava");

        var divider = blocks.FindLastIndex(block => block is DividerBlock);
        var hint = Assert.IsType<ParagraphBlock>(blocks[divider - 1]);
        Assert.True(hint.Secondary);
        Assert.StartsWith("Ctrl Key", Fixture.Plain(hint.Text));
    }

    [Fact]
    public void GearHeaderStatsAndBarsCarryTheirValues()
    {
        var blocks = Layout("gear-grimoire");

        var header = Assert.IsType<HeaderBlock>(blocks[0]);
        Assert.Equal("Zormor Grimoire", Fixture.Plain(header.Name));
        Assert.Equal(["Arcanist's Grimoire", "Item Level 660", "Lv. 93", "ACN SMN"], header.Lines.Select(Fixture.Plain));
        Assert.Equal(["Unique", "Untradable", "(Total: 1)"], header.Flags.Select(Fixture.Plain));
        Assert.Equal("ui/icon/037000/037810_hr1.tex", header.IconTexture);
        Assert.Equal(new ParamValue("Magic Damage", "131", string.Empty), blocks.OfType<ParamsBlock>().Single().Params[0]);
        Assert.Equal(
            [new LabelledValue("Vitality", "+410"), new LabelledValue("Intelligence", "+409"), new LabelledValue("Determination", "+212"), new LabelledValue("Direct Hit Rate", "+303")],
            blocks.OfType<StatTableBlock>().Single().Stats);
        Assert.Equal(["BONUSES", "MATERIA", "CRAFTING & REPAIRS"], blocks.OfType<CaptionBlock>().Select(caption => caption.Text));
        var condition = blocks.OfType<BarBlock>().First();
        Assert.Equal(("Condition", "100%", 1f), (condition.Label, condition.Value, condition.Fraction));
        Assert.Equal(0f, blocks.OfType<BarBlock>().Last().Fraction);
    }

    [Fact]
    public void FoodShowsEffectsAndEndsWithTheInjectedLineAsAnExtra()
    {
        var blocks = Layout("food-baklava");

        Assert.Contains(blocks, block => block is CaptionBlock { Text: "EFFECTS" });
        Assert.IsType<DividerBlock>(blocks[^2]);
        Assert.StartsWith("Marketboard Price:", Fixture.Plain(Assert.IsType<ExtraBlock>(blocks[^1]).Text));
        Assert.Equal(2, blocks.OfType<DividerBlock>().Count());
    }

    [Fact]
    public void TheGamesLineBreakInALongNameIsDropped()
    {
        var header = Assert.IsType<HeaderBlock>(Layout("gear-melded")[0]);

        Assert.Equal(-1, header.Name.Bytes.AsSpan().IndexOf([(byte)0x02, (byte)0x10, (byte)0x01, (byte)0x03]));
        Assert.Equal("Augmented Lunar Envoy's Gloves of Fending", Fixture.Plain(header.Name));
    }

    [Fact]
    public void ComparisonWithEquippedGearStaysWithItsParam()
    {
        var blocks = Layout("gear-compared");

        var parameters = blocks.OfType<ParamsBlock>().Single().Params;
        Assert.Equal([new ParamValue("Defense", "478", "(-705)"), new ParamValue("Magic Defense", "837", "(-346)")], parameters);
        Assert.DoesNotContain(blocks, block => block is ExtraBlock);
    }

    [Fact]
    public void MeldedMateriaAreNamedWithTheirEffect()
    {
        var materia = Layout("gear-melded").OfType<MateriaBlock>().Single().Materia;

        Assert.Equal("Savage Might Materia IX", materia[0].Label);
        Assert.All(materia, row => Assert.NotEqual(string.Empty, row.Label));
        Assert.All(materia, row => Assert.Matches(@"^\w[\w ]* \+\d+$", row.Value));
    }

    [Fact]
    public void BindingIsAHeaderFlag()
    {
        var header = Assert.IsType<HeaderBlock>(Layout("hq-gear-binding")[0]);

        Assert.Contains("Binding", header.Flags.Select(Fixture.Plain));
    }

    [Fact]
    public void RequirementsGetTheirOwnSection()
    {
        var blocks = Layout("requirements");

        var caption = blocks.FindIndex(block => block is CaptionBlock { Text: "REQUIREMENTS" });
        Assert.True(caption >= 0);
        var row = Assert.IsType<KeyValueBlock>(blocks[caption + 1]);
        Assert.Equal(("Base Item", "Item Level 160"), (row.Key, Fixture.Plain(row.Value)));
    }

    [Fact]
    public void RecastCountdownGoesOnTheIconNotIntoExtras()
    {
        var blocks = Layout("hq-max-potion-cooldown");

        Assert.Equal("20", Assert.IsType<HeaderBlock>(blocks[0]).IconCooldown);
        Assert.DoesNotContain(blocks, block => block is ParagraphBlock paragraph && Fixture.Plain(paragraph.Text) == "20");
    }

    [Fact]
    public void TreeWithNothingShownLaysOutNothing()
    {
        Assert.Empty(Layout("closed-baklava"));
    }

    [Fact]
    public void MismatchFallsBackToAWarningAndPlainLines()
    {
        var map = ItemDetailMap.Map with { Slots = [.. ItemDetailMap.Map.Slots, new SlotBinding("Gone", "999999", "Text")] };
        var snapshot = SnapshotBuilder.Build(map, Fixture.Load("food-baklava"));

        var blocks = ItemTooltipLayout.Build(snapshot);

        Assert.IsType<WarningBlock>(blocks[0]);
        Assert.All(blocks.Skip(1), block => Assert.IsType<ParagraphBlock>(block));
        Assert.Equal(snapshot.Extras.Count, blocks.Count - 1);
    }

    [Theory]
    [InlineData("Vitality +410", "Vitality", "+410")]
    [InlineData("Direct Hit Rate +303", "Direct Hit Rate", "+303")]
    [InlineData("Movement Speed -5", "Movement Speed", "-5")]
    [InlineData("Unique", "Unique", "")]
    [InlineData("Grants Resistance", "Grants Resistance", "")]
    [InlineData("Vitality \uFF0B410", "Vitality", "\uFF0B410")]
    [InlineData("Vitalit\u00E9\u00A0+410", "Vitalit\u00E9", "+410")]
    [InlineData("Vitality +410 ", "Vitality", "+410")]
    public void StatsSplitIntoNameAndAmount(string stat, string label, string value)
    {
        Assert.Equal(new LabelledValue(label, value), ItemTooltipLayout.SplitStat(stat));
    }

    [Theory]
    [InlineData("gear-grimoire")]
    [InlineData("gear-wristguards")]
    [InlineData("gear-compared")]
    [InlineData("gear-melded")]
    [InlineData("food-baklava")]
    [InlineData("card-chimera")]
    [InlineData("hq-max-potion")]
    [InlineData("hq-max-potion-cooldown")]
    [InlineData("hq-gear-binding")]
    [InlineData("requirements")]
    public void RecordedItemsMatchTheMapWithOnlyPluginLinesInExtras(string fixture)
    {
        var snapshot = SnapshotBuilder.Build(ItemDetailMap.Map, Fixture.Load(fixture));

        Assert.Equal(SnapshotStatus.Ok, snapshot.Status);
        Assert.All(snapshot.Extras, extra => Assert.Equal("32612", extra.Path));
    }

    [Theory]
    [InlineData("100%", 1f)]
    [InlineData("42%", 0.42f)]
    [InlineData("99,5%", 0.995f)]
    [InlineData("99.5 %", 0.995f)]
    [InlineData("150%", 1f)]
    [InlineData("0%", 0f)]
    public void PercentagesParseInEveryClientFormat(string value, float fraction)
    {
        Assert.Equal(fraction, ItemTooltipLayout.Percent(value)!.Value, 3);
    }

    [Theory]
    [InlineData("Grade 8 Dark Matter")]
    [InlineData("100")]
    public void TextWithoutAPercentSignIsNotABar(string value)
    {
        Assert.Null(ItemTooltipLayout.Percent(value));
    }

    private static List<TooltipBlock> Layout(string fixture) => ItemTooltipLayout.Build(SnapshotBuilder.Build(ItemDetailMap.Map, Fixture.Load(fixture)));
}
