using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace Tipsy.Game;

/// <summary>
/// Looks up an item's icon by its English name, for items the tooltip names but does not draw, such as materia. The
/// index is built in the background at load; until it is ready, lookups find nothing.
/// </summary>
internal sealed class ItemIcons
{
    private readonly Task<Dictionary<string, uint>> byName;

    public ItemIcons(IDataManager data)
    {
        byName = Task.Run(() => Build(data));
    }

    public uint? Find(string name) =>
        byName.IsCompletedSuccessfully && byName.Result.TryGetValue(name, out var icon) ? icon : null;

    private static Dictionary<string, uint> Build(IDataManager data)
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
