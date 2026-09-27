using System;
using Dalamud.Configuration;

namespace Tipsy;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    /// <summary>Whether to hide the game's own tooltip while Tipsy draws its replacement.</summary>
    public bool HideNativeTooltip { get; set; } = true;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
