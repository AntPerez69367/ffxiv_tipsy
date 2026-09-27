namespace Tipsy.Core.Tooltips;

/// <summary>Ties a semantic slot to the node that fills it and the node type that node must have.</summary>
public sealed record SlotBinding(string Slot, string Path, string Type);

/// <summary>The known nodes of one tooltip addon, recorded against <see cref="GameVersion"/>.</summary>
public sealed record TooltipMap(string Addon, string GameVersion, IReadOnlyList<SlotBinding> Slots);
