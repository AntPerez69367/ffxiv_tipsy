namespace Tipsy.Core.Tooltips;

public enum SnapshotStatus
{
    Ok,
    SchemaMismatch,
}

/// <summary>A shown slot's content: the raw SeString bytes for text nodes, the texture path and part for image nodes.</summary>
public sealed record SlotValue(byte[] Text, string Texture, int PartId);

/// <summary>A shown text node the map does not know, with where the game drew it.</summary>
public sealed record ExtraLine(string Path, byte[] Text, float ScreenX, float ScreenY);

/// <summary>
/// What one tooltip showed at draw time. <see cref="Slots"/> holds only the slots whose node was shown.
/// When the tree no longer matches the map, <see cref="Status"/> is <see cref="SnapshotStatus.SchemaMismatch"/>,
/// <see cref="Mismatch"/> says which binding failed, and every shown text line is in <see cref="Extras"/>.
/// </summary>
public sealed record TooltipSnapshot(
    string Addon,
    SnapshotStatus Status,
    string? Mismatch,
    IReadOnlyDictionary<string, SlotValue> Slots,
    IReadOnlyList<ExtraLine> Extras);
