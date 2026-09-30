using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TopBar.App.Services;
using TopBar.App.ViewModels;

namespace TopBar.App.Controls;

/// <summary>Weather element: [icon] 14 °C. Shows a globe button instead while no location is configured.</summary>
public sealed partial class WeatherControl : UserControl
{
    private readonly WeatherViewModel _weather;

    public WeatherControl(WeatherViewModel weather)
    {
        InitializeComponent();
        _weather = weather;

        ToolTipService.SetToolTip(LocationButton, Loc.Get("Weather_SetLocation"));
        Loaded += (_, _) => { _weather.Updated += Refresh; Refresh(); };
        Unloaded += (_, _) => _weather.Updated -= Refresh;
    }

    private void Refresh()
    {
        LocationButton.Visibility = _weather.NeedsLocation ? Visibility.Visible : Visibility.Collapsed;

        var show = _weather.ShowWeather && _weather.HasData;
        WeatherPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        WeatherIcon.Text = _weather.Glyph;
        WeatherText.Text = _weather.Text;
        WeatherPanel.Opacity = _weather.IsStale ? 0.5 : 1.0; // stale data is dimmed
        ToolTipService.SetToolTip(WeatherPanel, _weather.Tooltip);
        WeatherText.MinWidth = TextMeasure.Width(WeatherText, "-88 °C");
    }

    private void LocationButton_Click(object sender, RoutedEventArgs e) => AppController.OpenSettings("weather");
}
