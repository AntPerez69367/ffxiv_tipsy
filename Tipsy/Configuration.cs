using System;
using System.Collections.Generic;
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
    public ThemePreset Preset { get; set; } = ThemePreset.Minimal;

    public Dictionary<ThemeToken, uint> ColourOverrides { get; set; } = [];

    public Dictionary<ThemeToken, float> NumberOverrides { get; set; } = [];

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
        return configuration;
    }

    public ThemeColors Theme() => ThemeColors.Of(Preset).With(ColourOverrides, NumberOverrides);

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
