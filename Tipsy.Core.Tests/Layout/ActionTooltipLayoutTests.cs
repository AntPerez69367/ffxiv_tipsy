using Tipsy.Core.Layout;
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
        var text = SnapshotBuilder.Build(TextTooltipMap.Map, Fixture.Load("text-keybind"));

        var blocks = SharedLayout.WithKeybind(Layout("action-role"), text);

        Assert.Equal("`", Assert.IsType<HeaderBlock>(blocks[0]).Keybind);
    }

    [Fact]
    public void TextTooltipWithoutAKeyLeavesTheHeaderAlone()
    {
        var text = SnapshotBuilder.Build(TextTooltipMap.Map, Fixture.Load("text-no-keybind"));

        var blocks = SharedLayout.WithKeybind(Layout("action-role"), text);

        Assert.Equal(string.Empty, Assert.IsType<HeaderBlock>(blocks[0]).Keybind);
    }

    private static TooltipSnapshot Snapshot(string fixture) => SnapshotBuilder.Build(ActionDetailMap.Map, Fixture.Load(fixture));

    private static List<TooltipBlock> Layout(string fixture) => ActionTooltipLayout.Build(Snapshot(fixture));
}
