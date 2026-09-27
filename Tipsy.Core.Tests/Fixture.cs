using System.Text;
using Tipsy.Core.Nodes;

namespace Tipsy.Core.Tests;

/// <summary>Loads the probe dumps under Fixtures and turns SeString bytes into plain text for assertions.</summary>
internal static class Fixture
{
    public const string Session = "2026-09-27";

    public static List<NodeRecord> Load(string name)
    {
        using var reader = new StreamReader(Path.Combine(AppContext.BaseDirectory, "Fixtures", Session, $"{name}.tsv"));
        return NodeDump.Read(reader);
    }

    public static string Plain(byte[] bytes)
    {
        var text = new List<byte>(bytes.Length);
        for (var i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] != 0x02)
            {
                text.Add(bytes[i]);
                continue;
            }

            var length = bytes[i + 2] - 1;
            i += 3 + length;
        }

        return Encoding.UTF8.GetString(text.ToArray());
    }
}
