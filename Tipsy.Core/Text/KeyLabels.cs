using System.Text;

namespace Tipsy.Core.Text;

/// <summary>Spells out the game's mouse glyphs in keybind text, so "Shift+Mouse" followed by the button-5 glyph reads "Shift+Mouse 5".</summary>
public static class KeyLabels
{
    private const string Mouse = "Mouse";

    private static readonly Dictionary<char, string> Glyphs = new()
    {
        [''] = Mouse,
        [''] = "Left Click",
        [''] = "Right Click",
        [''] = "Both Clicks",
        [''] = "Mouse Wheel",
        [''] = "Mouse 1",
        [''] = "Mouse 2",
        [''] = "Mouse 3",
        [''] = "Mouse 4",
        [''] = "Mouse 5",
    };

    public static string Readable(string keybind)
    {
        var text = new StringBuilder(keybind.Length + 8);
        foreach (var character in keybind)
        {
            if (!Glyphs.TryGetValue(character, out var label))
            {
                text.Append(character);
                continue;
            }

            if (label.StartsWith(Mouse, StringComparison.Ordinal) && text.ToString().EndsWith(Mouse, StringComparison.Ordinal))
                text.Length -= Mouse.Length;
            text.Append(label);
        }

        return text.ToString();
    }
}
