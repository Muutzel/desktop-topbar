using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TopBar.App.Services;
using TopBar.Core.Services;

namespace TopBar.App.SettingsPages;

public sealed partial class DesktopsPage : Page
{
    private readonly PageState _s = new();

    public DesktopsPage()
    {
        InitializeComponent();
        DotSizeRow.Header = Loc.Get("Desktops_DotSize"); DotSizeRow.Minimum = 6; DotSizeRow.Maximum = 14; DotSizeRow.Step = 1; DotSizeRow.Suffix = " px";
        DotSizeRow.ValueChanged += v => _s.SaveDebounced(x => x.Desktops.DotSize = (int)Math.Round(v));

        Loaded += (_, _) => { _s.Load(LoadValues); PageState.LocalizeToggles(this); };
        Unloaded += (_, _) => _s.Flush();
    }

    private void LoadValues()
    {
        var d = _s.Settings.Current.Desktops;
        ScrollSwitch.IsOn = d.ScrollSwitch;
        DotSizeRow.Value = d.DotSize;
        UnavailableInfo.IsOpen = !AppController.Services.GetRequiredService<IVirtualDesktopService>().IsAvailable;
    }

    private void ScrollSwitch_Toggled(object sender, RoutedEventArgs e) => _s.Save(x => x.Desktops.ScrollSwitch = ScrollSwitch.IsOn);
}
