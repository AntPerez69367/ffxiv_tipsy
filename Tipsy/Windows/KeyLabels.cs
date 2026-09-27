using System.Collections.Generic;
using System.Text;
using Dalamud.Game.Text;

namespace Tipsy.Windows;

/// <summary>Spells out the game's mouse glyphs in keybind text, so "Shift+Mouse" followed by the button-5 glyph reads "Shift+Mouse 5".</summary>
internal static class KeyLabels
{
    private const string Mouse = "Mouse";

    private static readonly Dictionary<char, string> Glyphs = new()
    {
        [SeIconChar.MouseNoClick.ToIconChar()] = Mouse,
        [SeIconChar.MouseLeftClick.ToIconChar()] = "Left Click",
        [SeIconChar.MouseRightClick.ToIconChar()] = "Right Click",
        [SeIconChar.MouseBothClick.ToIconChar()] = "Both Clicks",
        [SeIconChar.MouseWheel.ToIconChar()] = "Mouse Wheel",
        [SeIconChar.Mouse1.ToIconChar()] = "Mouse 1",
        [SeIconChar.Mouse2.ToIconChar()] = "Mouse 2",
        [SeIconChar.Mouse3.ToIconChar()] = "Mouse 3",
        [SeIconChar.Mouse4.ToIconChar()] = "Mouse 4",
        [SeIconChar.Mouse5.ToIconChar()] = "Mouse 5",
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

            if (label.StartsWith(Mouse) && text.ToString().EndsWith(Mouse))
                text.Length -= Mouse.Length;
            text.Append(label);
        }

        return text.ToString();
    }
}
