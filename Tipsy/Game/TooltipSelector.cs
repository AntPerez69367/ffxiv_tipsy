using System.Collections.Generic;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Tipsy.Core.Layout;
using Tipsy.Core.Tooltips;
using Tipsy.Windows;

namespace Tipsy.Game;

/// <summary>
/// Decides which open tooltip Tipsy draws and with what blocks: the first open source, in priority order, whose layout
/// is not empty, with a text tooltip open alongside it folded in. The overlay draws from it and the hiders ask it, so
/// a native tooltip is only ever hidden when Tipsy draws its content.
/// </summary>
internal sealed unsafe class TooltipSelector
{
    private readonly IReadOnlyList<TooltipSource> sources;
    private readonly TooltipSource text;
    private readonly IGameGui gameGui;
    private readonly Dictionary<TooltipSource, (TooltipSnapshot Main, TooltipSnapshot? Folded, List<TooltipBlock> Blocks)> laidOut = [];

    public TooltipSelector(IReadOnlyList<TooltipSource> sources, TooltipSource text, IGameGui gameGui)
    {
        this.sources = sources;
        this.text = text;
        this.gameGui = gameGui;
    }

    public TooltipSource? Drawn { get; private set; }

    public IReadOnlyList<TooltipBlock> Blocks { get; private set; } = [];

    public bool FoldsText { get; private set; }

    public void Update()
    {
        Drawn = null;
        Blocks = [];
        FoldsText = false;
        var openText = IsShowing(text) ? text.Reader.Current : null;
        foreach (var source in sources)
        {
            if (!IsShowing(source))
                continue;
            var folded = source == text ? null : openText;
            var blocks = LayOut(source, source.Reader.Current!, folded);
            if (blocks.Count == 0)
                continue;
            Drawn = source;
            Blocks = blocks;
            FoldsText = folded is not null && blocks[0] is HeaderBlock;
            return;
        }
    }

    public bool Draws(string addon)
    {
        Update();
        return Drawn?.Reader.Addon == addon || (FoldsText && addon == text.Reader.Addon);
    }

    private List<TooltipBlock> LayOut(TooltipSource source, TooltipSnapshot main, TooltipSnapshot? folded)
    {
        if (laidOut.TryGetValue(source, out var cached) && ReferenceEquals(cached.Main, main) && ReferenceEquals(cached.Folded, folded))
            return cached.Blocks;
        var blocks = source.Layout(main);
        if (folded is not null)
            blocks = SharedLayout.WithTextTooltip(blocks, folded);
        laidOut[source] = (main, folded, blocks);
        return blocks;
    }

    private bool IsShowing(TooltipSource source)
    {
        var unit = (AtkUnitBase*)gameGui.GetAddonByName(source.Reader.Addon).Address;
        return unit != null && unit->IsVisible && source.Reader.Current is not null;
    }
}
