using Tipsy.Core.Layout;
using Xunit;

namespace Tipsy.Core.Tests.Layout;

public class ThemeColorsTests
{
    public static TheoryData<ThemePreset> Presets => [.. Enum.GetValues<ThemePreset>()];

    [Theory]
    [MemberData(nameof(Presets))]
    public void EveryPresetPassesTheContrastCheck(ThemePreset preset)
    {
        Assert.True(ThemeColors.Of(preset).SecondaryContrast() >= ThemeColors.MinimumContrast, $"{preset}: {ThemeColors.Of(preset).SecondaryContrast():F2}");
    }

    [Theory]
    [MemberData(nameof(Presets))]
    public void EveryPresetHasItsOwnColours(ThemePreset preset)
    {
        Assert.True(preset == ThemePreset.Native || ThemeColors.Of(preset) != ThemeColors.Native);
    }

    [Fact]
    public void GreySecondaryTextOnAGreySurfaceFailsTheContrastCheck()
    {
        var theme = ThemeColors.Native with { Surface = 0x606060, SecondaryText = 0x808080 };

        Assert.True(theme.SecondaryContrast() < ThemeColors.MinimumContrast);
    }

    [Fact]
    public void WhiteOnBlackIsTwentyOneToOne()
    {
        var theme = ThemeColors.Native with { Surface = 0x000000, SurfaceAlpha = 1, SecondaryText = 0xFFFFFF };

        Assert.Equal(21, theme.SecondaryContrast(), 3);
    }

    [Fact]
    public void OverridesReplaceOnlyTheirTokens()
    {
        var theme = ThemeColors.Aether.With(
            new Dictionary<ThemeToken, uint> { [ThemeToken.Accent] = 0x123456 },
            new Dictionary<ThemeToken, float> { [ThemeToken.Rounding] = 10 });

        Assert.Equal(ThemeColors.Aether with { Accent = 0x123456, Rounding = 10 }, theme);
    }

    [Fact]
    public void EveryTokenReadsBackFromThePreset()
    {
        foreach (var token in Enum.GetValues<ThemeToken>())
        {
            if (ThemeColors.IsColour(token))
                Assert.Equal(ThemeColors.Minimal.ColourOf(token), ThemeColors.Native.With(new Dictionary<ThemeToken, uint> { [token] = ThemeColors.Minimal.ColourOf(token) }, new Dictionary<ThemeToken, float>()).ColourOf(token));
            else
                Assert.Equal(ThemeColors.Minimal.NumberOf(token), ThemeColors.Native.With(new Dictionary<ThemeToken, uint>(), new Dictionary<ThemeToken, float> { [token] = ThemeColors.Minimal.NumberOf(token) }).NumberOf(token));
        }
    }
}
