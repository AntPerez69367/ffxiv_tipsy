using Tipsy.Core.Text;
using Xunit;

namespace Tipsy.Core.Tests.Text;

public class SeStringTextTests
{
    [Fact]
    public void PayloadsAreSkipped()
    {
        var bytes = Convert.FromHexString("024804F2022503024904F20226034D61782D506F74696F6E20EE80BC02490201030248020103");

        Assert.Equal("Max-Potion ", SeStringText.Plain(bytes));
    }

    [Fact]
    public void PayloadLengthsWrittenAsMultiByteIntegersAreSkipped()
    {
        var body = new byte[300];
        var bytes = new List<byte> { 0x41, 0x02, 0x27, 0xF2, 0x01, 0x2C };
        bytes.AddRange(body);
        bytes.Add(0x03);
        bytes.Add(0x42);

        Assert.Equal("AB", SeStringText.Plain(bytes.ToArray()));
    }

    [Fact]
    public void SingleLineReplacesLineBreaksAndKeepsOtherPayloads()
    {
        var bytes = Convert.FromHexString("024804F2022903" + "4F66" + "0210010346" + "4F6620" + "02100103" + "46" + "02490201030248020103");

        Assert.Equal(
            new SeText(Convert.FromHexString("024804F2022903" + "4F66" + "2046" + "4F6620" + "46" + "02490201030248020103")),
            SeStringText.SingleLine(new SeText(bytes)));
    }

    [Theory]
    [InlineData("02")]
    [InlineData("4102")]
    [InlineData("410248")]
    [InlineData("0248F201")]
    [InlineData("410248F7FF42")]
    [InlineData("4102481001")]
    public void TruncatedPayloadsEndTheTextWithoutThrowing(string hex)
    {
        var plain = SeStringText.Plain(Convert.FromHexString(hex));

        Assert.True(plain is "" or "A");
    }

    [Fact]
    public async Task LengthWithItsTopBitSetEndsTheTextInsteadOfLooping()
    {
        var plain = await Task.Run(() => SeStringText.Plain(Convert.FromHexString("410248FEFFFFFFF842"))).WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal("A", plain);
    }
}
