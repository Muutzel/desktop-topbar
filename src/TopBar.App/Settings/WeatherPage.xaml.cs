using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TopBar.App.Services;
using TopBar.Core.Services;

namespace TopBar.App.SettingsPages;

public sealed partial class WeatherPage : Page
{
    private readonly PageState _s = new();
    private readonly IWeatherService _weather = AppController.Services.GetRequiredService<IWeatherService>();
    private readonly Debouncer _searchDebounce = new(400);
    private CancellationTokenSource? _searchCts;

    public WeatherPage()
    {
        InitializeComponent();
        Loaded += (_, _) => { _s.Load(LoadValues); PageState.LocalizeToggles(this); };
        Unloaded += (_, _) => { _s.Flush(); _searchCts?.Cancel(); };
    }

    private void LoadValues()
    {
        var w = _s.Settings.Current.Weather;
        ShowSwitch.IsOn = w.Show;
        UnitRadios.SelectedIndex = w.Unit == "Fahrenheit" ? 1 : 0;
        IntervalBox.Value = w.IntervalMinutes;
        ShowCurrentLocation();
    }

    private void ShowCurrentLocation()
    {
        var w = _s.Settings.Current.Weather;
        CurrentLocationText.Text = w.HasLocation && !string.IsNullOrEmpty(w.LocationName)
            ? string.Format(Loc.Get("Weather_CurrentLocation"), w.LocationName)
            : Loc.Get("Weather_NoLocation");
    }

    private void ShowSwitch_Toggled(object sender, RoutedEventArgs e) => _s.Save(x => x.Weather.Show = ShowSwitch.IsOn);

    private void UnitRadios_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UnitRadios.SelectedItem is RadioButton { Tag: string tag }) _s.Save(x => x.Weather.Unit = tag);
    }

    private void IntervalBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (double.IsNaN(args.NewValue)) return;
        var minutes = (int)Math.Clamp(args.NewValue, 5, 120);
        _s.SaveDebounced(x => x.Weather.IntervalMinutes = minutes);
    }

    // ---- location search: from 2 characters, 400 ms debounce, running requests are cancelled ----

    private void LocationBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
        var query = sender.Text.Trim();
        _searchCts?.Cancel();
        if (query.Length < 2)
        {
            sender.ItemsSource = null;
            return;
        }
        _searchDebounce.Run(async () => await SearchAsync(sender, query));
    }

    private async Task SearchAsync(AutoSuggestBox box, string query)
    {
        _searchCts?.Cancel();
        var cts = _searchCts = new CancellationTokenSource();
        try
        {
            var results = await _weather.SearchLocationAsync(query, cts.Token);
            if (!cts.IsCancellationRequested) box.ItemsSource = results;
        }
        catch (OperationCanceledException) { /* superseded by a newer query */ }
        catch (Exception ex) { AppController.LogWarning(ex, "Location search failed"); }
    }

    private void LocationBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is GeoLocation g) sender.Text = g.DisplayName;
    }

    private void LocationBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var chosen = args.ChosenSuggestion as GeoLocation
                     ?? (sender.ItemsSource as IEnumerable<GeoLocation>)?.FirstOrDefault();
        if (chosen is null) return;

        _s.Settings.Update(x =>
        {
            x.Weather.LocationName = chosen.DisplayName;
            x.Weather.Latitude = chosen.Latitude;
            x.Weather.Longitude = chosen.Longitude;
        });
        sender.Text = "";
        sender.ItemsSource = null;
        ShowCurrentLocation();
    }
}
