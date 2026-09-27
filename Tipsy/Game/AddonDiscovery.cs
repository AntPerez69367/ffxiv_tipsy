#if TIPSY_PROBE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Tipsy.Game;

/// <summary>How often one addon became visible and how long it stayed up.</summary>
internal sealed class AddonSightings
{
    public required string Name { get; init; }

    public int Shows { get; set; }

    public double TotalMs { get; set; }

    public double ShortestMs { get; set; } = double.MaxValue;

    public double LongestMs { get; set; }

    public double AverageMs => Shows == 0 ? 0 : TotalMs / Shows;
}

/// <summary>
/// Polls every loaded addon once per frame while enabled and records each time one becomes visible and for how long,
/// so short-lived hover windows stand out. The table is rewritten to discovery.tsv once a second when it changes.
/// </summary>
internal sealed unsafe class AddonDiscovery
{
    private static readonly TimeSpan WriteInterval = TimeSpan.FromSeconds(1);

    private readonly Dictionary<string, AddonSightings> sightings = [];
    private readonly Dictionary<string, DateTimeOffset> visibleSince = [];
    private readonly HashSet<string> seenThisFrame = [];
    private DateTimeOffset lastWrite = DateTimeOffset.MinValue;
    private bool dirty;

    public AddonDiscovery(string directory)
    {
        FilePath = Path.Combine(directory, "discovery.tsv");
    }

    public bool Enabled { get; set; }

    public string FilePath { get; }

    public IEnumerable<AddonSightings> Sightings => sightings.Values;

    public void Tick()
    {
        if (!Enabled)
        {
            visibleSince.Clear();
            return;
        }

        var now = DateTimeOffset.Now;
        seenThisFrame.Clear();
        var list = &AtkStage.Instance()->RaptureAtkUnitManager->AtkUnitManager.AllLoadedUnitsList;
        for (var i = 0; i < list->Count; i++)
        {
            var unit = list->Entries[i].Value;
            if (unit == null || !unit->IsVisible)
                continue;
            var name = unit->NameString;
            seenThisFrame.Add(name);
            if (visibleSince.TryAdd(name, now))
            {
                Get(name).Shows++;
                dirty = true;
            }
        }

        foreach (var name in visibleSince.Keys.Where(name => !seenThisFrame.Contains(name)).ToList())
        {
            var shown = (now - visibleSince[name]).TotalMilliseconds;
            visibleSince.Remove(name);
            var sighting = Get(name);
            sighting.TotalMs += shown;
            sighting.ShortestMs = Math.Min(sighting.ShortestMs, shown);
            sighting.LongestMs = Math.Max(sighting.LongestMs, shown);
            dirty = true;
        }

        if (dirty && now - lastWrite >= WriteInterval)
            Write(now);
    }

    public void Reset()
    {
        sightings.Clear();
        visibleSince.Clear();
        dirty = true;
        Write(DateTimeOffset.Now);
    }

    private AddonSightings Get(string name)
    {
        if (!sightings.TryGetValue(name, out var sighting))
        {
            sighting = new AddonSightings { Name = name };
            sightings[name] = sighting;
        }

        return sighting;
    }

    private void Write(DateTimeOffset now)
    {
        using var output = new StreamWriter(FilePath);
        output.WriteLine($"written {now:O}");
        output.WriteLine("addon\tshows\taverageMs\tshortestMs\tlongestMs\tvisibleNow");
        foreach (var sighting in sightings.Values.OrderByDescending(sighting => sighting.Shows))
        {
            var shortest = sighting.ShortestMs == double.MaxValue ? 0 : sighting.ShortestMs;
            output.WriteLine($"{sighting.Name}\t{sighting.Shows}\t{sighting.AverageMs:F0}\t{shortest:F0}\t{sighting.LongestMs:F0}\t{(visibleSince.ContainsKey(sighting.Name) ? 1 : 0)}");
        }

        lastWrite = now;
        dirty = false;
    }
}
#endif
