using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;

namespace Tipsy.Game;

/// <summary>
/// Looks up an item's icon by its name in the client's language, for items the tooltip names but does not draw, such as
/// materia. The index is built in the background at load; until it is ready, or if building it failed, lookups find nothing.
/// </summary>
internal sealed class ItemIcons
{
    private readonly Task<Dictionary<string, uint>> byName;

    public ItemIcons(IDataManager data, IPluginLog log)
    {
        byName = Task.Run(() => Build(data, log));
    }

    public uint? Find(string name) =>
        byName.IsCompleted && byName.Result.TryGetValue(name, out var icon) ? icon : null;

    private static Dictionary<string, uint> Build(IDataManager data, IPluginLog log)
    {
        var icons = new Dictionary<string, uint>();
        try
        {
            foreach (var item in data.GetExcelSheet<Item>())
            {
                var name = item.Name.ExtractText();
                if (name.Length > 0)
                    icons.TryAdd(name, item.Icon);
            }
        }
        catch (Exception exception)
        {
            log.Error(exception, "Could not index item icons; materia will show without icons.");
            icons.Clear();
        }

        return icons;
    }
}
