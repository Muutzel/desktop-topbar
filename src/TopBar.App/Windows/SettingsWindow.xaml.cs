using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TopBar.App.Services;
using TopBar.App.SettingsPages;
using TopBar.Core.Services;
using TopBar.Core.Settings;
using TopBar.Platform.AppBar;
using Windows.Graphics;

namespace TopBar.App.Views;

/// <summary>Settings window (one instance). All changes apply immediately, there is no apply button.</summary>
public sealed partial class SettingsWindow : Window
{
    private static readonly Dictionary<string, Type> Pages = new()
    {
        ["general"] = typeof(GeneralPage),
        ["layout"] = typeof(LayoutPage),
        ["desktops"] = typeof(DesktopsPage),
        ["clock"] = typeof(ClockPage),
        ["weather"] = typeof(WeatherPage),
        ["pomodoro"] = typeof(PomodoroPage),
        ["urlchecker"] = typeof(UrlCheckerPage),
        ["system"] = typeof(SystemPage),
        ["info"] = typeof(InfoPage),
    };

    private bool _positioned;

    public SettingsWindow()
    {
        InitializeComponent();
        Title = Loc.Get("Settings_Title");
        PomodoroNavIcon.Loaded += (_, _) => MakeMonochrome(PomodoroNavIcon);
        // Pages of switched-off features are not offered; follows the switches live (they sit on the "Anordnung" page).
        ApplyFeatureVisibility();
        var settings = AppController.Services.GetRequiredService<ISettingsService>();
        EventHandler<AppSettings> onChanged = (_, _) => DispatcherQueue.TryEnqueue(ApplyFeatureVisibility);
        settings.Changed += onChanged;
        Closed += (_, _) => settings.Changed -= onChanged;
        NavigateTo("general");
    }

    private void ApplyFeatureVisibility()
    {
        foreach (var item in Nav.MenuItems.OfType<NavigationViewItem>())
            item.Visibility = FeatureEnabled((string)item.Tag) ? Visibility.Visible : Visibility.Collapsed;
        if (Nav.SelectedItem is NavigationViewItem { Tag: string tag } && !FeatureEnabled(tag)) NavigateTo("layout");
    }

    /// <summary>FontIcon has no color-font switch: find the TextBlock it renders with and turn color fonts off (emoji then take the icon color).</summary>
    private static void MakeMonochrome(DependencyObject root)
    {
        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
            if (child is TextBlock t) t.IsColorFontEnabled = false;
            MakeMonochrome(child);
        }
    }

    /// <summary>Shows and activates the window; navigates to <paramref name="page"/> if given.</summary>
    public void Show(string? page)
    {
        if (!_positioned)
        {
            CenterOnCursorMonitor();
            _positioned = true;
        }
        if (page is not null) NavigateTo(page);
        AppWindow.Show();
        Activate();
    }

    private static bool FeatureEnabled(string tag) =>
        AppController.Services.GetRequiredService<ISettingsService>().Current.Features.IsPageEnabled(tag);

    private void NavigateTo(string tag)
    {
        if (!Pages.ContainsKey(tag) || !FeatureEnabled(tag)) tag = "general";
        var item = Nav.MenuItems.OfType<NavigationViewItem>().First(i => (string)i.Tag == tag);
        if (!ReferenceEquals(Nav.SelectedItem, item)) Nav.SelectedItem = item; // triggers SelectionChanged
        else if (ContentFrame.Content is null) ContentFrame.Navigate(Pages[tag]);
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem { Tag: string tag } && Pages.TryGetValue(tag, out var type))
            ContentFrame.Navigate(type);
    }

    /// <summary>900 × 640 DIP, centered on the monitor with the mouse cursor.</summary>
    private void CenterOnCursorMonitor()
    {
        var monitor = MonitorEnumerator.FromCursor();
        if (monitor is null) { AppWindow.Resize(new SizeInt32(900, 640)); return; }

        var scale = monitor.DpiScale;
        var width = (int)Math.Round(900 * scale);
        var height = (int)Math.Round(640 * scale);
        AppWindow.MoveAndResize(new RectInt32(
            monitor.Left + (monitor.Width - width) / 2,
            monitor.Top + (monitor.Height - height) / 2,
            width, height));
    }
}
