namespace Tipsy.Core.Layout;

public enum ThemePreset
{
    Native,
    Aether,
    Minimal,
    CatppuccinMocha,
    TokyoNight,
    Nord,
    Dracula,
    GruvboxDark,
}

public enum ThemeToken
{
    Surface,
    SurfaceAlpha,
    Border,
    PrimaryText,
    SecondaryText,
    Accent,
    Divider,
    DividerAlpha,
    Better,
    Worse,
    Rounding,
}

/// <summary>Theme colours as 0xRRGGBB with separate alphas, plus the window rounding in pixels at scale 1.</summary>
public sealed record ThemeColors(
    uint Surface,
    float SurfaceAlpha,
    uint Border,
    uint PrimaryText,
    uint SecondaryText,
    uint Accent,
    uint Divider,
    float DividerAlpha,
    uint Better,
    uint Worse,
    float Rounding)
{
    public const float MinimumContrast = 4.5f;

    public static readonly ThemeColors Native = new(0x17130F, 0.95f, 0x8E7650, 0xF0DFC0, 0xB8A98D, 0xD8B86A, 0xA98D59, 0.25f, 0x8FD694, 0xE0826E, 6);
    public static readonly ThemeColors Aether = new(0x171B24, 0.96f, 0x596477, 0xF3EEE2, 0xAAAFAF, 0xD6B56D, 0xFFFFFF, 0x20 / 255f, 0x8FD694, 0xE0826E, 7);
    public static readonly ThemeColors Minimal = new(0x101214, 0.98f, 0x343A40, 0xF1F3F5, 0x9AA1A8, 0x72B7FF, 0xFFFFFF, 0x18 / 255f, 0x8FD694, 0xE0826E, 2);
    public static readonly ThemeColors CatppuccinMocha = new(0x1E1E2E, 0.96f, 0x45475A, 0xCDD6F4, 0xA6ADC8, 0xCBA6F7, 0x6C7086, 0.35f, 0xA6E3A1, 0xF38BA8, 8);
    public static readonly ThemeColors TokyoNight = new(0x1A1B26, 0.96f, 0x3B4261, 0xC0CAF5, 0xA9B1D6, 0x7AA2F7, 0x565F89, 0.4f, 0x9ECE6A, 0xF7768E, 6);
    public static readonly ThemeColors Nord = new(0x2E3440, 0.96f, 0x4C566A, 0xECEFF4, 0xD8DEE9, 0x88C0D0, 0x4C566A, 0.6f, 0xA3BE8C, 0xBF616A, 4);
    public static readonly ThemeColors Dracula = new(0x282A36, 0.96f, 0x44475A, 0xF8F8F2, 0xA4AFD6, 0xBD93F9, 0x6272A4, 0.5f, 0x50FA7B, 0xFF5555, 6);
    public static readonly ThemeColors GruvboxDark = new(0x282828, 0.96f, 0x504945, 0xEBDBB2, 0xA89984, 0xFABD2F, 0x665C54, 0.6f, 0xB8BB26, 0xFB4934, 3);

    public static ThemeColors Of(ThemePreset preset) => preset switch
    {
        ThemePreset.Aether => Aether,
        ThemePreset.Minimal => Minimal,
        ThemePreset.CatppuccinMocha => CatppuccinMocha,
        ThemePreset.TokyoNight => TokyoNight,
        ThemePreset.Nord => Nord,
        ThemePreset.Dracula => Dracula,
        ThemePreset.GruvboxDark => GruvboxDark,
        _ => Native,
    };

    public static string NameOf(ThemePreset preset) => preset switch
    {
        ThemePreset.CatppuccinMocha => "Catppuccin Mocha",
        ThemePreset.TokyoNight => "Tokyo Night",
        ThemePreset.GruvboxDark => "Gruvbox Dark",
        _ => preset.ToString(),
    };

    public static bool IsColour(ThemeToken token) => token is not (ThemeToken.SurfaceAlpha or ThemeToken.DividerAlpha or ThemeToken.Rounding);

    public uint ColourOf(ThemeToken token) => token switch
    {
        ThemeToken.Surface => Surface,
        ThemeToken.Border => Border,
        ThemeToken.PrimaryText => PrimaryText,
        ThemeToken.SecondaryText => SecondaryText,
        ThemeToken.Accent => Accent,
        ThemeToken.Divider => Divider,
        ThemeToken.Better => Better,
        ThemeToken.Worse => Worse,
        _ => throw new ArgumentOutOfRangeException(nameof(token), token, "not a colour token"),
    };

    public float NumberOf(ThemeToken token) => token switch
    {
        ThemeToken.SurfaceAlpha => SurfaceAlpha,
        ThemeToken.DividerAlpha => DividerAlpha,
        ThemeToken.Rounding => Rounding,
        _ => throw new ArgumentOutOfRangeException(nameof(token), token, "not a number token"),
    };

    /// <summary>The preset with each overridden token replaced.</summary>
    public ThemeColors With(IReadOnlyDictionary<ThemeToken, uint> colours, IReadOnlyDictionary<ThemeToken, float> numbers)
    {
        var theme = this;
        foreach (var (token, colour) in colours)
        {
            theme = token switch
            {
                ThemeToken.Surface => theme with { Surface = colour },
                ThemeToken.Border => theme with { Border = colour },
                ThemeToken.PrimaryText => theme with { PrimaryText = colour },
                ThemeToken.SecondaryText => theme with { SecondaryText = colour },
                ThemeToken.Accent => theme with { Accent = colour },
                ThemeToken.Divider => theme with { Divider = colour },
                ThemeToken.Better => theme with { Better = colour },
                ThemeToken.Worse => theme with { Worse = colour },
                _ => theme,
            };
        }

        foreach (var (token, number) in numbers)
        {
            theme = token switch
            {
                ThemeToken.SurfaceAlpha => theme with { SurfaceAlpha = number },
                ThemeToken.DividerAlpha => theme with { DividerAlpha = number },
                ThemeToken.Rounding => theme with { Rounding = number },
                _ => theme,
            };
        }

        return theme;
    }

    /// <summary>
    /// The WCAG contrast ratio of the secondary text against the surface, taking the worse of the surface composited over
    /// white and over black, since a translucent surface can sit on any part of the game.
    /// </summary>
    public double SecondaryContrast() =>
        Math.Min(Ratio(SecondaryText, Composite(Surface, SurfaceAlpha, 0xFFFFFF)), Ratio(SecondaryText, Composite(Surface, SurfaceAlpha, 0x000000)));

    private static double Ratio(uint foreground, uint background)
    {
        var text = Luminance(foreground);
        var back = Luminance(background);
        return (Math.Max(text, back) + 0.05) / (Math.Min(text, back) + 0.05);
    }

    private static uint Composite(uint colour, float alpha, uint backdrop)
    {
        uint Channel(int shift) => (uint)Math.Round((((colour >> shift) & 0xFF) * alpha) + (((backdrop >> shift) & 0xFF) * (1 - alpha)));
        return (Channel(16) << 16) | (Channel(8) << 8) | Channel(0);
    }

    private static double Luminance(uint colour)
    {
        static double Linear(uint channel)
        {
            var value = channel / 255.0;
            return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Linear((colour >> 16) & 0xFF)) + (0.7152 * Linear((colour >> 8) & 0xFF)) + (0.0722 * Linear(colour & 0xFF));
    }
}
