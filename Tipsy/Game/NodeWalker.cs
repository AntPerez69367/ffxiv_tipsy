using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Tipsy.Core.Nodes;

namespace Tipsy.Game;

/// <summary>Turns an addon's node tree into <see cref="NodeRecord"/>s, and hashes the parts a snapshot depends on.</summary>
internal static unsafe class NodeWalker
{
    private const int ComponentNodeType = 1000;

    public static List<NodeRecord> Collect(AtkUnitBase* unit)
    {
        var records = new List<NodeRecord>();
        Collect(&unit->UldManager, unit->RootNode, 0, [], records);
        return records;
    }

    /// <summary>
    /// A hash of every node's id, visibility, position, whether it has a size, image part and text bytes; it changes
    /// whenever a snapshot would. The root's visibility and position are left out, as in <see cref="IsShown"/>, because
    /// hiding or moving the addon changes them without changing the content.
    /// </summary>
    public static int Hash(AtkUnitBase* unit)
    {
        var hash = new HashCode();
        Hash(&unit->UldManager, unit->RootNode, ref hash);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Whether the node and its ancestors below <paramref name="root"/> are visible. The root itself is skipped: setting
    /// an addon's alpha to 0 clears the root's visibility flag while the addon stays open and keeps its content.
    /// </summary>
    public static bool IsShown(AtkResNode* node, AtkResNode* root)
    {
        for (var current = node; current != null && current != root; current = current->ParentNode)
        {
            if ((current->NodeFlags & NodeFlags.Visible) == 0)
                return false;
        }

        return true;
    }

    private static void Collect(AtkUldManager* manager, AtkResNode* root, int depth, List<uint> components, List<NodeRecord> records)
    {
        for (var i = 0; i < manager->NodeListCount; i++)
        {
            var node = manager->NodeList[i];
            if (node == null)
                continue;
            var isComponent = (int)node->Type >= ComponentNodeType;
            var partId = 0;
            var texture = string.Empty;
            PartRect part = default;
            var text = Array.Empty<byte>();
            switch (node->Type)
            {
                case NodeType.Image:
                    var image = node->GetAsAtkImageNode();
                    partId = image->PartId;
                    (texture, part) = ImageOf(image);
                    break;
                case NodeType.Text:
                    text = node->GetAsAtkTextNode()->NodeText.AsSpan().ToArray();
                    break;
            }

            var parent = node->ParentNode;
            records.Add(new NodeRecord(
                depth,
                NodeDump.PathOf(components, node->NodeId),
                node->NodeId,
                isComponent ? $"Component{(int)node->Type}" : node->Type.ToString(),
                parent == null ? 0 : parent->NodeId,
                (node->NodeFlags & NodeFlags.Visible) != 0,
                IsShown(node, root),
                node->X,
                node->Y,
                node->ScreenX,
                node->ScreenY,
                node->Width,
                node->Height,
                node->ScaleX,
                node->ScaleY,
                partId,
                texture,
                text,
                part));

            if (!isComponent || node->GetAsAtkComponentNode()->Component == null)
                continue;
            components.Add(node->NodeId);
            Collect(&node->GetAsAtkComponentNode()->Component->UldManager, root, depth + 1, components, records);
            components.RemoveAt(components.Count - 1);
        }
    }

    private static void Hash(AtkUldManager* manager, AtkResNode* root, ref HashCode hash)
    {
        for (var i = 0; i < manager->NodeListCount; i++)
        {
            var node = manager->NodeList[i];
            if (node == null)
                continue;
            hash.Add(node->NodeId);
            if (node != root)
            {
                hash.Add(node->NodeFlags & NodeFlags.Visible);
                hash.Add(node->X);
                hash.Add(node->Y);
            }

            hash.Add(node->Width > 0 && node->Height > 0);
            if (node->Type == NodeType.Image)
                hash.Add(node->GetAsAtkImageNode()->PartId);
            if (node->Type == NodeType.Text)
                hash.AddBytes(node->GetAsAtkTextNode()->NodeText.AsSpan());
            if ((int)node->Type >= ComponentNodeType && node->GetAsAtkComponentNode()->Component != null)
                Hash(&node->GetAsAtkComponentNode()->Component->UldManager, root, ref hash);
        }
    }

    private static (string Texture, PartRect Part) ImageOf(AtkImageNode* image)
    {
        var parts = image->PartsList;
        if (parts == null || image->PartId >= parts->PartCount)
            return (string.Empty, default);
        var part = &parts->Parts[image->PartId];
        var rect = new PartRect(part->U, part->V, part->Width, part->Height);
        if (part->UldAsset == null)
            return (string.Empty, rect);
        var texture = &part->UldAsset->AtkTexture;
        if (texture->TextureType != TextureType.Resource || texture->Resource == null || texture->Resource->TexFileResourceHandle == null)
            return (string.Empty, rect);
        return (texture->Resource->TexFileResourceHandle->FileName.ToString(), rect);
    }
}
