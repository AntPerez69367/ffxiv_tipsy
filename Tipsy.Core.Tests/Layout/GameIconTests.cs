using Tipsy.Core.Layout;
using Xunit;

namespace Tipsy.Core.Tests.Layout;

public class GameIconTests
{
    [Theory]
    [InlineData("ui/icon/037000/037810_hr1.tex", 37810u, false)]
    [InlineData("ui/icon/037000/037810.tex", 37810u, false)]
    [InlineData("ui/icon/020000/hq/020654_hr1.tex", 20654u, true)]
    public void IconTexturesGiveTheirIdAndQuality(string texture, uint id, bool highQuality)
    {
        Assert.Equal(new GameIcon(id, highQuality), GameIcon.FromTexture(texture));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ui/uld/ItemDetail_hr1.tex")]
    public void OtherTexturesAreNotIcons(string texture)
    {
        Assert.Null(GameIcon.FromTexture(texture));
    }
}
