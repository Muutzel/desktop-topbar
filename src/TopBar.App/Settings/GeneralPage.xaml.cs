using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TopBar.App.Services;

namespace TopBar.App.SettingsPages;

public sealed partial class GeneralPage : Page
{
    private readonly PageState _s = new();

    public GeneralPage()
    {
        InitializeComponent();

        BarHeightRow.Header = Loc.Get("General_BarHeight"); BarHeightRow.Minimum = 24; BarHeightRow.Maximum = 56; BarHeightRow.Step = 2; BarHeightRow.Suffix = " px";
        FontSizeRow.Header = Loc.Get("General_FontSize"); FontSizeRow.Minimum = 11; FontSizeRow.Maximum = 18; FontSizeRow.Step = 1; FontSizeRow.Suffix = " pt";
        OpacityRow.Header = Loc.Get("General_Opacity"); OpacityRow.Minimum = 0; OpacityRow.Maximum = 1; OpacityRow.Step = 0.05; OpacityRow.DisplayScale = 100; OpacityRow.Suffix = " %";

        BarHeightRow.ValueChanged += v => _s.SaveDebounced(x => x.General.BarHeight = (int)Math.Round(v));
        FontSizeRow.ValueChanged += v => _s.SaveDebounced(x => x.General.FontSize = (int)Math.Round(v));
        OpacityRow.ValueChanged += v => _s.SaveDebounced(x => x.General.BackgroundOpacity = Math.Round(v, 2));

        Loaded += (_, _) => { _s.Load(LoadValues); PageState.LocalizeToggles(this); };
        Unloaded += (_, _) => _s.Flush();
    }

    private void LoadValues()
    {
        var g = _s.Settings.Current.General;
        AutostartSwitch.IsOn = g.Autostart;
        ThemeRadios.SelectedIndex = g.Theme switch { "Light" => 1, "Dark" => 2, _ => 0 };
        MonitorRadios.SelectedIndex = g.Monitors == "All" ? 1 : 0;
        BarHeightRow.Value = g.BarHeight;
        FontSizeRow.Value = g.FontSize;
        OpacityRow.Value = g.BackgroundOpacity;
    }

    private void AutostartSwitch_Toggled(object sender, RoutedEventArgs e) => _s.Save(x => x.General.Autostart = AutostartSwitch.IsOn);

    private void ThemeRadios_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeRadios.SelectedItem is RadioButton { Tag: string tag }) _s.Save(x => x.General.Theme = tag);
    }

    private void MonitorRadios_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MonitorRadios.SelectedItem is RadioButton { Tag: string tag }) _s.Save(x => x.General.Monitors = tag);
    }
}
