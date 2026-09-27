#if TIPSY_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Tipsy.Core.Nodes;

namespace Tipsy.Game;

internal enum HideMethod
{
    Off,
    UnitAlpha,
    RootColor,
    Both,
    Early,
}

/// <summary>Frames the native tooltip was on screen while one hide method was active.</summary>
internal sealed class LeakCounters
{
    public int Opens { get; set; }

    public int Frames { get; set; }

    public int Leaked { get; set; }

    public int LeakedAtOpen { get; set; }

    public int MissedPreDraw { get; set; }
}

/// <summary>A visible text node of the current tooltip.</summary>
internal readonly record struct ProbeText(string Path, string Text);

/// <summary>
/// Dumps a tooltip addon's node tree to a file on the first draw after each content change while enabled,
/// applies one of the candidate ways of hiding the native tooltip at every draw, and counts the frames
/// where the tooltip reached the screen without being hidden. The counts are rewritten to a file each time
/// the tooltip closes, and the first frames after each open are traced to a second file.
/// </summary>
internal sealed unsafe class TooltipProbe : IDisposable
{
    private const int OpenFrames = 3;
    private const int TraceFrames = 10;

    private readonly IAddonLifecycle addonLifecycle;
    private readonly IGameGui gameGui;
    private readonly IPluginLog log;
    private readonly Func<string> hovered;
    private readonly string gameVersion;
    private readonly string resultsPath;
    private readonly StreamWriter trace;
    private readonly Dictionary<HideMethod, LeakCounters> counters = new()
    {
        [HideMethod.UnitAlpha] = new LeakCounters(),
        [HideMethod.RootColor] = new LeakCounters(),
        [HideMethod.Both] = new LeakCounters(),
        [HideMethod.Early] = new LeakCounters(),
    };

    private bool dumpPending;
    private string lastRows = string.Empty;
    private bool preDrawSinceRender;
    private bool wasVisible;
    private int framesSinceOpen;
    private int preUnitAlpha = -1;
    private int preRootColor = -1;
    private int preRootAlpha2 = -1;

    public TooltipProbe(string addonName, Func<string> hovered, IAddonLifecycle addonLifecycle, IGameGui gameGui, IPluginLog log, string directory, string gameVersion)
    {
        AddonName = addonName;
        this.hovered = hovered;
        this.addonLifecycle = addonLifecycle;
        this.gameGui = gameGui;
        this.log = log;
        this.gameVersion = gameVersion;
        Directory = directory;
        resultsPath = Path.Combine(directory, $"{addonName.ToLowerInvariant()}-leaks.tsv");
        System.IO.Directory.CreateDirectory(directory);
        var tracePath = Path.Combine(directory, $"{addonName.ToLowerInvariant()}-trace.tsv");
        var fresh = !File.Exists(tracePath);
        trace = new StreamWriter(new FileStream(tracePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
        if (fresh)
            trace.WriteLine("time	method	open	frame	sawPreDraw	preUnitAlpha	preRootColorA	preRootAlpha2	unitAlpha	rootColorA	rootAlpha2");

        addonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, AddonName, OnRequestedUpdate);
        addonLifecycle.RegisterListener(AddonEvent.PreDraw, AddonName, OnPreDraw);
    }

    public string AddonName { get; }

    public string Directory { get; }

    public bool DumpsEnabled { get; set; }

    public int DumpsWritten { get; private set; }

    public int DumpsSkipped { get; private set; }

    public string? LastDumpPath { get; private set; }

    public HideMethod Hide { get; private set; }

    public int PreDrawUnitAlpha { get; private set; } = -1;

    public int PreDrawRootAlpha { get; private set; } = -1;

    public int Overwrites { get; private set; }

    public IReadOnlyDictionary<HideMethod, LeakCounters> Counters => counters;

    public void Dispose()
    {
        addonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, AddonName, OnRequestedUpdate);
        addonLifecycle.UnregisterListener(AddonEvent.PreDraw, AddonName, OnPreDraw);
        SetHide(HideMethod.Off);
        trace.Dispose();
    }

    public void SetHide(HideMethod method)
    {
        if (Hide != HideMethod.Off && TryGetUnit(out var unit))
            Restore(unit);
        Hide = method;
        Overwrites = 0;
    }

    public void ResetCounters()
    {
        if (Hide == HideMethod.Off)
            return;
        counters[Hide] = new LeakCounters();
        Overwrites = 0;
        WriteResults();
    }

    public bool TryGetUnit(out AtkUnitBase* unit)
    {
        unit = (AtkUnitBase*)gameGui.GetAddonByName(AddonName).Address;
        return unit != null;
    }

    /// <summary>Called from the UI draw, after the game has rendered the frame.</summary>
    public void CheckRendered()
    {
        var visible = TryGetUnit(out var unit) && unit->IsVisible;
        var opened = visible && !wasVisible;
        var closed = !visible && wasVisible;
        wasVisible = visible;
        if (opened && DumpsEnabled)
            dumpPending = true;
        if (closed && Hide != HideMethod.Off)
            WriteResults();
        var sawPreDraw = preDrawSinceRender;
        preDrawSinceRender = false;
        if (!visible || Hide == HideMethod.Off)
            return;

        framesSinceOpen = opened ? 0 : framesSinceOpen + 1;
        var counter = counters[Hide];
        if (opened)
            counter.Opens++;
        counter.Frames++;
        if (!sawPreDraw)
            counter.MissedPreDraw++;
        var rootAlpha2 = unit->RootNode == null ? 0 : unit->RootNode->Alpha_2;
        if (framesSinceOpen < TraceFrames)
        {
            var rootColor = unit->RootNode == null ? -1 : unit->RootNode->Color.A;
            trace.WriteLine($"{DateTimeOffset.Now:HH:mm:ss.fff}\t{Hide}\t{counter.Opens}\t{framesSinceOpen}\t{(sawPreDraw ? 1 : 0)}\t{preUnitAlpha}\t{preRootColor}\t{preRootAlpha2}\t{unit->Alpha}\t{rootColor}\t{rootAlpha2}");
        }

        if (rootAlpha2 == 0)
            return;
        counter.Leaked++;
        if (framesSinceOpen < OpenFrames)
            counter.LeakedAtOpen++;
    }

    public List<ProbeText> VisibleText()
    {
        var texts = new List<ProbeText>();
        if (!TryGetUnit(out var unit) || !unit->IsVisible)
            return texts;
        foreach (var node in NodeWalker.Collect(unit))
        {
            if (node.Type != NodeRecord.TextType || !node.Shown || node.Text.Length == 0)
                continue;
            var text = SeString.Parse(node.Text).TextValue;
            if (text.Length > 0)
                texts.Add(new ProbeText(node.Path, text.ReplaceLineEndings("\\n")));
        }

        return texts;
    }

    private void OnRequestedUpdate(AddonEvent type, AddonArgs args)
    {
        if (DumpsEnabled)
            dumpPending = true;
        var unit = (AtkUnitBase*)args.Addon.Address;
        if (Hide == HideMethod.Early && unit != null)
            Apply(unit);
    }

    private void OnPreDraw(AddonEvent type, AddonArgs args)
    {
        var unit = (AtkUnitBase*)args.Addon.Address;
        if (unit == null)
            return;

        preDrawSinceRender = true;
        PreDrawUnitAlpha = unit->Alpha;
        PreDrawRootAlpha = unit->RootNode == null ? -1 : unit->RootNode->Color.A;
        preUnitAlpha = PreDrawUnitAlpha;
        preRootColor = PreDrawRootAlpha;
        preRootAlpha2 = unit->RootNode == null ? -1 : unit->RootNode->Alpha_2;
        if (Hide != HideMethod.Off)
            Reassert(unit);

        if (!dumpPending)
            return;
        dumpPending = false;
        Dump(unit);
    }

    private int ActiveValue(AtkUnitBase* unit)
    {
        var rootColor = unit->RootNode == null ? 0 : unit->RootNode->Color.A;
        return Hide switch
        {
            HideMethod.UnitAlpha => unit->Alpha,
            HideMethod.RootColor => rootColor,
            _ => Math.Max(unit->Alpha, rootColor),
        };
    }

    private void Reassert(AtkUnitBase* unit)
    {
        if (ActiveValue(unit) != 0)
            Overwrites++;
        Apply(unit);
    }

    private void Apply(AtkUnitBase* unit)
    {
        if (Hide != HideMethod.RootColor)
            unit->SetAlpha(0);
        if (Hide != HideMethod.UnitAlpha && unit->RootNode != null)
            unit->RootNode->Color.A = 0;
    }

    private static void Restore(AtkUnitBase* unit)
    {
        unit->SetAlpha(255);
        if (unit->RootNode != null)
            unit->RootNode->Color.A = 255;
    }

    private void Dump(AtkUnitBase* unit)
    {
        var rows = new StringWriter();
        NodeDump.WriteRows(NodeWalker.Collect(unit), bytes => SeString.Parse(bytes).TextValue, rows);
        var text = rows.ToString();
        if (text == lastRows)
        {
            DumpsSkipped++;
            return;
        }

        lastRows = text;
        var target = hovered();
        var path = Path.Combine(Directory, $"{AddonName.ToLowerInvariant()}-{DateTimeOffset.Now:yyyyMMdd-HHmmss-fff}-{target}.tsv");
        using var output = new StreamWriter(path);
        output.WriteLine($"game {gameVersion}\t{AddonName} {target}\thide {Hide}");
        output.WriteLine($"addon scale {unit->GetScale()} at {unit->GetX()},{unit->GetY()} alpha {PreDrawUnitAlpha} rootColorA {PreDrawRootAlpha}");
        output.WriteLine(NodeDump.ColumnHeader);
        output.Write(text);
        DumpsWritten++;
        LastDumpPath = path;
        log.Information($"Dumped {AddonName} for {target} to {path}");
    }

    private void WriteResults()
    {
        using var output = new StreamWriter(resultsPath);
        output.WriteLine($"game {gameVersion}\t{AddonName}\twritten {DateTimeOffset.Now:O}\toverwrites {Overwrites}");
        output.WriteLine("method\topens\tframes\tleaked\tleakedAtOpen\tnoPreDraw");
        foreach (var (method, counter) in counters)
            output.WriteLine($"{method}\t{counter.Opens}\t{counter.Frames}\t{counter.Leaked}\t{counter.LeakedAtOpen}\t{counter.MissedPreDraw}");
    }
}
#endif
