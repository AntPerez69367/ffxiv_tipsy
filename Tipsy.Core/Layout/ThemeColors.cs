namespace Tipsy.Core.Layout;

/// <summary>Theme colours as 0xRRGGBB with separate alphas.</summary>
public sealed record ThemeColors(
    uint Surface,
    float SurfaceAlpha,
    uint Border,
    uint PrimaryText,
    uint SecondaryText,
    uint Accent,
    uint Divider,
    float DividerAlpha,
    uint Better,
    uint Worse)
{
    public static readonly ThemeColors Native = new(0x17130F, 0.95f, 0x8E7650, 0xF0DFC0, 0xB8A98D, 0xD8B86A, 0xA98D59, 0.25f, 0x8FD694, 0xE0826E);
}
