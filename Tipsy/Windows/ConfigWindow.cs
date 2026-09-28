using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using Tipsy.Core.Layout;

namespace Tipsy.Windows;

public sealed class ConfigWindow : Window
{
    private const string KofiUrl = "https://ko-fi.com/elserie";
    private const string SwitchThemePopup = "Switch theme?##switchTheme";

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
        (ThemeToken.SecondaryText, "Labels and details"),
        (ThemeToken.Accent, "Headings and bars"),
        (ThemeToken.Divider, "Lines and bar track"),
        (ThemeToken.DividerAlpha, "Line opacity"),
        (ThemeToken.Better, "Better than equipped"),
        (ThemeToken.Worse, "Worse than equipped"),
        (ThemeToken.Rounding, "Corner rounding"),
    ];

    private readonly Configuration configuration;
    private readonly TooltipOverlay overlay;
    private readonly Action<bool> setReplaceTooltips;
    private readonly IKeyState keyState;
    private bool capturingKey;
    private ThemePreset? pendingPreset;
    private bool showSample;

    internal ConfigWindow(Configuration configuration, TooltipOverlay overlay, IKeyState keyState, Action<bool> setReplaceTooltips) : base("Tipsy settings##config")
    {
        this.keyState = keyState;
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
        showSample = false;
        capturingKey = false;
    }

    public override void Draw()
    {
        ImGui.Checkbox("Preview tooltip", ref showSample);
        overlay.SampleBeside = showSample ? (ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize()) : null;
        ImGui.Spacing();
        if (!ImGui.BeginTabBar("settings"))
            return;
        Tab("General", DrawGeneral);
        var onPosition = Tab("Position", DrawPosition);
        Tab("Appearance", DrawAppearance);
        if (!onPosition && overlay.Placing)
            overlay.Placing = false;
        ImGui.EndTabBar();

        ImGui.Spacing();
        if (ImGui.Button("Support on Ko-fi"))
            Util.OpenLink(KofiUrl);
    }

    private static bool Tab(string label, Action draw)
    {
        if (!ImGui.BeginTabItem(label))
            return false;
        draw();
        ImGui.EndTabItem();
        return true;
    }

    private void DrawGeneral()
    {
        var replace = configuration.ReplaceTooltips;
        if (ImGui.Checkbox("Use Tipsy tooltips", ref replace))
        {
            configuration.ReplaceTooltips = replace;
            configuration.Save();
            setReplaceTooltips(replace);
        }

        Hint("Off shows the game's own tooltips.");
        ImGui.Spacing();

        if (!BeginRows("general", false))
            return;
        Row("Hold to hide");
        DrawHideKey();
        ImGui.EndTable();
        Hint(capturingKey
            ? "Press the key to use. Esc cancels, Backspace sets no key."
            : "Hold this key to hide Tipsy's tooltip when it covers something.");
    }

    private void DrawHideKey()
    {
        if (capturingKey)
            CaptureHideKey();
        var name = configuration.HideKey == VirtualKey.NO_KEY ? "Not set" : configuration.HideKey.GetFancyName();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(capturingKey ? "Press a key..." : name);
        ImGui.SameLine();
        if (ImGui.Button(capturingKey ? "Cancel" : "Change key"))
            capturingKey = !capturingKey;
    }

    private void CaptureHideKey()
    {
        foreach (var key in keyState.GetValidVirtualKeys())
        {
            if (key is VirtualKey.LBUTTON or VirtualKey.RBUTTON or VirtualKey.MBUTTON || !keyState[key])
                continue;
            if (key != VirtualKey.ESCAPE)
            {
                configuration.HideKey = key == VirtualKey.BACK ? VirtualKey.NO_KEY : key;
                configuration.Save();
            }

            keyState[key] = false;
            capturingKey = false;
            return;
        }
    }

    private void DrawPosition()
    {
        if (BeginRows("position", false))
        {
            Row("Position");
            var current = Anchors.First(entry => entry.Anchor == configuration.Anchor).Label;
            if (ImGui.BeginCombo("##anchor", current))
            {
                foreach (var (anchor, label) in Anchors)
                {
                    if (!ImGui.Selectable(label, anchor == configuration.Anchor))
                        continue;
                    if (anchor == AnchorPreset.Custom && configuration.Anchor != AnchorPreset.Custom)
                        overlay.Placing = true;
                    else if (anchor != AnchorPreset.Custom)
                        overlay.Placing = false;
                    configuration.Anchor = anchor;
                    configuration.Save();
                }

                ImGui.EndCombo();
            }

            if (configuration.Anchor == AnchorPreset.Cursor)
            {
                var offset = configuration.CursorOffset;
                Row("Horizontal offset");
                if (ImGui.SliderFloat("##offsetX", ref offset.X, -Configuration.MaxCursorOffset, Configuration.MaxCursorOffset, "%.0f px", ImGuiSliderFlags.AlwaysClamp))
                    configuration.CursorOffset = offset;
                SaveAfterEdit();
                Row("Vertical offset");
                if (ImGui.SliderFloat("##offsetY", ref offset.Y, -Configuration.MaxCursorOffset, Configuration.MaxCursorOffset, "%.0f px", ImGuiSliderFlags.AlwaysClamp))
                    configuration.CursorOffset = offset;
                SaveAfterEdit();
            }

            ImGui.EndTable();
        }

        if (configuration.Anchor != AnchorPreset.Custom)
            return;
        ImGui.Spacing();
        if (ImGui.Button(overlay.Placing ? "Done" : "Move tooltip..."))
            overlay.Placing = !overlay.Placing;
        if (overlay.Placing)
            Hint("Drag the tooltip where you want it, then press Done.");
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

            var width = configuration.Width;
            var tokens = new LayoutTokens();
            Row("Width");
            if (ImGui.SliderFloat("##width", ref width, tokens.MinWidth, tokens.MaxWidth, "%.0f px", ImGuiSliderFlags.AlwaysClamp))
                configuration.Width = width;
            SaveAfterEdit();
            ImGui.EndTable();
        }

        if (pendingPreset is not null)
            ImGui.OpenPopup(SwitchThemePopup);
        DrawSwitchThemePopup();

        ImGui.BeginDisabled(modified == 0);
        if (ImGui.Button("Reset customizations"))
        {
            configuration.ClearOverrides();
            configuration.Save();
        }
        ImGui.EndDisabled();

        var theme = configuration.Theme();
        DrawContrastWarning(theme);
        if (!ImGui.CollapsingHeader("Customize") || !BeginRows("tokens", true))
            return;
        foreach (var (token, tokenLabel) in Tokens)
            DrawToken(theme, token, tokenLabel);
        ImGui.EndTable();
    }

    private void DrawSwitchThemePopup()
    {
        var open = true;
        if (!ImGui.BeginPopupModal(SwitchThemePopup, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (!open)
                pendingPreset = null;
            return;
        }

        var preset = pendingPreset!.Value;
        ImGui.TextUnformatted($"You've customized {ThemeColors.NameOf(configuration.Preset)}.");
        if (ImGui.Button("Keep my changes"))
        {
            SwitchTheme(preset, false);
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button($"Use {ThemeColors.NameOf(preset)} as it comes"))
        {
            SwitchTheme(preset, true);
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
            configuration.ClearOverrides();
        configuration.Save();
        pendingPreset = null;
    }

    private static void DrawContrastWarning(ThemeColors theme)
    {
        var contrast = theme.SecondaryContrast();
        if (contrast >= ThemeColors.MinimumContrast)
            return;
        ImGui.PushTextWrapPos(0);
        ImGui.TextColored(new Vector4(1f, 0.6f, 0.4f, 1f), "Labels and details may be hard to read on this background.");
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
            var colour = Rgb.ToVector3(theme.ColourOf(token));
            if (ImGui.ColorEdit3("##value", ref colour, ImGuiColorEditFlags.DisplayHex))
                configuration.SetColour(token, Rgb.FromVector3(colour));
        }
        else if (token == ThemeToken.Rounding)
        {
            var rounding = theme.NumberOf(token);
            if (ImGui.SliderFloat("##value", ref rounding, 0, Configuration.MaxRounding, "%.0f px", ImGuiSliderFlags.AlwaysClamp))
                configuration.SetNumber(token, rounding);
        }
        else
        {
            var percent = theme.NumberOf(token) * 100;
            if (ImGui.SliderFloat("##value", ref percent, 0, 100, "%.0f%%", ImGuiSliderFlags.AlwaysClamp))
                configuration.SetNumber(token, percent / 100);
        }

        SaveAfterEdit();
        ImGui.TableNextColumn();
        var overridden = configuration.ColourOverrides.ContainsKey(token) || configuration.NumberOverrides.ContainsKey(token);
        ImGui.BeginDisabled(!overridden);
        if (ImGui.Button("Reset"))
        {
            configuration.ResetToken(token);
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
}
