using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TopBar.App.Services;
using TopBar.App.SettingsPages;
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
        ["system"] = typeof(SystemPage),
        ["info"] = typeof(InfoPage),
    };

    private bool _positioned;

    public SettingsWindow()
    {
        InitializeComponent();
        Title = Loc.Get("Settings_Title");
        NavigateTo("general");
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

    private void NavigateTo(string tag)
    {
        if (!Pages.ContainsKey(tag)) tag = "general";
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
