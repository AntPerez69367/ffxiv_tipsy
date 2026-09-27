using Tipsy.Core.Nodes;

namespace Tipsy.Core.Tooltips;

/// <summary>The generic Tooltip addon used for status effects, currencies and UI buttons: one text node on a background.</summary>
public static class TextTooltipMap
{
    public const string Addon = "Tooltip";
    public const string Text = "Text";

    public static readonly TooltipMap Map = new(Addon, "2026.09.15.0000.0000", [new SlotBinding(Text, "2", NodeRecord.TextType)]);
}
