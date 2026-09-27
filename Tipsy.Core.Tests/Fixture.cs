using Tipsy.Core.Nodes;
using Tipsy.Core.Text;

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

    public static string Plain(SeText text) => SeStringText.Plain(text);
}
