using System.Globalization;

namespace Tipsy.Core.Nodes;

/// <summary>
/// Reads and writes the probe's tab-separated node dumps: two header lines, a column header, then one row per
/// node in tree order with each component's children directly after it. The text column is a readable copy of the
/// text and is ignored when reading; the hex column holds the exact bytes. The part column, "u,v,width,height", is
/// last and missing from dumps recorded before it was added.
/// </summary>
public static class NodeDump
{
    public const string ColumnHeader = "depth\tid\ttype\tparent\tvisible\tshown\tx\ty\tscreenX\tscreenY\twidth\theight\tscaleX\tscaleY\tpartId\ttexture\ttextHex\ttext\tpart";

    private const int HeaderLines = 3;
    private const int Columns = 18;
    private const int PartColumn = 18;
    private const string ComponentPrefix = "Component";

    public static List<NodeRecord> Read(TextReader input)
    {
        for (var i = 0; i < HeaderLines; i++)
            input.ReadLine();

        var records = new List<NodeRecord>();
        var components = new List<uint>();
        var lineNumber = HeaderLines;
        while (input.ReadLine() is { } line)
        {
            lineNumber++;
            if (line.Length == 0)
                continue;
            var cells = line.Split('\t');
            if (cells.Length < Columns)
                throw new FormatException($"Line {lineNumber} has {cells.Length} columns, expected {Columns}.");
            var depth = int.Parse(cells[0], CultureInfo.InvariantCulture);
            if (depth < 0 || depth > components.Count)
                throw new FormatException($"Line {lineNumber} is at depth {depth} with only {components.Count} components open.");
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
                Convert.FromHexString(cells[16]),
                cells.Length > PartColumn ? Part(cells[PartColumn]) : default));
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
                text,
                string.Join(',', node.Part.U, node.Part.V, node.Part.Width, node.Part.Height)));
        }
    }

    public static string PathOf(IReadOnlyList<uint> components, uint nodeId) =>
        components.Count == 0 ? nodeId.ToString(CultureInfo.InvariantCulture) : $"{string.Join('/', components)}/{nodeId}";

    private static PartRect Part(string cell)
    {
        var values = cell.Split(',').Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray();
        if (values.Length != 4)
            throw new FormatException($"Part \"{cell}\" is not u,v,width,height.");
        return new PartRect(values[0], values[1], values[2], values[3]);
    }

    private static float Number(string cell) => float.Parse(cell, CultureInfo.InvariantCulture);

    private static string Format(float value) => value.ToString(CultureInfo.InvariantCulture);
}
