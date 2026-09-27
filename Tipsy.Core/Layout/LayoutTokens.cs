namespace Tipsy.Core.Layout;

/// <summary>Spacing and sizes of the tooltip window in pixels at scale 1.</summary>
public sealed record LayoutTokens
{
    public float Padding { get; init; } = 16;
    public float SectionGap { get; init; } = 12;
    public float CaptionGap { get; init; } = 6;
    public float RowGap { get; init; } = 4;
    public float InlineGap { get; init; } = 8;
    public float DividerMargin { get; init; } = 8;
    public float IconTextGap { get; init; } = 12;
    public float IconSize { get; init; } = 48;
    public float MateriaIconSize { get; init; } = 20;
    public float Width { get; init; } = 400;
    public float MinWidth { get; init; } = 320;
    public float MaxWidth { get; init; } = 560;
    public float BarHeight { get; init; } = 6;
    public float BarRounding { get; init; } = 3;
    public float OverflowFade { get; init; } = 24;
    public float ViewportInset { get; init; } = 8;
    public float Rounding { get; init; } = 6;

    public float WrapWidth => Width - (2 * Padding);

    public LayoutTokens Scaled(float scale) => new()
    {
        Padding = Padding * scale,
        SectionGap = SectionGap * scale,
        CaptionGap = CaptionGap * scale,
        RowGap = RowGap * scale,
        InlineGap = InlineGap * scale,
        DividerMargin = DividerMargin * scale,
        IconTextGap = IconTextGap * scale,
        IconSize = IconSize * scale,
        MateriaIconSize = MateriaIconSize * scale,
        Width = Width * scale,
        MinWidth = MinWidth * scale,
        MaxWidth = MaxWidth * scale,
        BarHeight = BarHeight * scale,
        BarRounding = BarRounding * scale,
        OverflowFade = OverflowFade * scale,
        ViewportInset = ViewportInset * scale,
        Rounding = Rounding * scale,
    };
}
