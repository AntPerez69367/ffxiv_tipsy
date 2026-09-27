using Tipsy.Core.Layout;
using Tipsy.Core.Text;
using Xunit;

namespace Tipsy.Core.Tests.Text;

public class SeTextTests
{
    [Fact]
    public void TextsWithTheSameBytesAreEqual()
    {
        var first = new SeText([0x02, 0x48, 0x41]);
        var second = new SeText([0x02, 0x48, 0x41]);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void TextsWithDifferentBytesAreNotEqual()
    {
        Assert.NotEqual(SeText.Utf8("Vitality"), SeText.Utf8("Vitality "));
    }

    [Fact]
    public void BlocksHoldingTheSameTextAreEqual()
    {
        Assert.Equal(new ParagraphBlock(SeText.Utf8("Body"), false), new ParagraphBlock(SeText.Utf8("Body"), false));
    }
}
