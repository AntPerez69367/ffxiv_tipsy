using Tipsy.Core.Layout;
using Tipsy.Core.Tooltips;
using Xunit;

namespace Tipsy.Core.Tests.Layout;

public class TextTooltipLayoutTests
{
    [Theory]
    [InlineData("text-short")]
    [InlineData("text-defense")]
    [InlineData("text-coloured")]
    [InlineData("text-keybind")]
    [InlineData("text-no-keybind")]
    public void RecordedTextTooltipsMatchTheMap(string fixture)
    {
        Assert.Equal(SnapshotStatus.Ok, Snapshot(fixture).Status);
    }

    [Fact]
    public void ShortTooltipIsOneParagraph()
    {
        var paragraph = Assert.IsType<ParagraphBlock>(Assert.Single(Layout("text-short")));

        Assert.Equal("Body", Fixture.Plain(paragraph.Text));
    }

    [Fact]
    public void TextAnotherPluginAppendsStaysInTheParagraph()
    {
        var paragraph = Assert.IsType<ParagraphBlock>(Assert.Single(Layout("text-defense")));

        Assert.Contains("You are currently wasting 69 points.", Fixture.Plain(paragraph.Text));
    }

    [Fact]
    public void ColourPayloadsSurvive()
    {
        var paragraph = Assert.IsType<ParagraphBlock>(Assert.Single(Layout("text-coloured")));

        Assert.Equal(0x02, paragraph.Text.Bytes[0]);
        Assert.StartsWith("Skill/Spell Speed shortens", Fixture.Plain(paragraph.Text));
    }

    [Fact]
    public void MismatchFallsBackToPlainLines()
    {
        var map = TextTooltipMap.Map with { Slots = [new SlotBinding(TextTooltipMap.Text, "2", "Image")] };

        var blocks = TextTooltipLayout.Build(SnapshotBuilder.Build(map, Fixture.Load("text-short")));

        Assert.IsType<WarningBlock>(blocks[0]);
        Assert.Equal("Body", Fixture.Plain(Assert.IsType<ParagraphBlock>(blocks[1]).Text));
    }

    private static TooltipSnapshot Snapshot(string fixture) => SnapshotBuilder.Build(TextTooltipMap.Map, Fixture.Load(fixture));

    private static List<TooltipBlock> Layout(string fixture) => TextTooltipLayout.Build(Snapshot(fixture));
}
