using System;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Tipsy.Core.Tooltips;

namespace Tipsy.Game;

/// <summary>
/// Keeps an up-to-date <see cref="TooltipSnapshot"/> of one tooltip addon. It reads the tree at PreDraw, after
/// every plugin has written its lines, and only rebuilds when a content update was requested or the hash of the
/// tree's text and visibility changed.
/// </summary>
internal sealed unsafe class TooltipReader : IDisposable
{
    private readonly IAddonLifecycle addonLifecycle;
    private readonly IPluginLog log;
    private readonly TooltipMap map;
    private bool dirty = true;
    private int lastHash;
    private string? loggedMismatch;

    public TooltipReader(IAddonLifecycle addonLifecycle, IPluginLog log, TooltipMap map)
    {
        this.addonLifecycle = addonLifecycle;
        this.log = log;
        this.map = map;

        addonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, map.Addon, OnRequestedUpdate);
        addonLifecycle.RegisterListener(AddonEvent.PreDraw, map.Addon, OnPreDraw);
    }

    public string Addon => map.Addon;

    public TooltipSnapshot? Current { get; private set; }

    public int Rebuilds { get; private set; }

    /// <summary>Raised at the end of every PreDraw of the addon, after <see cref="Current"/> is up to date, with the addon's address.</summary>
    public event Action<nint>? Drawing;

    public void Dispose()
    {
        addonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, map.Addon, OnRequestedUpdate);
        addonLifecycle.UnregisterListener(AddonEvent.PreDraw, map.Addon, OnPreDraw);
    }

    private void OnRequestedUpdate(AddonEvent type, AddonArgs args) => dirty = true;

    private void OnPreDraw(AddonEvent type, AddonArgs args)
    {
        var unit = (AtkUnitBase*)args.Addon.Address;
        if (unit == null)
            return;

        var hash = NodeWalker.Hash(unit);
        if (dirty || hash != lastHash)
            Rebuild(unit, hash);
        Drawing?.Invoke(args.Addon.Address);
    }

    private void Rebuild(AtkUnitBase* unit, int hash)
    {
        Current = null;
        Current = SnapshotBuilder.Build(map, NodeWalker.Collect(unit));
        dirty = false;
        lastHash = hash;
        Rebuilds++;
        if (Current.Mismatch is { } mismatch && mismatch != loggedMismatch)
        {
            log.Warning($"{map.Addon} no longer matches its map (recorded for {map.GameVersion}): {mismatch}");
            loggedMismatch = mismatch;
        }
    }
}
