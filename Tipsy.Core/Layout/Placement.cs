using System.Numerics;

namespace Tipsy.Core.Layout;

public enum AnchorPreset
{
    TopLeft,
    TopCenter,
    TopRight,
    BottomLeft,
    BottomRight,
    Custom,
    Cursor,
}

/// <summary>Where to put the window this frame: the position, the pivot it grows away from, and its height cap.</summary>
public readonly record struct WindowPlacement(Vector2 Position, Vector2 Pivot, float MaxHeight, bool Overflowing);

public static class Placement
{
    /// <summary>
    /// Places a window of <paramref name="width"/> and last frame's <paramref name="contentHeight"/> inside the work
    /// area. The pivot sits on the anchor, so the window grows away from it; when the content is taller than the work
    /// area, the pivot moves to the top and the height is capped. Following the cursor, the window sits below and right
    /// of it, and flips to the other side of the cursor on an axis where it would run off the work area.
    /// </summary>
    public static WindowPlacement Place(AnchorPreset anchor, Vector2 workMin, Vector2 workMax, float inset, float width, float contentHeight, Vector2 custom, Vector2 cursor, Vector2 cursorOffset)
    {
        var min = workMin + new Vector2(inset);
        var max = workMax - new Vector2(inset);
        var maxHeight = Math.Max(0, max.Y - min.Y);
        var (position, pivot) = anchor switch
        {
            AnchorPreset.TopLeft => (min, new Vector2(0, 0)),
            AnchorPreset.TopCenter => (new Vector2((min.X + max.X) / 2, min.Y), new Vector2(0.5f, 0)),
            AnchorPreset.TopRight => (new Vector2(max.X, min.Y), new Vector2(1, 0)),
            AnchorPreset.BottomLeft => (new Vector2(min.X, max.Y), new Vector2(0, 1)),
            AnchorPreset.BottomRight => (max, new Vector2(1, 1)),
            AnchorPreset.Custom => (custom, new Vector2(0, 0)),
            _ => BesideCursor(cursor, cursorOffset, max, width, Math.Min(contentHeight, maxHeight)),
        };

        var overflowing = contentHeight > maxHeight;
        if (overflowing)
        {
            position.Y = min.Y;
            pivot.Y = 0;
        }

        var height = Math.Min(contentHeight, maxHeight);
        var left = position.X - (pivot.X * width);
        var top = position.Y - (pivot.Y * height);
        left = Math.Clamp(left, min.X, Math.Max(min.X, max.X - width));
        top = Math.Clamp(top, min.Y, Math.Max(min.Y, max.Y - height));
        return new WindowPlacement(new Vector2(left + (pivot.X * width), top + (pivot.Y * height)), pivot, maxHeight, overflowing);
    }

    private static (Vector2 Position, Vector2 Pivot) BesideCursor(Vector2 cursor, Vector2 offset, Vector2 max, float width, float height)
    {
        var flipX = cursor.X + offset.X + width > max.X;
        var flipY = cursor.Y + offset.Y + height > max.Y;
        return (
            new Vector2(flipX ? cursor.X - offset.X : cursor.X + offset.X, flipY ? cursor.Y - offset.Y : cursor.Y + offset.Y),
            new Vector2(flipX ? 1 : 0, flipY ? 1 : 0));
    }
}
