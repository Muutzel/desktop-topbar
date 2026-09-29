using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TopBar.App.Services;
using TopBar.App.ViewModels;
using TopBar.Core.Services;

namespace TopBar.App.Controls;

/// <summary>Center section: [weather icon] 14 °C · 14:32 · Di., 29. Sep.</summary>
public sealed partial class CenterInfo : UserControl
{
    private readonly ClockViewModel _clock;
    private readonly WeatherViewModel _weather;
    private readonly ISettingsService _settings;

    public CenterInfo(ClockViewModel clock, WeatherViewModel weather, ISettingsService settings)
    {
        InitializeComponent();
        _clock = clock;
        _weather = weather;
        _settings = settings;

        ToolTipService.SetToolTip(LocationButton, Loc.Get("Weather_SetLocation"));
        Loaded += (_, _) =>
        {
            _clock.Updated += RefreshClock;
            _weather.Updated += RefreshWeather;
            RefreshClock();
            RefreshWeather();
        };
        Unloaded += (_, _) =>
        {
            _clock.Updated -= RefreshClock;
            _weather.Updated -= RefreshWeather;
        };
    }

    private void RefreshClock()
    {
        TimeText.Text = _clock.Time;
        DateText.Text = _clock.Date;
        // Fixed width for the time so the digits never make the block jump: measured with the widest digit combination.
        TimeText.MinWidth = MeasureWidth(TimeText, _settings.Current.Clock.ShowSeconds
            ? (_settings.Current.Clock.Use24h ? "88:88:88" : "88:88:88 PM")
            : (_settings.Current.Clock.Use24h ? "88:88" : "88:88 PM"));
    }

    private void RefreshWeather()
    {
        LocationButton.Visibility = _weather.NeedsLocation ? Visibility.Visible : Visibility.Collapsed;

        var show = _weather.ShowWeather && _weather.HasData;
        WeatherPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        Sep1.Visibility = _weather.ShowWeather && (_weather.HasData || _weather.NeedsLocation) ? Visibility.Visible : Visibility.Collapsed;

        WeatherIcon.Text = _weather.Glyph;
        WeatherText.Text = _weather.Text;
        WeatherPanel.Opacity = _weather.IsStale ? 0.5 : 1.0; // stale data is dimmed
        ToolTipService.SetToolTip(WeatherPanel, _weather.Tooltip);
        WeatherText.MinWidth = MeasureWidth(WeatherText, "-88 °C");
    }

    private static double MeasureWidth(TextBlock reference, string sample)
    {
        var probe = new TextBlock
        {
            Text = sample,
            FontFamily = reference.FontFamily,
            FontSize = reference.FontSize,
            FontWeight = reference.FontWeight,
        };
        probe.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        return Math.Ceiling(probe.DesiredSize.Width);
    }

    private void LocationButton_Click(object sender, RoutedEventArgs e) => AppController.OpenSettings("weather");
}
