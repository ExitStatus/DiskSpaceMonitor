using System.Windows;
using System.Windows.Controls;

namespace DiskSpaceMonitor.Views
{
    /// <summary>
    /// One row of the settings dialog: a right-aligned caption, then the control it labels, then an
    /// optional readout hard against the right edge. Every row inside a <see cref="Scope"/> shares
    /// one caption column, sized to that tab's longest caption — so the controls line up down the
    /// page without a tab reserving room for captions that live on a different one.
    /// </summary>
    internal static class SettingRow
    {
        /// <summary>Name of the shared caption column. Rows only share within their own scope, so a
        /// long caption on one tab doesn't indent the controls on another.</summary>
        internal const string LabelColumn = "SettingLabel";

        /// <summary>Space between the caption and the control it labels.</summary>
        internal const double Gap = 8;

        /// <summary>Width of the value readout to the right of a slider.</summary>
        internal const double ReadoutWidth = 44;

        /// <summary>Marks a tab's root as the scope its rows measure their caption column within.
        /// Without this the columns are sized row by row and nothing lines up.</summary>
        internal static T Scope<T>(T root) where T : FrameworkElement
        {
            Grid.SetIsSharedSizeScope(root, true);
            return root;
        }

        /// <summary>A captioned row. The readout is optional — dropdowns don't have one.</summary>
        /// <param name="tooltip">What the setting does, shown from the caption as well as the
        /// control: the caption is what you read first, so it has to be hoverable too.</param>
        internal static Grid Build(string label, FrameworkElement control,
            FrameworkElement? readout = null, double topMargin = 0, string? tooltip = null)
        {
            var grid = new Grid { Margin = new Thickness(0, topMargin, 0, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto,
                SharedSizeGroup = LabelColumn,
            });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var caption = new TextBlock
            {
                Text = label,
                FontSize = 13,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, Gap, 0),
                ToolTip = tooltip,
            };
            Grid.SetColumn(caption, 0);
            grid.Children.Add(caption);

            control.VerticalAlignment = VerticalAlignment.Center;
            control.ToolTip = tooltip;
            Grid.SetColumn(control, 1);
            grid.Children.Add(control);

            if (readout != null)
            {
                readout.ToolTip = tooltip;
                Grid.SetColumn(readout, 2);
                grid.Children.Add(readout);
            }

            return grid;
        }

        /// <summary>The live value shown to the right of a slider.</summary>
        internal static TextBlock Readout(string text) => new()
        {
            Width = ReadoutWidth,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Text = text,
        };

        /// <summary>A control that carries its own caption (a checkbox), or a note that belongs
        /// under one. It takes an empty caption, so it starts where the labelled controls do
        /// however wide this tab's caption column turns out to be.</summary>
        internal static Grid Indented(FrameworkElement control, double topMargin = 0,
            string? tooltip = null)
            => Build("", control, topMargin: topMargin, tooltip: tooltip);
    }
}
