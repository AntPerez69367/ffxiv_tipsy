using System.IO;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Tipsy.Core.Layout;
using Tipsy.Core.Tooltips;
using Tipsy.Game;
using Tipsy.Windows;

namespace Tipsy;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

    private const string CommandName = "/tipsy";

    private readonly WindowSystem windowSystem = new("Tipsy");
    private readonly Configuration configuration;
    private readonly TooltipReader[] readers;
    private readonly NativeTooltipHider[] hiders;
    private readonly TooltipProbe[] probes;
    private readonly AddonDiscovery discovery;
    private readonly TooltipFonts fonts;
    private readonly TooltipOverlay overlay;
    private readonly ProbeWindow probeWindow;
    private readonly ConfigWindow configWindow;

    public Plugin()
    {
        var directory = Path.Combine(PluginInterface.GetPluginConfigDirectory(), "probe");
        var gameVersion = DataManager.GameData.Repositories["ffxiv"].Version;
        configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        readers = [new TooltipReader(AddonLifecycle, Log, ItemDetailMap.Map), new TooltipReader(AddonLifecycle, Log, TextTooltipMap.Map)];
        hiders = [.. readers.Select(reader => new NativeTooltipHider(AddonLifecycle, GameGui, reader, configuration.ReplaceTooltips))];
        probes =
        [
            new TooltipProbe("ItemDetail", () => GameGui.HoveredItem.ToString(), AddonLifecycle, GameGui, Log, directory, gameVersion),
            new TooltipProbe("ItemDetailCompare", () => GameGui.HoveredItem.ToString(), AddonLifecycle, GameGui, Log, directory, gameVersion),
            new TooltipProbe("ActionDetail", () => $"{GameGui.HoveredAction.DetailKind}-{GameGui.HoveredAction.ActionId}", AddonLifecycle, GameGui, Log, directory, gameVersion),
            new TooltipProbe("Tooltip", () => "text", AddonLifecycle, GameGui, Log, directory, gameVersion),
        ];
        discovery = new AddonDiscovery(directory);
        fonts = new TooltipFonts(PluginInterface.UiBuilder.FontAtlas);
        TooltipSource[] sources =
        [
            new(readers[0], ItemTooltipLayout.Build, false),
            new(readers[1], TextTooltipLayout.Build, true),
        ];
        overlay = new TooltipOverlay(sources, GameGui, TextureProvider, fonts, new ItemIcons(DataManager), configuration);
        configWindow = new ConfigWindow(configuration, overlay, SetReplaceTooltips);
        probeWindow = new ProbeWindow(probes, readers, discovery, overlay, DataManager);
        windowSystem.AddWindow(overlay);
        windowSystem.AddWindow(probeWindow);
        windowSystem.AddWindow(configWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "\"/tipsy\" opens the settings. \"/tipsy toggle\" turns Tipsy on or off. \"/tipsy probe\" opens the tooltip probe.",
        });

        PluginInterface.UiBuilder.Draw += OnDraw;
        PluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= OnDraw;
        PluginInterface.UiBuilder.OpenConfigUi -= configWindow.Toggle;
        windowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
        foreach (var probe in probes)
            probe.Dispose();
        foreach (var hider in hiders)
            hider.Dispose();
        foreach (var reader in readers)
            reader.Dispose();
        fonts.Dispose();
    }

    private void OnDraw()
    {
        discovery.Tick();
        foreach (var probe in probes)
            probe.CheckRendered();
        windowSystem.Draw();
    }

    private void SetReplaceTooltips(bool replace)
    {
        foreach (var hider in hiders)
            hider.Enabled = replace;
    }

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "":
            case "config":
                configWindow.Toggle();
                break;
            case "probe":
                probeWindow.Toggle();
                break;
            case "toggle":
                configuration.ReplaceTooltips = !configuration.ReplaceTooltips;
                configuration.Save();
                SetReplaceTooltips(configuration.ReplaceTooltips);
                ChatGui.Print(configuration.ReplaceTooltips ? "Tipsy is on: replacing the game's tooltips." : "Tipsy is off: showing the game's tooltips.");
                break;
            default:
                Log.Information($"Unknown command \"{args}\"");
                break;
        }
    }
}
