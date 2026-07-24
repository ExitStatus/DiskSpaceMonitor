using DiskSpaceMonitor.Widgets.BarGraph;

namespace DiskSpaceMonitor.Widgets.HorizontalBar
{
    /// <summary>Registry entry for the horizontal bar graph widget: one window showing a bar per drive.</summary>
    public sealed class HorizontalBarWidget : BarGraphWidget
    {
        public override string Id => "HorizontalBar";

        public override string DisplayName => "Horizontal bar graph";

        public override IWidgetView CreateView() => new HorizontalBarView();

        protected override BarOrientation DefaultOrientation => BarOrientation.LeftToRight;

        protected override (string Label, BarOrientation Value)[] Orientations => new[]
        {
            ("Left to Right", BarOrientation.LeftToRight),
            ("Right to Left", BarOrientation.RightToLeft),
        };

        protected override string BarSizeLabel => "Bar thickness";
    }
}
