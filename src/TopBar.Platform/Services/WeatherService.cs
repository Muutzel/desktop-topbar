using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using TopBar.Core.Services;
using TopBar.Core.Settings;

namespace TopBar.Platform.Services;

/// <summary>Open-Meteo client (no API key). Keeps the last good value on errors and marks it stale after 2 × interval.</summary>
public sealed class WeatherService : LoopService, IWeatherService
{
    private static readonly HttpClient Http = CreateClient();

    private readonly ISettingsService _settings;
    private readonly WakeableDelay _delay = new();
    private WeatherSnapshot? _last;
    private (double? Lat, double? Lon, string Unit) _lastKey;

    public WeatherService(ISettingsService settings, ILogger<WeatherService> log) : base(log) => _settings = settings;

    protected override string ModuleName => "Weather";

    public event EventHandler? Changed;

    public WeatherSnapshot? Current
    {
        get
        {
            var s = _last;
            if (s is null) return null;
            var interval = TimeSpan.FromMinutes(_settings.Current.Weather.IntervalMinutes);
            var stale = DateTimeOffset.Now - s.FetchedAt > interval * 2;
            return s with { IsStale = stale };
        }
    }

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("TopBar/1.0");
        return c;
    }

    protected override void OnStarting()
    {
        var w = _settings.Current.Weather;
        _lastKey = (w.Latitude, w.Longitude, w.Unit);
        _settings.Changed += OnSettingsChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    protected override void OnStopped()
    {
        _settings.Changed -= OnSettingsChanged;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
    }

    private void OnSettingsChanged(object? sender, AppSettings e)
    {
        var w = e.Weather;
        var key = (w.Latitude, w.Longitude, w.Unit);
        if (key != _lastKey)
        {
            _lastKey = key;
            _last = null; // do not show the old city's temperature
            Changed?.Invoke(this, EventArgs.Empty);
        }
        _delay.Wake();
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume) _delay.Wake();
    }

    protected override async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var w = _settings.Current.Weather;
            if (!_settings.Current.Layout.IsPlaced(BarElementIds.Weather) || !w.HasLocation)
            {
                await _delay.DelayAsync(TimeSpan.FromHours(1), ct).ConfigureAwait(false);
                continue;
            }

            var ok = await TryFetchAsync(ct).ConfigureAwait(false);
            var wait = ok ? TimeSpan.FromMinutes(_settings.Current.Weather.IntervalMinutes) : TimeSpan.FromMinutes(1);
            await _delay.DelayAsync(wait, ct).ConfigureAwait(false);
        }
    }

    public async Task RefreshAsync(CancellationToken ct)
    {
        if (_settings.Current.Weather.HasLocation)
            await TryFetchAsync(ct).ConfigureAwait(false);
    }

    private async Task<bool> TryFetchAsync(CancellationToken ct)
    {
        var w = _settings.Current.Weather;
        try
        {
            var unit = w.Unit == "Fahrenheit" ? "fahrenheit" : "celsius";
            var inv = CultureInfo.InvariantCulture;
            var url = "https://api.open-meteo.com/v1/forecast?latitude=" + w.Latitude!.Value.ToString(inv) +
                      "&longitude=" + w.Longitude!.Value.ToString(inv) +
                      "&current=temperature_2m,apparent_temperature,weather_code,is_day,wind_speed_10m" +
                      "&daily=temperature_2m_max,temperature_2m_min,precipitation_probability_max" +
                      "&timezone=auto&forecast_days=1&temperature_unit=" + unit + "&wind_speed_unit=kmh";

            using var response = await Http.GetAsync(url, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

            var snapshot = Parse(doc.RootElement);
            _last = snapshot;
            ClearLogged("fetch");
            Changed?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogOnce("fetch", ex, "Fetching weather failed, keeping last value");
            Changed?.Invoke(this, EventArgs.Empty); // lets the UI re-evaluate IsStale
            return false;
        }
    }

    internal static WeatherSnapshot Parse(JsonElement root)
    {
        var cur = root.GetProperty("current");
        var daily = root.GetProperty("daily");
        return new WeatherSnapshot(
            Temperature: cur.GetProperty("temperature_2m").GetDouble(),
            ApparentTemperature: cur.GetProperty("apparent_temperature").GetDouble(),
            WeatherCode: cur.GetProperty("weather_code").GetInt32(),
            IsDay: cur.GetProperty("is_day").GetInt32() != 0,
            WindSpeed: cur.GetProperty("wind_speed_10m").GetDouble(),
            TempMax: First(daily, "temperature_2m_max"),
            TempMin: First(daily, "temperature_2m_min"),
            PrecipitationProbability: (int)Math.Round(First(daily, "precipitation_probability_max")),
            FetchedAt: DateTimeOffset.Now,
            IsStale: false);
    }

    private static double First(JsonElement daily, string name)
    {
        if (!daily.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0) return 0;
        var v = arr[0];
        return v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;
    }

    public async Task<IReadOnlyList<GeoLocation>> SearchLocationAsync(string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2) return Array.Empty<GeoLocation>();
        try
        {
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query.Trim())}&count=5&language=de&format=json";
            using var response = await Http.GetAsync(url, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

            var list = new List<GeoLocation>();
            if (doc.RootElement.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
            {
                foreach (var r in results.EnumerateArray())
                {
                    list.Add(new GeoLocation(
                        r.GetProperty("name").GetString() ?? "",
                        r.TryGetProperty("admin1", out var a) ? a.GetString() : null,
                        r.TryGetProperty("country", out var c) ? c.GetString() ?? "" : "",
                        r.GetProperty("latitude").GetDouble(),
                        r.GetProperty("longitude").GetDouble()));
                }
            }
            return list;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.LogWarning(ex, "Weather: location search failed");
            return Array.Empty<GeoLocation>();
        }
    }
}
