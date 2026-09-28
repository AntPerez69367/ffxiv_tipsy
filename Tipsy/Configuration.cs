using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Game.ClientState.Keys;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Tipsy.Core.Layout;

namespace Tipsy;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public const float MaxCursorOffset = 100;
    public const float MaxRounding = 16;

    [JsonProperty(nameof(ColourOverrides))]
    private Dictionary<ThemeToken, uint> colourOverrides = [];

    [JsonProperty(nameof(NumberOverrides))]
    private Dictionary<ThemeToken, float> numberOverrides = [];

    private ThemePreset preset = ThemePreset.Minimal;
    private ThemeColors? theme;

    public int Version { get; set; } = 1;

    /// <summary>Whether Tipsy hides the game's tooltips and draws its own; when off, only the game's tooltips show.</summary>
    public bool ReplaceTooltips { get; set; } = true;

    /// <summary>While this key is held, Tipsy's tooltip is not drawn; <see cref="VirtualKey.NO_KEY"/> turns this off.</summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public VirtualKey HideKey { get; set; } = VirtualKey.MENU;

    [JsonConverter(typeof(StringEnumConverter))]
    public AnchorPreset Anchor { get; set; } = AnchorPreset.TopRight;

    /// <summary>The window's top-left corner when <see cref="Anchor"/> is <see cref="AnchorPreset.Custom"/>, in screen pixels.</summary>
    public Vector2 CustomPosition { get; set; } = new(100, 100);

    /// <summary>How far from the mouse cursor the window sits when <see cref="Anchor"/> is <see cref="AnchorPreset.Cursor"/>, at scale 1.</summary>
    public Vector2 CursorOffset { get; set; } = new(24, 24);

    public float Width { get; set; } = new LayoutTokens().Width;

    [JsonConverter(typeof(StringEnumConverter))]
    public ThemePreset Preset
    {
        get => preset;
        set
        {
            preset = value;
            theme = null;
        }
    }

    [JsonIgnore]
    public IReadOnlyDictionary<ThemeToken, uint> ColourOverrides => colourOverrides;

    [JsonIgnore]
    public IReadOnlyDictionary<ThemeToken, float> NumberOverrides => numberOverrides;

    /// <summary>The saved settings, with values that no longer exist or fall outside their range put back to defaults.</summary>
    public static Configuration Load()
    {
        var configuration = Plugin.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        var defaults = new Configuration();
        var tokens = new LayoutTokens();
        if (!Enum.IsDefined(configuration.Anchor))
            configuration.Anchor = defaults.Anchor;
        if (!Enum.IsDefined(configuration.Preset))
            configuration.Preset = defaults.Preset;
        if (!Enum.IsDefined(configuration.HideKey))
            configuration.HideKey = defaults.HideKey;
        configuration.Width = Math.Clamp(configuration.Width, tokens.MinWidth, tokens.MaxWidth);
        configuration.CursorOffset = Vector2.Clamp(configuration.CursorOffset, new Vector2(-MaxCursorOffset), new Vector2(MaxCursorOffset));
        foreach (var (token, number) in configuration.numberOverrides.ToList())
            configuration.numberOverrides[token] = Math.Clamp(number, 0, token == ThemeToken.Rounding ? MaxRounding : 1);
        return configuration;
    }

    public ThemeColors Theme() => theme ??= ThemeColors.Of(Preset).With(colourOverrides, numberOverrides);

    public void SetColour(ThemeToken token, uint colour)
    {
        colourOverrides[token] = colour;
        theme = null;
    }

    public void SetNumber(ThemeToken token, float number)
    {
        numberOverrides[token] = number;
        theme = null;
    }

    public void ResetToken(ThemeToken token)
    {
        colourOverrides.Remove(token);
        numberOverrides.Remove(token);
        theme = null;
    }

    public void ClearOverrides()
    {
        colourOverrides.Clear();
        numberOverrides.Clear();
        theme = null;
    }

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
