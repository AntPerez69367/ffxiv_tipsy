using System.IO;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
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

    private const string CommandName = "/tipsy";

    private readonly WindowSystem windowSystem = new("Tipsy");
    private readonly TooltipReader[] readers;
    private readonly TooltipProbe[] probes;
    private readonly AddonDiscovery discovery;
    private readonly TooltipFonts fonts;
    private readonly TooltipOverlay overlay;
    private readonly ProbeWindow probeWindow;

    public Plugin()
    {
        var directory = Path.Combine(PluginInterface.GetPluginConfigDirectory(), "probe");
        var gameVersion = DataManager.GameData.Repositories["ffxiv"].Version;
        readers = [new TooltipReader(AddonLifecycle, Log, ItemDetailMap.Map)];
        probes =
        [
            new TooltipProbe("ItemDetail", () => GameGui.HoveredItem.ToString(), AddonLifecycle, GameGui, Log, directory, gameVersion),
            new TooltipProbe("ItemDetailCompare", () => GameGui.HoveredItem.ToString(), AddonLifecycle, GameGui, Log, directory, gameVersion),
            new TooltipProbe("ActionDetail", () => $"{GameGui.HoveredAction.DetailKind}-{GameGui.HoveredAction.ActionId}", AddonLifecycle, GameGui, Log, directory, gameVersion),
            new TooltipProbe("Tooltip", () => "text", AddonLifecycle, GameGui, Log, directory, gameVersion),
        ];
        discovery = new AddonDiscovery(directory);
        fonts = new TooltipFonts(PluginInterface.UiBuilder.FontAtlas);
        overlay = new TooltipOverlay(readers[0], GameGui, TextureProvider, fonts, new ItemIcons(DataManager));
        probeWindow = new ProbeWindow(probes, readers, discovery, overlay, DataManager);
        windowSystem.AddWindow(overlay);
        windowSystem.AddWindow(probeWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "\"/tipsy\" or \"/tipsy probe\" toggles the tooltip probe window.",
        });

        PluginInterface.UiBuilder.Draw += OnDraw;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= OnDraw;
        windowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
        foreach (var probe in probes)
            probe.Dispose();
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

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "":
            case "probe":
                probeWindow.Toggle();
                break;
            default:
                Log.Information($"Unknown command \"{args}\"");
                break;
        }
    }
}
