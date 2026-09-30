using CommunityToolkit.Mvvm.ComponentModel;
using TopBar.Core.Services;
using TopBar.Core.Weather;
using TopBar.Platform;

namespace TopBar.App.ViewModels;

public sealed partial class WeatherViewModel : ObservableObject
{
    private readonly IWeatherService _weather;
    private readonly ISettingsService _settings;

    [ObservableProperty] private bool _needsLocation;
    [ObservableProperty] private bool _hasData;
    [ObservableProperty] private bool _isStale;
    [ObservableProperty] private string _glyph = "";
    [ObservableProperty] private string _text = "";
    [ObservableProperty] private string _tooltip = "";

    public event Action? Updated;

    internal WeatherViewModel(IWeatherService weather, ISettingsService settings, IUiThread ui)
    {
        _weather = weather;
        _settings = settings;
        weather.Changed += (_, _) => ui.Post(Update);
        settings.Changed += (_, _) => ui.Post(Update);
        Update();
    }

    public void Update()
    {
        var w = _settings.Current.Weather;
        NeedsLocation = !w.HasLocation;

        var snapshot = w.HasLocation ? _weather.Current : null;
        if (snapshot is null)
        {
            HasData = false; IsStale = false; Glyph = ""; Text = ""; Tooltip = "";
        }
        else
        {
            var fahrenheit = w.Unit == "Fahrenheit";
            HasData = true;
            IsStale = snapshot.IsStale;
            Glyph = WeatherCodes.Get(snapshot.WeatherCode, snapshot.IsDay).Glyph;
            Text = WeatherFormatter.Temperature(snapshot.Temperature, fahrenheit);
            Tooltip = WeatherFormatter.Tooltip(snapshot, fahrenheit);
        }
        Updated?.Invoke();
    }
}
