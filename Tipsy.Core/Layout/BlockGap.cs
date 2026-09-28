namespace Tipsy.Core.Layout;

public static class BlockGap
{
    /// <summary>The vertical space drawn above <paramref name="block"/> when <paramref name="previous"/> comes right before it.</summary>
    public static float Before(TooltipBlock? previous, TooltipBlock block, LayoutTokens tokens) => block switch
    {
        _ when previous is null => 0,
        DividerBlock => 0,
        _ when previous is DividerBlock => 0,
        _ when previous is CaptionBlock => tokens.CaptionGap,
        CaptionBlock or ParamsBlock => tokens.SectionGap,
        ParagraphBlock when previous is not ParagraphBlock => tokens.SectionGap,
        ExtraBlock when previous is not ExtraBlock => tokens.SectionGap,
        _ => tokens.RowGap,
    };
}
