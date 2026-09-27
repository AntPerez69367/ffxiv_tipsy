using Tipsy.Core.Layout;
using Tipsy.Core.Text;
using Xunit;

namespace Tipsy.Core.Tests.Layout;

public class BlockGapTests
{
    private static readonly LayoutTokens Tokens = new();
    private static readonly ParagraphBlock Paragraph = new(SeText.Utf8("Body"), false);
    private static readonly ExtraBlock Extra = new(SeText.Utf8("Extra"));
    private static readonly CaptionBlock Caption = new("BONUSES");
    private static readonly DividerBlock Divider = new();

    [Fact]
    public void TheFirstBlockHasNoGap()
    {
        Assert.Equal(0, BlockGap.Before(null, Paragraph, Tokens));
    }

    [Fact]
    public void DividersHaveNoGapOnEitherSide()
    {
        Assert.Equal(0, BlockGap.Before(Paragraph, Divider, Tokens));
        Assert.Equal(0, BlockGap.Before(Divider, Paragraph, Tokens));
    }

    [Fact]
    public void ABlockUnderACaptionSitsCloseToIt()
    {
        Assert.Equal(Tokens.CaptionGap, BlockGap.Before(Caption, Paragraph, Tokens));
    }

    [Fact]
    public void ACaptionStartsASection()
    {
        Assert.Equal(Tokens.SectionGap, BlockGap.Before(Paragraph, Caption, Tokens));
    }

    [Fact]
    public void ParagraphsInARowAreRowsAndTheFirstStartsASection()
    {
        Assert.Equal(Tokens.RowGap, BlockGap.Before(Paragraph, Paragraph, Tokens));
        Assert.Equal(Tokens.SectionGap, BlockGap.Before(Extra, Paragraph, Tokens));
    }

    [Fact]
    public void ExtrasInARowAreRowsAndTheFirstStartsASection()
    {
        Assert.Equal(Tokens.RowGap, BlockGap.Before(Extra, Extra, Tokens));
        Assert.Equal(Tokens.SectionGap, BlockGap.Before(Paragraph, Extra, Tokens));
    }
}
