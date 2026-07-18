using System.Windows;

namespace DiskSpaceMonitor.Views
{
    /// <summary>
    /// Base class for standard (chromed) dialogs. Applies the WPF Fluent theme in
    /// <see cref="ThemeMode.System"/> mode, which follows the OS light/dark setting
    /// (updating live) and themes every standard control via implicit styles — so
    /// any control added to a derived dialog in future is themed automatically.
    ///
    /// The transparent, custom-drawn widget windows deliberately do NOT derive from
    /// this; only chromed dialogs should.
    /// </summary>
    public class ThemedWindow : Window
    {
        public ThemedWindow()
        {
            // ThemeMode is an experimental API (WPF0001); opting in deliberately.
#pragma warning disable WPF0001
            ThemeMode = ThemeMode.System;
#pragma warning restore WPF0001
        }
    }
}
