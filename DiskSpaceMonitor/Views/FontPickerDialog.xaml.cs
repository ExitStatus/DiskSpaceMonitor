using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace DiskSpaceMonitor.Views
{
    /// <summary>
    /// Picks one installed font family. Each family in the list is drawn in its own face, so the
    /// list doubles as the preview, with a larger sample underneath showing the kind of text the
    /// widgets actually draw. Raises <see cref="LivePreview"/> as the selection moves so the widget
    /// behind the dialog updates; the caller reverts on Cancel.
    /// </summary>
    public partial class FontPickerDialog : ThemedWindow
    {
        /// <summary>One installed family: its display name and the family to render it with.</summary>
        public sealed record FontChoice(string Name, FontFamily Family);

        private readonly List<FontChoice> _all;
        private bool _ready;

        /// <summary>Raised with the family name as the selection moves.</summary>
        public event Action<string>? LivePreview;

        /// <summary>The family currently selected.</summary>
        public string SelectedFamily { get; private set; }

        public FontPickerDialog(string initialFamily)
        {
            InitializeComponent();

            SelectedFamily = initialFamily;
            _all = InstalledFamilies();

            // A saved font that is no longer installed still belongs in the list, or the dialog
            // would silently reassign it the moment it opens.
            if (!_all.Any(f => NameMatches(f.Name, initialFamily)))
                _all.Insert(0, new FontChoice(initialFamily, new FontFamily(initialFamily)));

            ShowFamilies(_all);
            Select(initialFamily);
            UpdateSample();
            _ready = true;
        }

        /// <summary>Installed families under the current UI culture's names, sorted, de-duplicated.</summary>
        private static List<FontChoice> InstalledFamilies()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var choices = new List<FontChoice>();

            foreach (var family in Fonts.SystemFontFamilies)
            {
                string name = DisplayName(family);
                if (!string.IsNullOrWhiteSpace(name) && seen.Add(name))
                    choices.Add(new FontChoice(name, family));
            }

            return choices.OrderBy(f => f.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        }

        // FontFamily.Source is the invariant name; the typeface's own names are localised. Prefer
        // the current culture's name, then English, then whatever Source says.
        private static string DisplayName(FontFamily family)
        {
            var names = family.FamilyNames;
            if (names.TryGetValue(XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.IetfLanguageTag), out var local))
                return local;
            if (names.TryGetValue(XmlLanguage.GetLanguage("en-us"), out var english))
                return english;
            return family.Source;
        }

        private static bool NameMatches(string a, string b)
            => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private void ShowFamilies(IEnumerable<FontChoice> families)
        {
            FamilyList.ItemsSource = families.ToList();
        }

        private void Select(string familyName)
        {
            foreach (var choice in FamilyList.Items.OfType<FontChoice>())
            {
                if (NameMatches(choice.Name, familyName))
                {
                    FamilyList.SelectedItem = choice;
                    FamilyList.ScrollIntoView(choice);
                    return;
                }
            }
        }

        private void OnFilterChanged(object sender, TextChangedEventArgs e)
        {
            if (!_ready)
                return;

            string filter = FilterBox.Text.Trim();
            ShowFamilies(filter.Length == 0
                ? _all
                : _all.Where(f => f.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase)));

            // Keep the chosen font selected while filtering; the user hasn't changed it by typing.
            Select(SelectedFamily);
        }

        private void OnFamilySelected(object sender, SelectionChangedEventArgs e)
        {
            if (FamilyList.SelectedItem is not FontChoice choice)
                return;

            SelectedFamily = choice.Name;
            UpdateSample();

            if (_ready)
                LivePreview?.Invoke(SelectedFamily);
        }

        private void UpdateSample()
        {
            Sample.FontFamily = new FontFamily(SelectedFamily);
            OkButton.IsEnabled = true;
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e) => Close();
    }
}
