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

    [Fact]
    public void NoTwoPresetsAreTheSame()
    {
        var presets = Enum.GetValues<ThemePreset>().Select(ThemeColors.Of).ToList();

        Assert.Equal(presets.Count, presets.Distinct().Count());
    }

    [Fact]
    public void LightTranslucentSurfaceIsJudgedAgainstADarkBackdropToo()
    {
        var theme = ThemeColors.Native with { Surface = 0xFFFFFF, SurfaceAlpha = 0.5f, SecondaryText = 0x777777 };

        Assert.True(theme.SecondaryContrast() < ThemeColors.MinimumContrast);
    }

    [Fact]
    public void TextReadableOverBlackAndWhiteFailsOnATransparentSurfaceOverAMatchingGrey()
    {
        var theme = ThemeColors.Native with { SurfaceAlpha = 0, SecondaryText = 0x767676 };

        Assert.Equal(1, theme.SecondaryContrast());
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
