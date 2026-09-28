using Tipsy.Core.Layout;
using Tipsy.Core.Text;
using Tipsy.Core.Tooltips;
using Xunit;

namespace Tipsy.Core.Tests.Layout;

public class ActionTooltipLayoutTests
{
    [Theory]
    [InlineData("action-weaponskill")]
    [InlineData("action-role")]
    [InlineData("action-charges")]
    [InlineData("action-mount")]
    [InlineData("action-general")]
    public void RecordedActionsMatchTheMapWithNothingLeftInExtras(string fixture)
    {
        var snapshot = Snapshot(fixture);

        Assert.Equal(SnapshotStatus.Ok, snapshot.Status);
        Assert.Empty(snapshot.Extras);
    }

    [Fact]
    public void WeaponskillHasHeaderReachParamsDescriptionAndLevels()
    {
        var blocks = Layout("action-weaponskill");

        var header = Assert.IsType<HeaderBlock>(blocks[0]);
        Assert.Equal("Fell Cleave", Fixture.Plain(header.Name));
        Assert.Equal(["Weaponskill", "Range 3y   Radius 0y"], header.Lines.Select(Fixture.Plain));
        Assert.EndsWith(".tex", header.IconTexture);
        Assert.Equal([new ParamValue("Cast", "Instant", string.Empty), new ParamValue("Recast", "2.40s", string.Empty)], blocks.OfType<ParamsBlock>().Single().Params);
        Assert.Single(blocks.OfType<ParagraphBlock>());
        Assert.Equal(
            [("Acquired", "Lv. 54"), ("Affinity", "WAR")],
            blocks.OfType<KeyValueBlock>().Select(row => (row.Key, Fixture.Plain(row.Value))));
    }

    [Fact]
    public void ChargedAbilityKeepsTheGameLabel()
    {
        var parameters = Layout("action-charges").OfType<ParamsBlock>().Single().Params;

        Assert.Equal(new ParamValue("Charge Time", "60.00s", string.Empty), parameters[1]);
    }

    [Theory]
    [InlineData("action-mount", "Direwolf")]
    [InlineData("action-general", "Limit Break")]
    public void MountsAndGeneralActionsAreJustNameAndDescription(string fixture, string name)
    {
        var blocks = Layout(fixture);

        Assert.Equal(name, Fixture.Plain(Assert.IsType<HeaderBlock>(blocks[0]).Name));
        Assert.Equal([typeof(HeaderBlock), typeof(DividerBlock), typeof(ParagraphBlock)], blocks.Select(block => block.GetType()));
    }

    [Fact]
    public void RecastCountdownGoesOnTheIcon()
    {
        var snapshot = Snapshot("action-cooldown");

        Assert.Empty(snapshot.Extras);
        Assert.Equal("16", Assert.IsType<HeaderBlock>(ActionTooltipLayout.Build(snapshot)[0]).IconCooldown);
    }

    [Fact]
    public void KeybindFromTheTextTooltipJoinsTheHeader()
    {
        var blocks = SharedLayout.WithTextTooltip(Layout("action-role"), TextSnapshot("Rampart [`]"));

        Assert.Equal("`", Assert.IsType<HeaderBlock>(blocks[0]).Keybind);
    }

    [Fact]
    public void TextTooltipWithoutAKeyLeavesTheHeaderAlone()
    {
        var text = SnapshotBuilder.Build(TextTooltipMap.Map, Fixture.Load("text-no-keybind"));

        var blocks = SharedLayout.WithTextTooltip(Layout("action-role"), text);

        Assert.Equal(string.Empty, Assert.IsType<HeaderBlock>(blocks[0]).Keybind);
    }

    [Theory]
    [InlineData("Rampart [[]", "[")]
    [InlineData("Rampart []]", "]")]
    [InlineData("Rampart [Ctrl+[]", "Ctrl+[")]
    [InlineData("Rampart [ Shift+X ]", "Shift+X")]
    [InlineData("Rampart [Shift+Mouse\uE058]", "Shift+Mouse\uE058")]
    [InlineData("Rampart \uE03C [Ctrl+1]", "Ctrl+1")]
    public void KeybindIsTheBracketedTextAfterTheName(string tooltip, string key)
    {
        var blocks = SharedLayout.WithTextTooltip(Layout("action-role"), TextSnapshot(tooltip));

        Assert.Equal(key, Assert.IsType<HeaderBlock>(blocks[0]).Keybind);
    }

    [Theory]
    [InlineData("Rampart [ ]")]
    [InlineData("Rampart")]
    public void BlankKeyOrJustTheNameAddsNothing(string tooltip)
    {
        var layout = Layout("action-role");

        Assert.Same(layout, SharedLayout.WithTextTooltip(layout, TextSnapshot(tooltip)));
    }

    [Fact]
    public void OtherTextFromTheTextTooltipIsKeptAsAnExtra()
    {
        var blocks = SharedLayout.WithTextTooltip(Layout("action-role"), TextSnapshot("Summon [Carbuncle]"));

        Assert.Equal(string.Empty, Assert.IsType<HeaderBlock>(blocks[0]).Keybind);
        Assert.IsType<DividerBlock>(blocks[^2]);
        Assert.Equal("Summon [Carbuncle]", Fixture.Plain(Assert.IsType<ExtraBlock>(blocks[^1]).Text));
    }

    [Fact]
    public void LinesOtherPluginsAddToTheTextTooltipAreKeptAsExtras()
    {
        var text = TextSnapshot("Rampart [`]") with { Extras = [new ExtraLine("9", SeText.Utf8("Added by a plugin"), 0, 0)] };

        var blocks = SharedLayout.WithTextTooltip(Layout("action-role"), text);

        Assert.Equal("`", Assert.IsType<HeaderBlock>(blocks[0]).Keybind);
        Assert.IsType<DividerBlock>(blocks[^2]);
        Assert.Equal("Added by a plugin", Fixture.Plain(Assert.IsType<ExtraBlock>(blocks[^1]).Text));
    }

    [Fact]
    public void TextTooltipRepeatingAHeaderLineAddsNothing()
    {
        var layout = ItemTooltipLayout.Build(SnapshotBuilder.Build(ItemDetailMap.Map, Fixture.Load("gear-melded")));

        Assert.Same(layout, SharedLayout.WithTextTooltip(layout, TextSnapshot("Hands")));
    }

    private static TooltipSnapshot TextSnapshot(string text) =>
        new(TextTooltipMap.Addon, SnapshotStatus.Ok, null, new Dictionary<string, SlotValue> { [TextTooltipMap.Text] = new(SeText.Utf8(text), string.Empty, 0, default) }, []);

    private static TooltipSnapshot Snapshot(string fixture) => SnapshotBuilder.Build(ActionDetailMap.Map, Fixture.Load(fixture));

    private static List<TooltipBlock> Layout(string fixture) => ActionTooltipLayout.Build(Snapshot(fixture));
}
