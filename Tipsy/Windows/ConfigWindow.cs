using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Tipsy.Core.Layout;

namespace Tipsy.Windows;

public sealed class ConfigWindow : Window
{
    private const string SwitchThemePopup = "Switch theme?##switchTheme";
    private const float MaxCursorOffset = 100;
    private const float MaxRounding = 16;

    private static readonly (AnchorPreset Anchor, string Label)[] Anchors =
    [
        (AnchorPreset.TopLeft, "Top left"),
        (AnchorPreset.TopCenter, "Top center"),
        (AnchorPreset.TopRight, "Top right"),
        (AnchorPreset.BottomLeft, "Bottom left"),
        (AnchorPreset.BottomRight, "Bottom right"),
        (AnchorPreset.Custom, "Custom position"),
        (AnchorPreset.Cursor, "Follow the cursor"),
    ];

    private static readonly (ThemeToken Token, string Label)[] Tokens =
    [
        (ThemeToken.Surface, "Background"),
        (ThemeToken.SurfaceAlpha, "Background opacity"),
        (ThemeToken.Border, "Border"),
        (ThemeToken.PrimaryText, "Text"),
        (ThemeToken.SecondaryText, "Secondary text"),
        (ThemeToken.Accent, "Accent"),
        (ThemeToken.Divider, "Divider"),
        (ThemeToken.DividerAlpha, "Divider opacity"),
        (ThemeToken.Better, "Better than equipped"),
        (ThemeToken.Worse, "Worse than equipped"),
        (ThemeToken.Rounding, "Corner rounding"),
    ];

    private readonly Configuration configuration;
    private readonly TooltipOverlay overlay;
    private readonly Action<bool> setReplaceTooltips;
    private ThemePreset? pendingPreset;
    private bool showSample;

    internal ConfigWindow(Configuration configuration, TooltipOverlay overlay, Action<bool> setReplaceTooltips) : base("Tipsy settings##config")
    {
        this.configuration = configuration;
        this.overlay = overlay;
        this.setReplaceTooltips = setReplaceTooltips;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void OnClose()
    {
        overlay.SampleBeside = null;
        overlay.Placing = false;
    }

    public override void Draw()
    {
        ImGui.Checkbox("Show sample tooltip", ref showSample);
        overlay.SampleBeside = showSample ? (ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize()) : null;
        ImGui.Spacing();
        if (!ImGui.BeginTabBar("settings"))
            return;
        Tab("General", DrawGeneral);
        Tab("Position", DrawPosition);
        Tab("Appearance", DrawAppearance);
        ImGui.EndTabBar();
    }

    private static void Tab(string label, Action draw)
    {
        if (!ImGui.BeginTabItem(label))
            return;
        draw();
        ImGui.EndTabItem();
    }

    private void DrawGeneral()
    {
        var replace = configuration.ReplaceTooltips;
        if (ImGui.Checkbox("Replace the game's tooltips", ref replace))
        {
            configuration.ReplaceTooltips = replace;
            configuration.Save();
            setReplaceTooltips(replace);
        }

        Hint("When off, only the game's own tooltips show.");
        ImGui.Spacing();

        if (!BeginRows("general", false))
            return;
        var width = configuration.Width;
        var tokens = new LayoutTokens();
        Row("Width");
        if (ImGui.SliderFloat("##width", ref width, tokens.MinWidth, tokens.MaxWidth, "%.0f px"))
            configuration.Width = width;
        SaveAfterEdit();
        ImGui.EndTable();
    }

    private void DrawPosition()
    {
        if (BeginRows("position", false))
        {
            Row("Position");
            var current = Array.Find(Anchors, entry => entry.Anchor == configuration.Anchor).Label;
            if (ImGui.BeginCombo("##anchor", current))
            {
                foreach (var (anchor, label) in Anchors)
                {
                    if (!ImGui.Selectable(label, anchor == configuration.Anchor))
                        continue;
                    configuration.Anchor = anchor;
                    configuration.Save();
                    if (anchor != AnchorPreset.Custom)
                        overlay.Placing = false;
                }

                ImGui.EndCombo();
            }

            if (configuration.Anchor == AnchorPreset.Cursor)
            {
                var offset = configuration.CursorOffset;
                Row("Right of the cursor");
                if (ImGui.SliderFloat("##offsetX", ref offset.X, -MaxCursorOffset, MaxCursorOffset, "%.0f px"))
                    configuration.CursorOffset = offset;
                SaveAfterEdit();
                Row("Below the cursor");
                if (ImGui.SliderFloat("##offsetY", ref offset.Y, -MaxCursorOffset, MaxCursorOffset, "%.0f px"))
                    configuration.CursorOffset = offset;
                SaveAfterEdit();
            }

            ImGui.EndTable();
        }

        ImGui.Spacing();
        if (ImGui.Button(overlay.Placing ? "Done placing" : "Place tooltip"))
        {
            overlay.Placing = !overlay.Placing;
            if (overlay.Placing && configuration.Anchor != AnchorPreset.Custom)
            {
                configuration.Anchor = AnchorPreset.Custom;
                configuration.Save();
            }
        }

        Hint(overlay.Placing ? "Drag the tooltip where you want it, then press Done placing." : "Drag the tooltip to a spot of your own.");
    }

    private void DrawAppearance()
    {
        var modified = configuration.ColourOverrides.Count + configuration.NumberOverrides.Count;
        if (BeginRows("theme", false))
        {
            Row("Theme");
            var name = ThemeColors.NameOf(configuration.Preset);
            var label = modified > 0 ? $"{name} (modified)" : name;
            if (ImGui.BeginCombo("##theme", label))
            {
                foreach (var preset in Enum.GetValues<ThemePreset>())
                {
                    if (!ImGui.Selectable(ThemeColors.NameOf(preset), preset == configuration.Preset) || preset == configuration.Preset)
                        continue;
                    if (modified > 0)
                        pendingPreset = preset;
                    else
                        SwitchTheme(preset, false);
                }

                ImGui.EndCombo();
            }

            ImGui.EndTable();
        }

        if (pendingPreset is not null)
            ImGui.OpenPopup(SwitchThemePopup);
        DrawSwitchThemePopup(modified);

        ImGui.BeginDisabled(modified == 0);
        if (ImGui.Button("Reset all colors"))
            ClearOverrides();
        ImGui.EndDisabled();

        var theme = configuration.Theme();
        DrawContrastWarning(theme);
        if (!ImGui.CollapsingHeader("Customize colors") || !BeginRows("tokens", true))
            return;
        foreach (var (token, tokenLabel) in Tokens)
            DrawToken(theme, token, tokenLabel);
        ImGui.EndTable();
    }

    private void DrawSwitchThemePopup(int modified)
    {
        var open = true;
        if (!ImGui.BeginPopupModal(SwitchThemePopup, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (!open)
                pendingPreset = null;
            return;
        }

        ImGui.TextUnformatted($"You have changed {modified} colors in {ThemeColors.NameOf(configuration.Preset)}.");
        if (ImGui.Button($"Keep them on {ThemeColors.NameOf(pendingPreset!.Value)}"))
        {
            SwitchTheme(pendingPreset!.Value, false);
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Discard them"))
        {
            SwitchTheme(pendingPreset!.Value, true);
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            pendingPreset = null;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private void SwitchTheme(ThemePreset preset, bool discardChanges)
    {
        configuration.Preset = preset;
        if (discardChanges)
            ClearOverrides();
        configuration.Save();
        pendingPreset = null;
    }

    private void ClearOverrides()
    {
        configuration.ColourOverrides.Clear();
        configuration.NumberOverrides.Clear();
        configuration.Save();
    }

    private static void DrawContrastWarning(ThemeColors theme)
    {
        var contrast = theme.SecondaryContrast();
        if (contrast >= ThemeColors.MinimumContrast)
            return;
        ImGui.PushTextWrapPos(0);
        ImGui.TextColored(new Vector4(1f, 0.6f, 0.4f, 1f), "Secondary text may be hard to read on this background.");
        ImGui.PopTextWrapPos();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip($"Contrast is {contrast:F1}:1. Text stays readable at {ThemeColors.MinimumContrast}:1 or more.");
    }

    private void DrawToken(ThemeColors theme, ThemeToken token, string label)
    {
        ImGui.PushID((int)token);
        Row(label);
        if (ThemeColors.IsColour(token))
        {
            var colour = ToVector(theme.ColourOf(token));
            if (ImGui.ColorEdit3("##value", ref colour, ImGuiColorEditFlags.DisplayHex))
                configuration.ColourOverrides[token] = FromVector(colour);
        }
        else if (token == ThemeToken.Rounding)
        {
            var rounding = theme.NumberOf(token);
            if (ImGui.SliderFloat("##value", ref rounding, 0, MaxRounding, "%.0f px"))
                configuration.NumberOverrides[token] = rounding;
        }
        else
        {
            var percent = theme.NumberOf(token) * 100;
            if (ImGui.SliderFloat("##value", ref percent, 0, 100, "%.0f%%"))
                configuration.NumberOverrides[token] = percent / 100;
        }

        SaveAfterEdit();
        ImGui.TableNextColumn();
        var overridden = configuration.ColourOverrides.ContainsKey(token) || configuration.NumberOverrides.ContainsKey(token);
        ImGui.BeginDisabled(!overridden);
        if (ImGui.Button("Reset"))
        {
            configuration.ColourOverrides.Remove(token);
            configuration.NumberOverrides.Remove(token);
            configuration.Save();
        }

        ImGui.EndDisabled();
        ImGui.PopID();
    }

    private static bool BeginRows(string id, bool withReset)
    {
        if (!ImGui.BeginTable(id, withReset ? 3 : 2, ImGuiTableFlags.SizingFixedFit))
            return false;
        ImGui.TableSetupColumn("label", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("control", ImGuiTableColumnFlags.WidthStretch);
        if (withReset)
            ImGui.TableSetupColumn("reset", ImGuiTableColumnFlags.WidthFixed);
        return true;
    }

    private static void Row(string label)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.TableNextColumn();
        ImGui.SetNextItemWidth(-float.Epsilon);
    }

    private static void Hint(string text)
    {
        ImGui.PushTextWrapPos(0);
        ImGui.TextDisabled(text);
        ImGui.PopTextWrapPos();
    }

    private void SaveAfterEdit()
    {
        if (ImGui.IsItemDeactivatedAfterEdit())
            configuration.Save();
    }

    private static Vector3 ToVector(uint rgb) => new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);

    private static uint FromVector(Vector3 colour) =>
        ((uint)MathF.Round(colour.X * 255) << 16) | ((uint)MathF.Round(colour.Y * 255) << 8) | (uint)MathF.Round(colour.Z * 255);
}
