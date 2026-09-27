using Tipsy.Core.Layout;
using Tipsy.Core.Text;
using Tipsy.Core.Tooltips;
using Xunit;

namespace Tipsy.Core.Tests.Layout;

public class EquatableListTests
{
    [Fact]
    public void ListsWithTheSameItemsInOrderAreEqual()
    {
        EquatableList<int> first = [1, 2, 3];
        EquatableList<int> second = [1, 2, 3];

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void ListsWithTheSameItemsInAnotherOrderAreNotEqual()
    {
        EquatableList<int> first = [1, 2, 3];
        EquatableList<int> second = [3, 2, 1];

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void HeadersBuiltFromTheSameContentAreEqual()
    {
        Assert.Equal(Header(), Header());
    }

    [Fact]
    public void LaidOutTooltipsFromTheSameDumpAreEqual()
    {
        var nodes = Fixture.Load("gear-grimoire");

        Assert.Equal(
            ItemTooltipLayout.Build(SnapshotBuilder.Build(ItemDetailMap.Map, nodes)),
            ItemTooltipLayout.Build(SnapshotBuilder.Build(ItemDetailMap.Map, nodes)));
    }

    private static HeaderBlock Header() =>
        new(null, string.Empty, SeText.Utf8("Rampart"), [SeText.Utf8("Ability")], [SeText.Utf8("Unique")]);
}
