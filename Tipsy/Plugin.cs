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
#if TIPSY_PROBE
using System.IO;
#endif

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
    [PluginService] internal static IKeyState KeyState { get; private set; } = null!;

    private const string CommandName = "/tipsy";
    private const string Usage = "Use /tipsy for settings or /tipsy toggle to turn Tipsy on or off.";

    private readonly WindowSystem windowSystem = new("Tipsy");
    private readonly Configuration configuration;
    private readonly TooltipReader[] readers;
    private readonly NativeTooltipHider[] hiders;
    private readonly TooltipFonts fonts;
    private readonly ItemIcons itemIcons;
    private readonly TooltipOverlay overlay;
    private readonly ConfigWindow configWindow;
#if TIPSY_PROBE
    private readonly TooltipProbe[] probes;
    private readonly AddonDiscovery discovery;
    private readonly ProbeWindow probeWindow;
#endif

    public Plugin()
    {
        configuration = Configuration.Load();
        var itemReader = new TooltipReader(AddonLifecycle, Log, ItemDetailMap.Map);
        var actionReader = new TooltipReader(AddonLifecycle, Log, ActionDetailMap.Map);
        var textReader = new TooltipReader(AddonLifecycle, Log, TextTooltipMap.Map);
        readers = [itemReader, actionReader, textReader];
        var textSource = new TooltipSource(textReader, TextTooltipLayout.Build, true);
        TooltipSource[] sources =
        [
            new(itemReader, ItemTooltipLayout.Build, false),
            new(actionReader, ActionTooltipLayout.Build, false),
            textSource,
        ];
        var selector = new TooltipSelector(sources, textSource, GameGui);
        hiders = [.. readers.Select(reader => new NativeTooltipHider(AddonLifecycle, GameGui, reader, selector, configuration.ReplaceTooltips))];
        fonts = new TooltipFonts(PluginInterface.UiBuilder.FontAtlas);
        itemIcons = new ItemIcons(DataManager, Log);
        overlay = new TooltipOverlay(selector, KeyState, TextureProvider, fonts, itemIcons, configuration);
        configWindow = new ConfigWindow(configuration, overlay, KeyState, SetReplaceTooltips);
        windowSystem.AddWindow(overlay);
        windowSystem.AddWindow(configWindow);
#if TIPSY_PROBE
        var directory = Path.Combine(PluginInterface.GetPluginConfigDirectory(), "probe");
        var gameVersion = DataManager.GameData.Repositories["ffxiv"].Version;
        probes =
        [
            new TooltipProbe("ItemDetail", () => GameGui.HoveredItem.ToString(), AddonLifecycle, GameGui, Log, directory, gameVersion),
            new TooltipProbe("ActionDetail", () => $"{GameGui.HoveredAction.DetailKind}-{GameGui.HoveredAction.ActionId}", AddonLifecycle, GameGui, Log, directory, gameVersion),
            new TooltipProbe("Tooltip", () => "text", AddonLifecycle, GameGui, Log, directory, gameVersion),
        ];
        discovery = new AddonDiscovery(directory);
        probeWindow = new ProbeWindow(probes, readers, discovery, overlay, DataManager);
        windowSystem.AddWindow(probeWindow);
#endif

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) { HelpMessage = Usage });

        PluginInterface.UiBuilder.DisableGposeUiHide = true;
        PluginInterface.UiBuilder.DisableCutsceneUiHide = true;
        PluginInterface.UiBuilder.Draw += OnDraw;
        PluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
        PluginInterface.UiBuilder.OpenMainUi += configWindow.Toggle;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= OnDraw;
        PluginInterface.UiBuilder.OpenConfigUi -= configWindow.Toggle;
        PluginInterface.UiBuilder.OpenMainUi -= configWindow.Toggle;
        windowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
#if TIPSY_PROBE
        foreach (var probe in probes)
            probe.Dispose();
#endif
        foreach (var hider in hiders)
            hider.Dispose();
        foreach (var reader in readers)
            reader.Dispose();
        fonts.Dispose();
    }

    private void OnDraw()
    {
#if TIPSY_PROBE
        discovery.Tick();
        foreach (var probe in probes)
            probe.CheckRendered();
#endif
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
            case "toggle":
                configuration.ReplaceTooltips = !configuration.ReplaceTooltips;
                configuration.Save();
                SetReplaceTooltips(configuration.ReplaceTooltips);
                ChatGui.Print(configuration.ReplaceTooltips ? "Tipsy is on: replacing the game's tooltips." : "Tipsy is off: showing the game's tooltips.");
                break;
#if TIPSY_PROBE
            case "probe":
                probeWindow.Toggle();
                break;
#endif
            default:
                ChatGui.PrintError($"Unknown command \"{args.Trim()}\". {Usage}");
                break;
        }
    }
}
