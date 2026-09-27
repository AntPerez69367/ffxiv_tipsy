using System.Globalization;

namespace Tipsy.Core.Nodes;

/// <summary>
/// Reads and writes the probe's tab-separated node dumps: two header lines, a column header, then one row per
/// node in tree order with each component's children directly after it. The last column is a readable copy of
/// the text and is ignored when reading; the hex column holds the exact bytes.
/// </summary>
public static class NodeDump
{
    public const string ColumnHeader = "depth\tid\ttype\tparent\tvisible\tshown\tx\ty\tscreenX\tscreenY\twidth\theight\tscaleX\tscaleY\tpartId\ttexture\ttextHex\ttext";

    private const int HeaderLines = 3;
    private const string ComponentPrefix = "Component";

    public static List<NodeRecord> Read(TextReader input)
    {
        for (var i = 0; i < HeaderLines; i++)
            input.ReadLine();

        var records = new List<NodeRecord>();
        var components = new List<uint>();
        while (input.ReadLine() is { } line)
        {
            if (line.Length == 0)
                continue;
            var cells = line.Split('\t');
            var depth = int.Parse(cells[0], CultureInfo.InvariantCulture);
            var nodeId = uint.Parse(cells[1], CultureInfo.InvariantCulture);
            var type = cells[2];
            components.RemoveRange(depth, components.Count - depth);
            var path = PathOf(components, nodeId);
            if (type.StartsWith(ComponentPrefix, StringComparison.Ordinal))
                components.Add(nodeId);

            records.Add(new NodeRecord(
                depth,
                path,
                nodeId,
                type,
                uint.Parse(cells[3], CultureInfo.InvariantCulture),
                cells[4] == "1",
                cells[5] == "1",
                Number(cells[6]),
                Number(cells[7]),
                Number(cells[8]),
                Number(cells[9]),
                Number(cells[10]),
                Number(cells[11]),
                Number(cells[12]),
                Number(cells[13]),
                int.Parse(cells[14], CultureInfo.InvariantCulture),
                cells[15],
                Convert.FromHexString(cells[16])));
        }

        return records;
    }

    public static void WriteRows(IEnumerable<NodeRecord> records, Func<byte[], string> decode, TextWriter output)
    {
        foreach (var node in records)
        {
            var text = node.Text.Length == 0 ? string.Empty : decode(node.Text).ReplaceLineEndings("\\n").Replace("\t", "\\t");
            output.WriteLine(string.Join('\t',
                node.Depth.ToString(CultureInfo.InvariantCulture),
                node.NodeId.ToString(CultureInfo.InvariantCulture),
                node.Type,
                node.ParentId.ToString(CultureInfo.InvariantCulture),
                node.Visible ? "1" : "0",
                node.Shown ? "1" : "0",
                Format(node.X),
                Format(node.Y),
                Format(node.ScreenX),
                Format(node.ScreenY),
                Format(node.Width),
                Format(node.Height),
                Format(node.ScaleX),
                Format(node.ScaleY),
                node.PartId.ToString(CultureInfo.InvariantCulture),
                node.Texture,
                Convert.ToHexString(node.Text),
                text));
        }
    }

    public static string PathOf(IReadOnlyList<uint> components, uint nodeId) =>
        components.Count == 0 ? nodeId.ToString(CultureInfo.InvariantCulture) : $"{string.Join('/', components)}/{nodeId}";

    private static float Number(string cell) => float.Parse(cell, CultureInfo.InvariantCulture);

    private static string Format(float value) => value.ToString(CultureInfo.InvariantCulture);
}
