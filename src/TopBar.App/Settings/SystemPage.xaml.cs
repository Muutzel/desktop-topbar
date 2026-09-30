using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TopBar.App.Services;
using TopBar.Core.Services;

namespace TopBar.App.SettingsPages;

public sealed partial class SystemPage : Page
{
    private readonly PageState _s = new();

    public SystemPage()
    {
        InitializeComponent();

        WarnRow.Header = Loc.Get("System_Warn"); WarnRow.Minimum = 50; WarnRow.Maximum = 100; WarnRow.Step = 1; WarnRow.Suffix = " %";
        CriticalRow.Header = Loc.Get("System_Critical"); CriticalRow.Minimum = 50; CriticalRow.Maximum = 100; CriticalRow.Step = 1; CriticalRow.Suffix = " %";
        WarnRow.ValueChanged += v =>
        {
            _s.SaveDebounced(x => x.Stats.WarnThreshold = (int)Math.Round(v));
            if (!_s.Loading && CriticalRow.Value < v) CriticalRow.Value = v; // critical must stay >= warn
        };
        CriticalRow.ValueChanged += v =>
        {
            _s.SaveDebounced(x => x.Stats.CriticalThreshold = (int)Math.Round(v));
            if (!_s.Loading && WarnRow.Value > v) WarnRow.Value = v;
        };

        Loaded += (_, _) => { _s.Load(LoadValues); PageState.LocalizeToggles(this); };
        Unloaded += (_, _) => _s.Flush();
    }

    private void LoadValues()
    {
        var c = _s.Settings.Current.Stats;
        LabelsSwitch.IsOn = c.ShowLabels;

        IntervalCombo.SelectedIndex = c.IntervalMs switch { <= 500 => 0, <= 1000 => 1, <= 2000 => 2, _ => 3 };
        WarnRow.Value = c.WarnThreshold;
        CriticalRow.Value = c.CriticalThreshold;

        SelectByTag(NetUnitCombo, c.NetworkUnit);
        SelectByTag(NetScaleCombo, c.NetworkScale);

        NicCombo.Items.Clear();
        NicCombo.Items.Add(new ComboBoxItem { Content = Loc.Get("System_Nic_Auto"), Tag = "" });
        var stats = AppController.Services.GetRequiredService<ISystemStatsService>();
        foreach (var (id, name) in stats.NetworkInterfaces)
            NicCombo.Items.Add(new ComboBoxItem { Content = name, Tag = id });

        NicCombo.SelectedIndex = 0;
        if (c.NetworkInterfaceId is { } saved)
        {
            var match = NicCombo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == saved);
            if (match is null)
            {
                match = new ComboBoxItem { Content = $"{saved} ({Loc.Get("System_Nic_Missing")})", Tag = saved };
                NicCombo.Items.Add(match);
            }
            NicCombo.SelectedItem = match;
        }
    }

    private void LabelsSwitch_Toggled(object sender, RoutedEventArgs e) => _s.Save(x => x.Stats.ShowLabels = LabelsSwitch.IsOn);

    private void IntervalCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IntervalCombo.SelectedItem is ComboBoxItem { Tag: string tag } && int.TryParse(tag, out var ms))
            _s.Save(x => x.Stats.IntervalMs = ms);
    }

    private static void SelectByTag(ComboBox combo, string tag) =>
        combo.SelectedItem = combo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == tag) ?? combo.Items[0];

    private void NetUnitCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NetUnitCombo.SelectedItem is ComboBoxItem { Tag: string tag })
            _s.Save(x => x.Stats.NetworkUnit = tag);
    }

    private void NetScaleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NetScaleCombo.SelectedItem is ComboBoxItem { Tag: string tag })
            _s.Save(x => x.Stats.NetworkScale = tag);
    }

    private void NicCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NicCombo.SelectedItem is ComboBoxItem { Tag: string tag })
            _s.Save(x => x.Stats.NetworkInterfaceId = tag.Length == 0 ? null : tag);
    }
}
