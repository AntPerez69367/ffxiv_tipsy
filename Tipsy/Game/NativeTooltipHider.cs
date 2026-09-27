using System;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Tipsy.Core.Tooltips;

namespace Tipsy.Game;

/// <summary>
/// Makes one tooltip addon transparent while Tipsy draws it. The addon is never hidden outright, because a hidden
/// addon stops drawing and the reader reads it at PreDraw. Unit alpha and the root node's alpha go to 0 when a
/// content update is requested, before the game works out what to draw, and again at every PreDraw. When the
/// snapshot no longer matches the map, the native tooltip is left visible instead. Only addons Tipsy can draw get a
/// hider, so turning replacement on never leaves a tooltip with nothing on screen.
/// </summary>
internal sealed unsafe class NativeTooltipHider : IDisposable
{
    private const byte Opaque = 255;

    private readonly IAddonLifecycle addonLifecycle;
    private readonly IGameGui gameGui;
    private readonly TooltipReader reader;
    private bool enabled;

    public NativeTooltipHider(IAddonLifecycle addonLifecycle, IGameGui gameGui, TooltipReader reader, bool enabled)
    {
        this.addonLifecycle = addonLifecycle;
        this.gameGui = gameGui;
        this.reader = reader;
        this.enabled = enabled;

        addonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, reader.Addon, OnRequestedUpdate);
        addonLifecycle.RegisterListener(AddonEvent.PreDraw, reader.Addon, OnPreDraw);
    }

    public bool Enabled
    {
        get => enabled;
        set
        {
            enabled = value;
            if (!value)
                Restore();
        }
    }

    public void Dispose()
    {
        addonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, reader.Addon, OnRequestedUpdate);
        addonLifecycle.UnregisterListener(AddonEvent.PreDraw, reader.Addon, OnPreDraw);
        Restore();
    }

    private void OnRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if (enabled)
            SetAlpha((AtkUnitBase*)args.Addon.Address, 0);
    }

    private void OnPreDraw(AddonEvent type, AddonArgs args)
    {
        if (!enabled)
            return;
        var matches = reader.Current?.Status != SnapshotStatus.SchemaMismatch;
        SetAlpha((AtkUnitBase*)args.Addon.Address, matches ? (byte)0 : Opaque);
    }

    private void Restore() => SetAlpha((AtkUnitBase*)gameGui.GetAddonByName(reader.Addon).Address, Opaque);

    private static void SetAlpha(AtkUnitBase* unit, byte alpha)
    {
        if (unit == null)
            return;
        unit->SetAlpha(alpha);
        if (unit->RootNode != null)
            unit->RootNode->Color.A = alpha;
    }
}
