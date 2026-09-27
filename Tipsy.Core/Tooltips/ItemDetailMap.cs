namespace Tipsy.Core.Tooltips;

/// <summary>The ItemDetail addon's known nodes, taken from the probe dumps of 2026-09-27.</summary>
public static class ItemDetailMap
{
    public const string Addon = "ItemDetail";

    public const string Icon = "Header.Icon";
    public const string IconCooldown = "Header.IconCooldown";
    public const string Name = "Header.Name";
    public const string Category = "Header.Category";
    public const string Owned = "Header.Owned";
    public const string Unique = "Header.Unique";
    public const string Untradable = "Header.Untradable";
    public const string Binding = "Header.Binding";
    public const string RecipeIcon = "Header.RecipeIcon";
    public const string CraftedBy = "Header.CraftedBy";
    public const string ItemLevel = "Header.ItemLevel";
    public const string Classes = "Header.Classes";
    public const string Level = "Header.Level";
    public const string Description = "Description";
    public const string EffectsHeader = "Effects.Header";
    public const string Effects = "Effects.Text";
    public const string BonusesHeader = "Bonuses.Header";
    public const string MateriaHeader = "Materia.Header";
    public const string RepairsHeader = "Repairs.Header";
    public const string RepairsJobIcon = "Repairs.JobIcon";
    public const string RepairsJobLevel = "Repairs.JobLevel";
    public const string RepairsFlags = "Repairs.Flags";
    public const string RequirementsHeader = "Requirements.Header";
    public const string ShopPrice = "Footer.ShopPrice";
    public const string SellsFor = "Footer.SellsFor";
    public const string ControlHint = "Footer.ControlHint";

    public const int ParamCount = 3;
    public const int BonusRows = 4;
    public const int MateriaSlots = 5;

    public static readonly IReadOnlyList<string> StorageIcons = ["Header.Storage1", "Header.Storage2", "Header.Storage3", "Header.Storage4"];

    public static readonly IReadOnlyList<(string Label, string Value)> RepairRows =
    [
        ("Repairs.Condition.Label", "Repairs.Condition.Value"),
        ("Repairs.Spiritbond.Label", "Repairs.Spiritbond.Value"),
        ("Repairs.RepairLevel.Label", "Repairs.RepairLevel.Value"),
        ("Repairs.Materials.Label", "Repairs.Materials.Value"),
        ("Repairs.QuickRepairs.Label", "Repairs.QuickRepairs.Value"),
        ("Repairs.MateriaMelding.Label", "Repairs.MateriaMelding.Value"),
    ];

    public static readonly IReadOnlyList<(string Label, string Value)> RequirementRows =
    [
        ("Requirements.1.Label", "Requirements.1.Value"),
        ("Requirements.2.Label", "Requirements.2.Value"),
    ];

    private static readonly string[] ParamNodes = ["39", "38", "37"];
    private static readonly string[] BonusNodes = ["100", "1000101", "1000102", "1000103"];
    private static readonly string[] MateriaNodes = ["96", "960101", "960102", "960103", "960104"];
    private static readonly string[] StorageNodes = ["26", "28", "29", "31"];
    private static readonly (string Label, string Value)[] RepairNodes = [("75", "76"), ("78", "79"), ("81", "82"), ("84", "85"), ("87", "88"), ("90", "91")];
    private static readonly (string Label, string Value)[] RequirementNodes = [("57", "58"), ("60", "61")];

    public static readonly TooltipMap Map = new(Addon, "2026.09.15.0000.0000", Bindings());

    public static string ParamLabel(int index) => $"Params.{index + 1}.Label";

    public static string ParamValue(int index) => $"Params.{index + 1}.Value";

    public static string ParamDelta(int index) => $"Params.{index + 1}.Delta";

    public static string BonusLeft(int row) => $"Bonuses.{row + 1}.Left";

    public static string BonusRight(int row) => $"Bonuses.{row + 1}.Right";

    public static string MateriaName(int slot) => $"Materia.{slot + 1}.Name";

    public static string MateriaSocket(int slot) => $"Materia.{slot + 1}.Socket";

    public static string MateriaEffect(int slot) => $"Materia.{slot + 1}.Effect";

    private static List<SlotBinding> Bindings()
    {
        List<SlotBinding> bindings =
        [
            Image(Icon, "32/12"),
            Text(IconCooldown, "6"),
            Text(Name, "33"),
            Text(Category, "35"),
            Text(Owned, "34"),
            Text(Unique, "21"),
            Text(Untradable, "23"),
            Text(Binding, "22"),
            Image(RecipeIcon, "19"),
            Text(CraftedBy, "4"),
            Text(ItemLevel, "63"),
            Text(Classes, "65/2"),
            Text(Level, "66/2"),
            Text(Description, "42"),
            Text(EffectsHeader, "50"),
            Text(Effects, "52"),
            Text(BonusesHeader, "98"),
            Text(MateriaHeader, "94"),
            Text(RepairsHeader, "69"),
            Image(RepairsJobIcon, "72"),
            Text(RepairsJobLevel, "73"),
            Text(RepairsFlags, "92"),
            Text(RequirementsHeader, "54"),
            Text(ShopPrice, "44"),
            Text(SellsFor, "48"),
            Text(ControlHint, "3/2"),
        ];

        for (var i = 0; i < StorageNodes.Length; i++)
            bindings.Add(Image(StorageIcons[i], StorageNodes[i]));
        for (var i = 0; i < ParamNodes.Length; i++)
        {
            bindings.Add(Text(ParamLabel(i), $"{ParamNodes[i]}/2"));
            bindings.Add(Text(ParamValue(i), $"{ParamNodes[i]}/3"));
            bindings.Add(Text(ParamDelta(i), $"{ParamNodes[i]}/4"));
        }

        for (var i = 0; i < BonusNodes.Length; i++)
        {
            bindings.Add(Text(BonusLeft(i), $"{BonusNodes[i]}/2"));
            bindings.Add(Text(BonusRight(i), $"{BonusNodes[i]}/3"));
        }

        for (var i = 0; i < MateriaNodes.Length; i++)
        {
            bindings.Add(Text(MateriaName(i), $"{MateriaNodes[i]}/5"));
            bindings.Add(Text(MateriaEffect(i), $"{MateriaNodes[i]}/6"));
            bindings.Add(Image(MateriaSocket(i), $"{MateriaNodes[i]}/2"));
        }

        for (var i = 0; i < RepairNodes.Length; i++)
        {
            bindings.Add(Text(RepairRows[i].Label, RepairNodes[i].Label));
            bindings.Add(Text(RepairRows[i].Value, RepairNodes[i].Value));
        }

        for (var i = 0; i < RequirementNodes.Length; i++)
        {
            bindings.Add(Text(RequirementRows[i].Label, RequirementNodes[i].Label));
            bindings.Add(Text(RequirementRows[i].Value, RequirementNodes[i].Value));
        }

        return bindings;
    }

    private static SlotBinding Text(string slot, string path) => new(slot, path, "Text");

    private static SlotBinding Image(string slot, string path) => new(slot, path, "Image");
}
