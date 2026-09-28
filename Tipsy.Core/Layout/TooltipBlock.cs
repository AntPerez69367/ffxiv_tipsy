using Tipsy.Core.Text;

namespace Tipsy.Core.Layout;

/// <summary>One piece of the tooltip window, top to bottom. SeString content stays as raw bytes so game colours survive.</summary>
public abstract record TooltipBlock;

/// <summary>
/// The header of an item or action. <see cref="IconCooldown"/> is the remaining recast the game draws over the icon,
/// and <see cref="Keybind"/> the hotbar key shown by the text tooltip that opens alongside; either may be empty.
/// </summary>
public sealed record HeaderBlock(string? IconTexture, string IconCooldown, SeText Name, EquatableList<SeText> Lines, EquatableList<SeText> Flags) : TooltipBlock
{
    public string Keybind { get; init; } = string.Empty;
}

public sealed record ParamsBlock(EquatableList<ParamValue> Params) : TooltipBlock;

public sealed record CaptionBlock(string Text) : TooltipBlock;

public sealed record StatTableBlock(EquatableList<LabelledValue> Stats) : TooltipBlock;

public sealed record MateriaBlock(EquatableList<LabelledValue> Materia) : TooltipBlock;

public sealed record BarBlock(string Label, string Value, float Fraction) : TooltipBlock;

public sealed record KeyValueBlock(string Key, SeText Value) : TooltipBlock;

public sealed record ParagraphBlock(SeText Text, bool Secondary) : TooltipBlock;

/// <summary>A line another plugin or the game added that Tipsy has no place for, shown in the last section.</summary>
public sealed record ExtraBlock(SeText Text) : TooltipBlock;

public sealed record DividerBlock : TooltipBlock;

public sealed record WarningBlock(string Text) : TooltipBlock;

public readonly record struct LabelledValue(string Label, string Value);

/// <summary>A main param such as Physical Damage, with the game's difference from the equipped item, like "(-705)", or empty.</summary>
public readonly record struct ParamValue(string Label, string Value, string Delta);
