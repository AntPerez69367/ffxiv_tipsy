using System.Collections.Generic;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace Tipsy.Game;

/// <summary>Looks up an item's icon by its English name, for items the tooltip names but does not draw, such as materia.</summary>
internal sealed class ItemIcons
{
    private readonly IDataManager data;
    private Dictionary<string, uint>? byName;

    public ItemIcons(IDataManager data)
    {
        this.data = data;
    }

    public uint? Find(string name)
    {
        byName ??= Build();
        return byName.TryGetValue(name, out var icon) ? icon : null;
    }

    private Dictionary<string, uint> Build()
    {
        var icons = new Dictionary<string, uint>();
        foreach (var item in data.GetExcelSheet<Item>())
        {
            var name = item.Name.ExtractText();
            if (name.Length > 0)
                icons.TryAdd(name, item.Icon);
        }

        return icons;
    }
}
