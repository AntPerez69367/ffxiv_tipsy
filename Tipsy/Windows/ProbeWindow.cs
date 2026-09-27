using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Tipsy.Core.Tooltips;
using Tipsy.Game;

namespace Tipsy.Windows;

public sealed class ProbeWindow : Window
{
    private const int DiscoveryRows = 15;

    private readonly IReadOnlyList<TooltipProbe> probes;
    private readonly IReadOnlyList<TooltipReader> readers;
    private readonly AddonDiscovery discovery;
    private TooltipProbe probe;

    internal ProbeWindow(IReadOnlyList<TooltipProbe> probes, IReadOnlyList<TooltipReader> readers, AddonDiscovery discovery) : base("Tipsy probe##probe")
    {
        this.probes = probes;
        this.readers = readers;
        this.discovery = discovery;
        probe = probes[0];
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(520, 420),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw()
    {
        if (!ImGui.BeginTabBar("addons"))
            return;
        if (ImGui.BeginTabItem("Discovery"))
        {
            DrawDiscovery();
            ImGui.EndTabItem();
        }

        foreach (var candidate in probes)
        {
            if (!ImGui.BeginTabItem(candidate.AddonName))
                continue;
            probe = candidate;
            DrawProbe();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void DrawDiscovery()
    {
        var enabled = discovery.Enabled;
        if (ImGui.Checkbox("Record every addon that becomes visible", ref enabled))
            discovery.Enabled = enabled;
        ImGui.TextUnformatted($"{discovery.Sightings.Count()} addons seen, saved to {discovery.FilePath}");
        if (ImGui.Button("Reset"))
            discovery.Reset();
        ImGui.SameLine();
        if (ImGui.Button("Open folder"))
            Process.Start(new ProcessStartInfo(probe.Directory) { UseShellExecute = true });

        if (!ImGui.BeginTable("discovery", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            return;
        foreach (var header in new[] { "Addon", "Shows", "Average ms", "Shortest ms", "Longest ms" })
            ImGui.TableSetupColumn(header);
        ImGui.TableHeadersRow();
        foreach (var sighting in discovery.Sightings.OrderByDescending(sighting => sighting.Shows).Take(DiscoveryRows))
        {
            ImGui.TableNextRow();
            Cell(sighting.Name);
            Cell(sighting.Shows.ToString());
            Cell(sighting.AverageMs.ToString("F0"));
            Cell(sighting.ShortestMs == double.MaxValue ? "-" : sighting.ShortestMs.ToString("F0"));
            Cell(sighting.LongestMs.ToString("F0"));
        }

        ImGui.EndTable();
    }

    private void DrawProbe()
    {
        DrawDumps();
        ImGui.Separator();
        DrawHide();
        ImGui.Separator();
        if (readers.FirstOrDefault(reader => reader.Addon == probe.AddonName) is { } reader)
        {
            DrawSnapshot(reader);
            ImGui.Separator();
        }

        DrawLiveText();
    }

    private static void DrawSnapshot(TooltipReader reader)
    {
        if (!ImGui.CollapsingHeader("Reader snapshot"))
            return;
        if (reader.Current is not { } snapshot)
        {
            ImGui.TextUnformatted("No snapshot yet.");
            return;
        }

        ImGui.TextUnformatted($"{snapshot.Status}, {snapshot.Slots.Count} slots, {snapshot.Extras.Count} extras, {reader.Rebuilds} rebuilds");
        if (snapshot.Mismatch is { } mismatch)
            ImGui.TextWrapped($"Mismatch: {mismatch}");

        if (!ImGui.BeginTable("snapshot", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            return;
        ImGui.TableSetupColumn("Slot", ImGuiTableColumnFlags.WidthFixed, 220);
        ImGui.TableSetupColumn("Content");
        ImGui.TableHeadersRow();
        foreach (var (slot, value) in snapshot.Slots)
            Row(slot, value.Text.Length > 0 ? Plain(value.Text) : $"{value.Texture} part {value.PartId}");
        foreach (var extra in snapshot.Extras)
            Row($"extra {extra.Path}", Plain(extra.Text));
        ImGui.EndTable();
    }

    private static void Row(string label, string content)
    {
        ImGui.TableNextRow();
        Cell(label);
        ImGui.TableNextColumn();
        ImGui.TextWrapped(content);
    }

    private static string Plain(byte[] text) => SeString.Parse(text).TextValue;

    private void DrawDumps()
    {
        var enabled = probe.DumpsEnabled;
        if (ImGui.Checkbox("Dump the tooltip on every content change", ref enabled))
            probe.DumpsEnabled = enabled;
        ImGui.TextUnformatted($"{probe.DumpsWritten} written, {probe.DumpsSkipped} skipped as unchanged");
        ImGui.TextUnformatted($"Last: {probe.LastDumpPath ?? "none yet"}");
        if (ImGui.Button("Open folder"))
            Process.Start(new ProcessStartInfo(probe.Directory) { UseShellExecute = true });
    }

    private void DrawHide()
    {
        ImGui.TextUnformatted("Hide the native tooltip");
        HideOption("Off", HideMethod.Off);
        ImGui.SameLine();
        HideOption("Unit alpha", HideMethod.UnitAlpha);
        ImGui.SameLine();
        HideOption("Root color", HideMethod.RootColor);
        ImGui.SameLine();
        HideOption("Both", HideMethod.Both);
        ImGui.SameLine();
        HideOption("Early", HideMethod.Early);

        ImGui.TextUnformatted($"At PreDraw: unit alpha {probe.PreDrawUnitAlpha}, root color A {probe.PreDrawRootAlpha}");
        ImGui.TextUnformatted($"Game overwrote the active value {probe.Overwrites} times");

        if (ImGui.BeginTable("leaks", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            foreach (var header in new[] { "Method", "Opens", "Frames", "Leaked", "Leaked at open", "No PreDraw" })
                ImGui.TableSetupColumn(header);
            ImGui.TableHeadersRow();
            foreach (var (method, counter) in probe.Counters)
            {
                ImGui.TableNextRow();
                Cell(method.ToString());
                Cell(counter.Opens.ToString());
                Cell(counter.Frames.ToString());
                Cell(counter.Leaked.ToString());
                Cell(counter.LeakedAtOpen.ToString());
                Cell(counter.MissedPreDraw.ToString());
            }

            ImGui.EndTable();
        }

        if (probe.Hide != HideMethod.Off && ImGui.Button($"Reset {probe.Hide} counters"))
            probe.ResetCounters();
    }

    private void HideOption(string label, HideMethod method)
    {
        if (ImGui.RadioButton(label, probe.Hide == method))
            probe.SetHide(method);
    }

    private void DrawLiveText()
    {
        var texts = probe.VisibleText();
        if (texts.Count == 0)
        {
            ImGui.TextUnformatted("No tooltip open.");
            return;
        }

        if (!ImGui.BeginTable("text", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
            return;
        ImGui.TableSetupColumn("Node", ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Text");
        ImGui.TableHeadersRow();
        foreach (var text in texts)
        {
            ImGui.TableNextRow();
            Cell(text.Path);
            ImGui.TableNextColumn();
            ImGui.TextWrapped(text.Text);
        }

        ImGui.EndTable();
    }

    private static void Cell(string text)
    {
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(text);
    }
}
