using System.Numerics;
using Tipsy.Core.Layout;
using Xunit;

namespace Tipsy.Core.Tests.Layout;

public class RgbTests
{
    [Theory]
    [InlineData(0x000000u)]
    [InlineData(0xFFFFFFu)]
    [InlineData(0x72B7FFu)]
    [InlineData(0x17130Fu)]
    public void ColoursRoundTripThroughVectors(uint rgb)
    {
        Assert.Equal(rgb, Rgb.FromVector3(Rgb.ToVector3(rgb)));
    }

    [Fact]
    public void ChannelsAreRedGreenBlueFromHighToLowByte()
    {
        Assert.Equal(new Vector4(1, 0, 0x80 / 255f, 0.5f), Rgb.ToVector4(0xFF0080, 0.5f));
    }

    [Fact]
    public void HalfBlackOverWhiteIsMidGrey()
    {
        Assert.Equal(0x808080u, Rgb.Composite(0x000000, 0.5f, 0xFFFFFF));
    }

    [Fact]
    public void OpaqueColourHidesTheBackdrop()
    {
        Assert.Equal(0x123456u, Rgb.Composite(0x123456, 1f, 0xFFFFFF));
    }
}
