using System.Numerics;

namespace Tipsy.Core.Layout;

/// <summary>Converts 0xRRGGBB colours to and from 0–1 channel vectors.</summary>
public static class Rgb
{
    public static Vector3 ToVector3(uint rgb) =>
        new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);

    public static Vector4 ToVector4(uint rgb, float alpha = 1f) => new(ToVector3(rgb), alpha);

    public static uint FromVector3(Vector3 colour) =>
        (Channel(colour.X) << 16) | (Channel(colour.Y) << 8) | Channel(colour.Z);

    /// <summary><paramref name="colour"/> at <paramref name="alpha"/> drawn over an opaque <paramref name="backdrop"/>.</summary>
    public static uint Composite(uint colour, float alpha, uint backdrop) =>
        FromVector3((ToVector3(colour) * alpha) + (ToVector3(backdrop) * (1 - alpha)));

    private static uint Channel(float value) => (uint)MathF.Round(value * 255);
}
