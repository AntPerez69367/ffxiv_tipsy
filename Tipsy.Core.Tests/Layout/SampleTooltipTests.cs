using Tipsy.Core.Layout;
using Xunit;

namespace Tipsy.Core.Tests.Layout;

public class SampleTooltipTests
{
    [Fact]
    public void SampleCoversEveryBlockTheLayoutProducesForItems()
    {
        var kinds = SampleTooltip.Blocks.Select(block => block.GetType()).ToHashSet();

        Assert.Superset(
            new HashSet<Type> { typeof(HeaderBlock), typeof(DividerBlock), typeof(ParamsBlock), typeof(CaptionBlock), typeof(StatTableBlock), typeof(MateriaBlock), typeof(BarBlock), typeof(KeyValueBlock), typeof(ParagraphBlock) },
            kinds);
    }
}
