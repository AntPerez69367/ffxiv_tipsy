using Tipsy.Core.Text;

namespace Tipsy.Core.Layout;

/// <summary>A made-up item shown in the preview before any real tooltip has been read, covering every kind of block.</summary>
public static class SampleTooltip
{
    public const string IconTexture = "ui/icon/037000/037810_hr1.tex";

    public static readonly IReadOnlyList<TooltipBlock> Blocks =
    [
        new HeaderBlock(IconTexture, string.Empty, SeText.Utf8("Sample Grimoire"), [SeText.Utf8("Arcanist's Grimoire"), SeText.Utf8("Item Level 660"), SeText.Utf8("Lv. 93"), SeText.Utf8("ACN SMN")], [SeText.Utf8("Unique"), SeText.Utf8("Untradable")]),
        new DividerBlock(),
        new ParamsBlock([new ParamValue("Magic Damage", "131", "(+4)"), new ParamValue("Auto-attack", "136.24", string.Empty), new ParamValue("Delay", "3.12", string.Empty)]),
        new CaptionBlock("BONUSES"),
        new StatTableBlock([new LabelledValue("Vitality", "+410"), new LabelledValue("Intelligence", "+409"), new LabelledValue("Determination", "+212"), new LabelledValue("Direct Hit Rate", "+303")]),
        new CaptionBlock("MATERIA"),
        new MateriaBlock([new LabelledValue("Savage Might Materia IX", "Determination +12"), new LabelledValue(string.Empty, string.Empty)]),
        new CaptionBlock("CRAFTING & REPAIRS"),
        new IconTextBlock(new ImagePart("ui/icon/062000/062114_hr1.tex", default), SeText.Utf8("90")),
        new BarBlock("Condition", "100%", 1f),
        new BarBlock("Spiritbond", "42%", 0.42f),
        new KeyValueBlock("Repair Level", SeText.Utf8("Alchemist Lv. 83")),
        new ParagraphBlock(SeText.Utf8("Preview of your settings."), false),
        new ParagraphBlock(SeText.Utf8("Sells for 1,329 gil"), true),
        new DividerBlock(),
        new ExtraBlock(SeText.Utf8("Lines other plugins add appear here.")),
    ];
}
