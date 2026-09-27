namespace Tipsy.Core.Nodes;

/// <summary>
/// One node of an addon tree as the game had it at draw time. <see cref="Path"/> joins the ids of the enclosing
/// component nodes and the node's own id with '/', so "32/12" is node 12 inside component node 32, because
/// node ids repeat between components. <see cref="Type"/> is the game's node type name, with component nodes
/// written as "Component" plus their numeric type. <see cref="Shown"/> is true when the node and every
/// ancestor are visible.
/// </summary>
public sealed record NodeRecord(
    int Depth,
    string Path,
    uint NodeId,
    string Type,
    uint ParentId,
    bool Visible,
    bool Shown,
    float X,
    float Y,
    float ScreenX,
    float ScreenY,
    float Width,
    float Height,
    float ScaleX,
    float ScaleY,
    int PartId,
    string Texture,
    byte[] Text)
{
    public const string TextType = "Text";
    public const string ImageType = "Image";
}
