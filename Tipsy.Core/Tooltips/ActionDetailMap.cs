namespace Tipsy.Core.Tooltips;

/// <summary>The ActionDetail addon's known nodes, taken from the probe dumps of 2026-09-27.</summary>
public static class ActionDetailMap
{
    public const string Addon = "ActionDetail";

    public const string Icon = "Header.Icon";
    public const string IconCooldown = "Header.IconCooldown";
    public const string Name = "Header.Name";
    public const string Category = "Header.Category";
    public const string RangeLabel = "Header.Range.Label";
    public const string RangeValue = "Header.Range.Value";
    public const string RadiusLabel = "Header.Radius.Label";
    public const string RadiusValue = "Header.Radius.Value";
    public const string Description = "Description";
    public const string AcquiredLabel = "Acquired.Label";
    public const string AcquiredValue = "Acquired.Value";
    public const string AffinityLabel = "Affinity.Label";
    public const string AffinityValue = "Affinity.Value";

    public const int ParamCount = 3;

    private static readonly string[] ParamNodes = ["14", "15", "16"];

    public static readonly TooltipMap Map = new(Addon, "2026.09.15.0000.0000", Bindings());

    public static string ParamLabel(int index) => $"Params.{index + 1}.Label";

    public static string ParamValue(int index) => $"Params.{index + 1}.Value";

    private static List<SlotBinding> Bindings()
    {
        List<SlotBinding> bindings =
        [
            new(Icon, "4/20", "Image"),
            Text(IconCooldown, "3"),
            Text(Name, "5"),
            Text(Category, "6"),
            Text(RangeLabel, "8"),
            Text(RangeValue, "9"),
            Text(RadiusLabel, "11"),
            Text(RadiusValue, "12"),
            Text(Description, "19"),
            Text(AcquiredLabel, "22"),
            Text(AcquiredValue, "26"),
            Text(AffinityLabel, "28"),
            Text(AffinityValue, "29"),
        ];

        for (var i = 0; i < ParamNodes.Length; i++)
        {
            bindings.Add(Text(ParamLabel(i), $"{ParamNodes[i]}/2"));
            bindings.Add(Text(ParamValue(i), $"{ParamNodes[i]}/3"));
        }

        return bindings;
    }

    private static SlotBinding Text(string slot, string path) => new(slot, path, "Text");
}
