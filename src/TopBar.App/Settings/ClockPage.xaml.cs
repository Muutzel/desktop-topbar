using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TopBar.App.Services;
using TopBar.Core.Formatting;

namespace TopBar.App.SettingsPages;

public sealed partial class ClockPage : Page
{
    private readonly PageState _s = new();

    public ClockPage()
    {
        InitializeComponent();
        foreach (var preset in DateFormats.Presets) FormatCombo.Items.Add(new ComboBoxItem { Content = preset, Tag = preset });
        FormatCombo.Items.Add(new ComboBoxItem { Content = Loc.Get("Clock_Custom"), Tag = "" }); // empty tag = custom

        Loaded += (_, _) => { _s.Load(LoadValues); PageState.LocalizeToggles(this); };
        Unloaded += (_, _) => _s.Flush();
    }

    private void LoadValues()
    {
        var c = _s.Settings.Current.Clock;
        Use24hSwitch.IsOn = c.Use24h;
        SecondsSwitch.IsOn = c.ShowSeconds;

        var presetIndex = DateFormats.Presets.ToList().IndexOf(c.DateFormat);
        if (presetIndex >= 0)
        {
            FormatCombo.SelectedIndex = presetIndex;
            CustomBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            FormatCombo.SelectedIndex = DateFormats.Presets.Count;
            CustomBox.Visibility = Visibility.Visible;
            CustomBox.Text = c.DateFormat;
        }
        UpdatePreview(c.DateFormat);
    }

    private void Use24hSwitch_Toggled(object sender, RoutedEventArgs e) => _s.Save(x => x.Clock.Use24h = Use24hSwitch.IsOn);
    private void SecondsSwitch_Toggled(object sender, RoutedEventArgs e) => _s.Save(x => x.Clock.ShowSeconds = SecondsSwitch.IsOn);

    private void FormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FormatCombo.SelectedItem is not ComboBoxItem { Tag: string tag }) return;
        var custom = tag.Length == 0;
        CustomBox.Visibility = custom ? Visibility.Visible : Visibility.Collapsed;
        if (custom)
        {
            UpdatePreview(CustomBox.Text);
            return;
        }
        UpdatePreview(tag);
        _s.Save(x => x.Clock.DateFormat = tag);
    }

    private void CustomBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var text = CustomBox.Text;
        if (!UpdatePreview(text)) return; // invalid: preview shows the error, value is not adopted
        _s.SaveDebounced(x => x.Clock.DateFormat = text);
    }

    private bool UpdatePreview(string format)
    {
        if (DateFormats.TryFormat(format, DateTime.Now, CultureInfo.CurrentCulture, out var result))
        {
            PreviewText.Text = result;
            return true;
        }
        PreviewText.Text = Loc.Get("Clock_InvalidFormat");
        return false;
    }
}
