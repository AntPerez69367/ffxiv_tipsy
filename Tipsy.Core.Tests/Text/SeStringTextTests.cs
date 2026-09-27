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
}
