using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using TopBar.App.Services;
using TopBar.Core.Settings;
using Windows.System;

namespace TopBar.App.SettingsPages;

/// <summary>URL list, check interval and timeout. Only reachable while <c>Features.UrlChecker</c> is on. Every change is saved immediately.</summary>
public sealed partial class UrlCheckerPage : Page
{
    private readonly PageState _s = new();

    public UrlCheckerPage()
    {
        InitializeComponent();
        Loaded += (_, _) => _s.Load(LoadValues);
        Unloaded += (_, _) => _s.Flush();
    }

    private void LoadValues()
    {
        var c = _s.Settings.Current.UrlChecker;
        IntervalBox.Value = c.IntervalMinutes;
        TimeoutBox.Value = c.TimeoutSeconds;
        RebuildList();
    }

    private void RebuildList()
    {
        var urls = _s.Settings.Current.UrlChecker.Urls;
        UrlList.Children.Clear();
        foreach (var url in urls.ToList()) UrlList.Children.Add(BuildRow(url));
        EmptyText.Visibility = urls.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        AddButton.IsEnabled = urls.Count < UrlCheckerSettings.MaxUrls;
    }

    private UIElement BuildRow(string url)
    {
        var text = new TextBlock
        {
            Text = url,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        ToolTipService.SetToolTip(text, url);

        var remove = new Button
        {
            Content = new FontIcon
            {
                FontFamily = (FontFamily)Application.Current.Resources["SegoeIconsFont"],
                Glyph = "", // Delete
                FontSize = 12,
            },
            Padding = new Thickness(8, 4, 8, 4),
            Background = null,
            BorderThickness = new Thickness(0),
        };
        ToolTipService.SetToolTip(remove, Loc.Get("UrlChecker_Remove"));
        remove.Click += (_, _) => Remove(url);

        var row = new Grid { ColumnSpacing = 8 };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(remove, 1);
        row.Children.Add(text);
        row.Children.Add(remove);
        return row;
    }

    private void Remove(string url)
    {
        _s.Save(x => x.UrlChecker.Urls.RemoveAll(u => string.Equals(u, url, StringComparison.OrdinalIgnoreCase)));
        RebuildList();
    }

    private void Add()
    {
        var input = UrlBox.Text;
        if (string.IsNullOrWhiteSpace(input)) return;

        if (!UrlCheckerSettings.TryNormalizeUrl(input, out var url))
        {
            ErrorText.Visibility = Visibility.Visible;
            return;
        }
        ErrorText.Visibility = Visibility.Collapsed;

        _s.Save(x =>
        {
            if (!x.UrlChecker.Urls.Contains(url, StringComparer.OrdinalIgnoreCase)) x.UrlChecker.Urls.Add(url);
        });
        UrlBox.Text = "";
        RebuildList();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e) => Add();

    private void UrlBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter) { Add(); e.Handled = true; }
        else ErrorText.Visibility = Visibility.Collapsed;
    }

    // NumberBox raises ValueChanged on commit (Enter, focus loss, spinner), not per keystroke: saving immediately is fine.
    private void IntervalBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) =>
        Save(args, 1, 120, (x, v) => x.UrlChecker.IntervalMinutes = v);

    private void TimeoutBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) =>
        Save(args, 2, 60, (x, v) => x.UrlChecker.TimeoutSeconds = v);

    private void Save(NumberBoxValueChangedEventArgs args, int min, int max, Action<AppSettings, int> apply)
    {
        if (double.IsNaN(args.NewValue)) return;
        var value = (int)Math.Clamp(Math.Round(args.NewValue), min, max);
        _s.Save(x => apply(x, value));
    }
}
