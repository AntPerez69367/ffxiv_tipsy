using Tipsy.Core.Text;
using Xunit;

namespace Tipsy.Core.Tests.Text;

public class KeyLabelsTests
{
    [Fact]
    public void TextWithoutGlyphsIsUnchanged()
    {
        Assert.Equal("Shift+1", KeyLabels.Readable("Shift+1"));
    }

    [Fact]
    public void MouseFollowedByAButtonGlyphReadsAsOneButton()
    {
        Assert.Equal("Shift+Mouse 5", KeyLabels.Readable("Shift+"));
    }

    [Fact]
    public void ClickGlyphsAreSpelledOut()
    {
        Assert.Equal("Ctrl+Right Click", KeyLabels.Readable("Ctrl+"));
    }
}
