namespace Tipsy.Core.Layout;

/// <summary>One piece of the tooltip window, top to bottom. SeString content stays as raw bytes so game colours survive.</summary>
public abstract record TooltipBlock;

/// <summary>The item header. <see cref="IconCooldown"/> is the remaining recast the game draws over the icon, or empty.</summary>
public sealed record HeaderBlock(string? IconTexture, string IconCooldown, byte[] Name, IReadOnlyList<byte[]> Lines, IReadOnlyList<byte[]> Flags) : TooltipBlock;

public sealed record ParamsBlock(IReadOnlyList<ParamValue> Params) : TooltipBlock;

public sealed record CaptionBlock(string Text) : TooltipBlock;

public sealed record StatTableBlock(IReadOnlyList<LabelledValue> Stats) : TooltipBlock;

public sealed record MateriaBlock(IReadOnlyList<LabelledValue> Materia) : TooltipBlock;

public sealed record BarBlock(string Label, string Value, float Fraction) : TooltipBlock;

public sealed record KeyValueBlock(string Key, byte[] Value) : TooltipBlock;

public sealed record ParagraphBlock(byte[] Text, bool Secondary) : TooltipBlock;

public sealed record DividerBlock : TooltipBlock;

public sealed record WarningBlock(string Text) : TooltipBlock;

public readonly record struct LabelledValue(string Label, string Value);

/// <summary>A main param such as Physical Damage, with the game's difference from the equipped item, like "(-705)", or empty.</summary>
public readonly record struct ParamValue(string Label, string Value, string Delta);
