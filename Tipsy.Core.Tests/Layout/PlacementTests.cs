using System.Numerics;
using Tipsy.Core.Layout;
using Xunit;

namespace Tipsy.Core.Tests.Layout;

public class PlacementTests
{
    private static readonly Vector2 WorkMin = new(0, 0);
    private static readonly Vector2 WorkMax = new(1920, 1080);

    [Theory]
    [InlineData(AnchorPreset.TopLeft, 8, 8, 0, 0)]
    [InlineData(AnchorPreset.TopCenter, 960, 8, 0.5f, 0)]
    [InlineData(AnchorPreset.TopRight, 1912, 8, 1, 0)]
    [InlineData(AnchorPreset.BottomLeft, 8, 1072, 0, 1)]
    [InlineData(AnchorPreset.BottomRight, 1912, 1072, 1, 1)]
    public void PresetsPinTheMatchingCornerInsideTheInset(AnchorPreset anchor, float x, float y, float pivotX, float pivotY)
    {
        var placed = Place(anchor, 300);

        Assert.Equal(new Vector2(x, y), placed.Position);
        Assert.Equal(new Vector2(pivotX, pivotY), placed.Pivot);
        Assert.False(placed.Overflowing);
    }

    [Fact]
    public void TallContentMovesThePivotToTheTopAndCapsTheHeight()
    {
        var placed = Place(AnchorPreset.BottomRight, 1500);

        Assert.True(placed.Overflowing);
        Assert.Equal(0, placed.Pivot.Y);
        Assert.Equal(8, placed.Position.Y);
        Assert.Equal(1064, placed.MaxHeight);
    }

    [Fact]
    public void CursorPlacementIsClampedToTheWorkArea()
    {
        var placed = Placement.Place(AnchorPreset.Cursor, WorkMin, WorkMax, 8, 400, 300, Vector2.Zero, new Vector2(1800, 1000), new Vector2(16, 16));

        Assert.Equal(new Vector2(1512, 772), placed.Position);
    }

    private static WindowPlacement Place(AnchorPreset anchor, float contentHeight) =>
        Placement.Place(anchor, WorkMin, WorkMax, 8, 400, contentHeight, Vector2.Zero, Vector2.Zero, Vector2.Zero);
}
