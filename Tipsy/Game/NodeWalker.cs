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
        Collect(&unit->UldManager, 0, [], records);
        return records;
    }

    /// <summary>A hash of every node's id, visibility and text bytes; it changes whenever a snapshot would.</summary>
    public static int Hash(AtkUnitBase* unit)
    {
        var hash = new HashCode();
        Hash(&unit->UldManager, ref hash);
        return hash.ToHashCode();
    }

    public static bool IsShown(AtkResNode* node)
    {
        for (var current = node; current != null; current = current->ParentNode)
        {
            if ((current->NodeFlags & NodeFlags.Visible) == 0)
                return false;
        }

        return true;
    }

    private static void Collect(AtkUldManager* manager, int depth, List<uint> components, List<NodeRecord> records)
    {
        for (var i = 0; i < manager->NodeListCount; i++)
        {
            var node = manager->NodeList[i];
            if (node == null)
                continue;
            var isComponent = (int)node->Type >= ComponentNodeType;
            var partId = 0;
            var texture = string.Empty;
            var text = Array.Empty<byte>();
            switch (node->Type)
            {
                case NodeType.Image:
                    var image = node->GetAsAtkImageNode();
                    partId = image->PartId;
                    texture = TexturePath(image);
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
                IsShown(node),
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
                text));

            if (!isComponent || node->GetAsAtkComponentNode()->Component == null)
                continue;
            components.Add(node->NodeId);
            Collect(&node->GetAsAtkComponentNode()->Component->UldManager, depth + 1, components, records);
            components.RemoveAt(components.Count - 1);
        }
    }

    private static void Hash(AtkUldManager* manager, ref HashCode hash)
    {
        for (var i = 0; i < manager->NodeListCount; i++)
        {
            var node = manager->NodeList[i];
            if (node == null)
                continue;
            hash.Add(node->NodeId);
            hash.Add(node->NodeFlags & NodeFlags.Visible);
            if (node->Type == NodeType.Text)
                hash.AddBytes(node->GetAsAtkTextNode()->NodeText.AsSpan());
            if ((int)node->Type >= ComponentNodeType && node->GetAsAtkComponentNode()->Component != null)
                Hash(&node->GetAsAtkComponentNode()->Component->UldManager, ref hash);
        }
    }

    private static string TexturePath(AtkImageNode* image)
    {
        var parts = image->PartsList;
        if (parts == null || image->PartId >= parts->PartCount)
            return string.Empty;
        var asset = parts->Parts[image->PartId].UldAsset;
        if (asset == null)
            return string.Empty;
        var texture = &asset->AtkTexture;
        if (texture->TextureType != TextureType.Resource || texture->Resource == null || texture->Resource->TexFileResourceHandle == null)
            return string.Empty;
        return texture->Resource->TexFileResourceHandle->FileName.ToString();
    }
}
