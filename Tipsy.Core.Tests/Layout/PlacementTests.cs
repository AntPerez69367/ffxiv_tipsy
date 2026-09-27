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
    public void CursorPlacementSitsBelowRightOfTheCursorWhenThereIsRoom()
    {
        var placed = Placement.Place(AnchorPreset.Cursor, WorkMin, WorkMax, 8, 400, 300, Vector2.Zero, new Vector2(500, 300), new Vector2(16, 16));

        Assert.Equal((new Vector2(516, 316), new Vector2(0, 0)), (placed.Position, placed.Pivot));
    }

    [Theory]
    [InlineData(1800, 300, 1784, 316, 1, 0)]
    [InlineData(500, 1000, 516, 984, 0, 1)]
    [InlineData(1800, 1000, 1784, 984, 1, 1)]
    public void CursorPlacementFlipsAwayFromTheEdgeSoTheCursorStaysUncovered(float cursorX, float cursorY, float x, float y, float pivotX, float pivotY)
    {
        var placed = Placement.Place(AnchorPreset.Cursor, WorkMin, WorkMax, 8, 400, 300, Vector2.Zero, new Vector2(cursorX, cursorY), new Vector2(16, 16));

        Assert.Equal((new Vector2(x, y), new Vector2(pivotX, pivotY)), (placed.Position, placed.Pivot));
        var left = placed.Position.X - (placed.Pivot.X * 400);
        var top = placed.Position.Y - (placed.Pivot.Y * 300);
        Assert.False(cursorX >= left && cursorX <= left + 400 && cursorY >= top && cursorY <= top + 300);
    }

    [Fact]
    public void CustomPositionIsClampedBackOnScreen()
    {
        var placed = Placement.Place(AnchorPreset.Custom, WorkMin, WorkMax, 8, 400, 300, new Vector2(5000, -50), Vector2.Zero, Vector2.Zero);

        Assert.Equal(new Vector2(1512, 8), placed.Position);
    }

    [Fact]
    public void WorkAreaSmallerThanTheInsetGivesNoNegativeHeight()
    {
        var placed = Placement.Place(AnchorPreset.TopLeft, Vector2.Zero, new Vector2(10, 10), 8, 400, 300, Vector2.Zero, Vector2.Zero, Vector2.Zero);

        Assert.Equal(0, placed.MaxHeight);
    }

    private static WindowPlacement Place(AnchorPreset anchor, float contentHeight) =>
        Placement.Place(anchor, WorkMin, WorkMax, 8, 400, contentHeight, Vector2.Zero, Vector2.Zero, Vector2.Zero);
}
