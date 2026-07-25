using System;
using System.Collections.Generic;
using DiskSpaceMonitor.Widgets.BarGraph;

namespace DiskSpaceMonitor.Widgets.VerticalBar
{
    /// <summary>Registry entry for the vertical bar graph widget: one window showing a bar per drive.</summary>
    public sealed class VerticalBarWidget : BarGraphWidget
    {
        public override string Id => "VerticalBar";

        public override string DisplayName => "Vertical bar graph";

        public override IWidgetView CreateView() => new VerticalBarView();

        protected override BarOrientation DefaultOrientation => BarOrientation.BottomUp;

        protected override (string Label, BarOrientation Value)[] Orientations => new[]
        {
            ("Bottom Up", BarOrientation.BottomUp),
            ("Top Down", BarOrientation.TopDown),
        };
    }
}
