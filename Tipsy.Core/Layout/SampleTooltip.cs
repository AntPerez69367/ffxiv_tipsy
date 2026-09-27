using System.Text;

namespace Tipsy.Core.Layout;

/// <summary>A made-up item shown in the preview before any real tooltip has been read, covering every kind of block.</summary>
public static class SampleTooltip
{
    public const string IconTexture = "ui/icon/037000/037810_hr1.tex";

    public static readonly IReadOnlyList<TooltipBlock> Blocks =
    [
        new HeaderBlock(IconTexture, string.Empty, Utf8("Sample Grimoire"), [Utf8("Arcanist's Grimoire"), Utf8("Item Level 660"), Utf8("Lv. 93"), Utf8("ACN SMN")], [Utf8("Unique"), Utf8("Untradable")]),
        new DividerBlock(),
        new ParamsBlock([new ParamValue("Magic Damage", "131", "(+4)"), new ParamValue("Auto-attack", "136.24", string.Empty), new ParamValue("Delay", "3.12", string.Empty)]),
        new CaptionBlock("BONUSES"),
        new StatTableBlock([new LabelledValue("Vitality", "+410"), new LabelledValue("Intelligence", "+409"), new LabelledValue("Determination", "+212"), new LabelledValue("Direct Hit Rate", "+303")]),
        new CaptionBlock("MATERIA"),
        new MateriaBlock([new LabelledValue("Savage Might Materia IX", "Determination +12"), new LabelledValue(string.Empty, string.Empty)]),
        new CaptionBlock("CRAFTING & REPAIRS"),
        new BarBlock("Condition", "100%", 1f),
        new BarBlock("Spiritbond", "42%", 0.42f),
        new KeyValueBlock("Repair Level", Utf8("Alchemist Lv. 83")),
        new ParagraphBlock(Utf8("A sample item, shown until you hover a real one."), false),
        new ParagraphBlock(Utf8("Sells for 1,329 gil"), true),
        new DividerBlock(),
        new CaptionBlock(ItemTooltipLayout.ExtrasCaption),
        new ParagraphBlock(Utf8("Lines other plugins add appear here."), false),
    ];

    private static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);
}
