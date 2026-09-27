using System;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Tipsy.Game;

/// <summary>
/// Makes one tooltip addon transparent while Tipsy draws its content. The addon is never hidden outright, because a
/// hidden addon stops drawing and the reader reads it at PreDraw. Alpha goes to 0 as soon as a content update is
/// requested, before the game works out what to draw, so a new tooltip never flashes. Each time the reader has read
/// the addon, the native tooltip is made opaque again and only hidden if the <see cref="TooltipSelector"/> says
/// Tipsy draws it this frame.
/// </summary>
internal sealed unsafe class NativeTooltipHider : IDisposable
{
    private const byte Opaque = 255;

    private readonly IAddonLifecycle addonLifecycle;
    private readonly IGameGui gameGui;
    private readonly TooltipReader reader;
    private readonly TooltipSelector selector;
    private bool enabled;

    public NativeTooltipHider(IAddonLifecycle addonLifecycle, IGameGui gameGui, TooltipReader reader, TooltipSelector selector, bool enabled)
    {
        this.addonLifecycle = addonLifecycle;
        this.gameGui = gameGui;
        this.reader = reader;
        this.selector = selector;
        this.enabled = enabled;

        addonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, reader.Addon, OnRequestedUpdate);
        reader.Drawing += OnDrawing;
    }

    public bool Enabled
    {
        get => enabled;
        set
        {
            enabled = value;
            if (!value)
                SetAlpha((AtkUnitBase*)gameGui.GetAddonByName(reader.Addon).Address, Opaque);
        }
    }

    public void Dispose()
    {
        addonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, reader.Addon, OnRequestedUpdate);
        reader.Drawing -= OnDrawing;
        SetAlpha((AtkUnitBase*)gameGui.GetAddonByName(reader.Addon).Address, Opaque);
    }

    private void OnRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if (enabled)
            SetAlpha((AtkUnitBase*)args.Addon.Address, 0);
    }

    private void OnDrawing(nint address)
    {
        if (!enabled)
            return;
        var unit = (AtkUnitBase*)address;
        SetAlpha(unit, Opaque);
        if (selector.Draws(reader.Addon))
            SetAlpha(unit, 0);
    }

    private static void SetAlpha(AtkUnitBase* unit, byte alpha)
    {
        if (unit == null)
            return;
        unit->SetAlpha(alpha);
        if (unit->RootNode != null)
            unit->RootNode->Color.A = alpha;
    }
}
