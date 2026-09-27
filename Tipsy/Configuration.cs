using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Configuration;
using Tipsy.Core.Layout;

namespace Tipsy;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    /// <summary>Whether Tipsy hides the game's tooltips and draws its own; when off, only the game's tooltips show.</summary>
    public bool ReplaceTooltips { get; set; } = true;

    public AnchorPreset Anchor { get; set; } = AnchorPreset.TopRight;

    /// <summary>The window's top-left corner when <see cref="Anchor"/> is <see cref="AnchorPreset.Custom"/>, in screen pixels.</summary>
    public Vector2 CustomPosition { get; set; } = new(100, 100);

    /// <summary>How far from the mouse cursor the window sits when <see cref="Anchor"/> is <see cref="AnchorPreset.Cursor"/>, at scale 1.</summary>
    public Vector2 CursorOffset { get; set; } = new(24, 24);

    public float Width { get; set; } = new LayoutTokens().Width;

    public ThemePreset Preset { get; set; } = ThemePreset.Minimal;

    public Dictionary<ThemeToken, uint> ColourOverrides { get; set; } = [];

    public Dictionary<ThemeToken, float> NumberOverrides { get; set; } = [];

    public ThemeColors Theme() => ThemeColors.Of(Preset).With(ColourOverrides, NumberOverrides);

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
