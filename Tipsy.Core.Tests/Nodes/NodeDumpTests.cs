using System.Text;
using Tipsy.Core.Nodes;
using Xunit;

namespace Tipsy.Core.Tests.Nodes;

public class NodeDumpTests
{
    [Fact]
    public void ComponentChildrenGetTheComponentInTheirPath()
    {
        var nodes = Fixture.Load("gear-grimoire");

        var icon = Assert.Single(nodes, node => node.Path == "32/12");
        Assert.Equal("Image", icon.Type);
        Assert.Equal(1, icon.Depth);
        Assert.Equal("ui/icon/037000/037810_hr1.tex", icon.Texture);
        Assert.Contains(nodes, node => node.Path == "12" && node.Type == "Res");
    }

    [Fact]
    public void NonComponentParentsStayOutOfThePath()
    {
        var nodes = Fixture.Load("gear-grimoire");

        Assert.Contains(nodes, node => node.Path == "96/6" && node.Type == "Text");
        Assert.DoesNotContain(nodes, node => node.Path == "96/4/6");
    }

    [Fact]
    public void WrittenRowsReadBackUnchanged()
    {
        var nodes = Fixture.Load("food-baklava");
        var output = new StringWriter();
        output.WriteLine("header");
        output.WriteLine("header");
        output.WriteLine(NodeDump.ColumnHeader);
        NodeDump.WriteRows(nodes, bytes => Encoding.UTF8.GetString(bytes), output);

        var again = NodeDump.Read(new StringReader(output.ToString()));

        Assert.Equal(nodes.Count, again.Count);
        for (var i = 0; i < nodes.Count; i++)
        {
            Assert.Equal(nodes[i] with { Text = [] }, again[i] with { Text = [] });
            Assert.Equal(nodes[i].Text, again[i].Text);
        }
    }

    [Theory]
    [InlineData("0\t1\tRes")]
    [InlineData("2\t1\tRes\t0\t1\t1\t0\t0\t0\t0\t10\t10\t1\t1\t0\t\t\t")]
    public void MalformedRowsAreReportedWithTheirLine(string row)
    {
        var dump = $"header\nheader\n{NodeDump.ColumnHeader}\n{row}\n";

        var error = Assert.Throws<FormatException>(() => NodeDump.Read(new StringReader(dump)));

        Assert.StartsWith("Line 4", error.Message);
    }
}
