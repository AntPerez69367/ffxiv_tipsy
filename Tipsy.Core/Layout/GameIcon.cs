using System.Globalization;
using System.Text.RegularExpressions;

namespace Tipsy.Core.Layout;

/// <summary>A game icon id and whether the texture was the high-quality variant.</summary>
public readonly partial record struct GameIcon(uint Id, bool HighQuality)
{
    /// <summary>The icon a texture path such as "ui/icon/037000/037810_hr1.tex" loads, or null for any other texture.</summary>
    public static GameIcon? FromTexture(string texture)
    {
        var match = IconFileName().Match(texture);
        if (!match.Success)
            return null;
        return new GameIcon(uint.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), texture.Contains("/hq/", StringComparison.Ordinal));
    }

    [GeneratedRegex(@"(\d{6})(?:_hr1)?\.tex$")]
    private static partial Regex IconFileName();
}
