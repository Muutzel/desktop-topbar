using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TopBar.App.Services;
using TopBar.App.ViewModels;

namespace TopBar.App.Controls;

/// <summary>
/// URL checker: [icon] ok/total. Gray globe (like the network value) while everything is reachable, red warning (and a red tint) as soon as a URL is confirmed down.
/// A small ring replaces the icon while a round runs. Click = check now (opens the settings if no URL is configured).
/// </summary>
public sealed partial class UrlCheckerControl : UserControl
{
    // Globe: same glyph as the network value (Fluent System Icons font, codepoint verified in the project: "globe").
    private const int GlyphGlobe = 62554;
    // Warning triangle with exclamation mark from Segoe Fluent Icons (codepoint from memory, not verified against the font).
    private const string GlyphProblem = "\uE7BA";

    private readonly UrlCheckerViewModel _vm;

    public UrlCheckerControl(UrlCheckerViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        Loaded += (_, _) => { _vm.Updated += Refresh; Refresh(); };
        Unloaded += (_, _) => _vm.Updated -= Refresh;
        ActualThemeChanged += (_, _) => Refresh();
    }

    private void Refresh()
    {
        var secondary = (Brush)Resources["SecondaryTextBrush"];
        var critical = (Brush)Resources["CriticalBrush"];

        // Ring while a round runs (also on the very first one); otherwise the state icon.
        var showRing = _vm.IsRunning && _vm.HasUrls;
        Ring.IsActive = showRing;
        Ring.Visibility = showRing ? Visibility.Visible : Visibility.Collapsed;
        StateIcon.Visibility = showRing ? Visibility.Collapsed : Visibility.Visible;

        if (!_vm.HasUrls || !_vm.HasResults)
        {
            // Nothing to show yet: dimmed link icon (or just the ring during the first round)
            ShowGlobe();
            StateIcon.Foreground = secondary;
            StateIcon.Opacity = 0.45;
            CountText.Text = "";
            Tint.Background = null;
        }
        else if (_vm.HasProblem)
        {
            StateIcon.FontFamily = (FontFamily)Application.Current.Resources["SegoeIconsFont"];
            StateIcon.FontSize = 14;
            StateIcon.Text = GlyphProblem;
            StateIcon.Foreground = critical;
            StateIcon.Opacity = 1.0;
            CountText.Text = $"{_vm.OkCount}/{_vm.Total}";
            CountText.Foreground = critical;
            Tint.Background = (Brush)Resources["CriticalTintBrush"];
        }
        else
        {
            ShowGlobe();
            StateIcon.Foreground = secondary;
            StateIcon.Opacity = 0.8;
            CountText.Text = $"{_vm.OkCount}/{_vm.Total}";
            CountText.Foreground = secondary;
            Tint.Background = null;
        }

        // Fixed width for up to two-digit counts: no jumping when the numbers change.
        CountText.MinWidth = _vm.HasResults ? TextMeasure.Width(CountText, "88/88") : 0;
        ToolTipService.SetToolTip(MainButton, _vm.Tooltip);
    }

    private void ShowGlobe()
    {
        StateIcon.FontFamily = (FontFamily)Application.Current.Resources["FluentIconsFont"];
        StateIcon.FontSize = 16;
        StateIcon.Text = char.ConvertFromUtf32(GlyphGlobe);
    }

    private void MainButton_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.HasUrls) _vm.CheckNow();
        else AppController.OpenSettings("urlchecker");
    }
}
