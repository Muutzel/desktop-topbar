using System.Text.Json;
using System.Text.Json.Serialization;
using TopBar.Core.Formatting;

namespace TopBar.Core.Settings;

public sealed class GeneralSettings
{
    public bool Autostart { get; set; } = false;
    public string Theme { get; set; } = "System";       // System | Light | Dark
    public string Monitors { get; set; } = "Primary";   // Primary | All
    public int BarHeight { get; set; } = 32;
    public int FontSize { get; set; } = 13;
    public double BackgroundOpacity { get; set; } = 0.6;
}

public sealed class DesktopSettings
{
    public int DotSize { get; set; } = 8;
    public bool ScrollSwitch { get; set; } = true;
}

public sealed class ClockSettings
{
    public bool Use24h { get; set; } = true;
    public bool ShowSeconds { get; set; } = false;
    public string DateFormat { get; set; } = DateFormats.Default;
}

public sealed class WeatherSettings
{
    public string? LocationName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Unit { get; set; } = "Celsius";       // Celsius | Fahrenheit
    public int IntervalMinutes { get; set; } = 15;

    public bool HasLocation => Latitude.HasValue && Longitude.HasValue;
}

public sealed class StatsSettings
{
    public bool ShowLabels { get; set; } = true;
    public int IntervalMs { get; set; } = 1000;
    public int WarnThreshold { get; set; } = 85;
    public int CriticalThreshold { get; set; } = 95;
    public string? NetworkInterfaceId { get; set; }
    public string NetworkUnit { get; set; } = "Bytes";  // Bytes (KB/s) | Bits (kbit/s)
    public string NetworkScale { get; set; } = "Auto"; // Auto | Kilo | Mega
}

public sealed class PomodoroSettings
{
    public int WorkMinutes { get; set; } = 25;
    public int ShortBreakMinutes { get; set; } = 5;
    /// <summary>Number of work units until the long break.</summary>
    public int Sessions { get; set; } = 4;
    public int LongBreakMinutes { get; set; } = 15;
}

/// <summary>
/// Switches (edited by hand in settings.json) that decide which bar elements and settings pages exist at all.
/// A switched-off element is neither offered in "Anordnung" nor shown on the bar, and its settings page disappears.
/// Everything is on by default except the URL checker.
/// </summary>
public sealed class FeatureSettings
{
    // Alphabetical on purpose: settings.json lists the switches in declaration order.
    public bool Clock { get; set; } = true;
    public bool Cpu { get; set; } = true;
    public bool Date { get; set; } = true;
    public bool Desktops { get; set; } = true;
    public bool Gpu { get; set; } = true;
    public bool Network { get; set; } = true;
    public bool Pomodoro { get; set; } = true;
    public bool Ram { get; set; } = true;
    /// <summary>The gear button. Without it the settings can still be opened by starting TopBar a second time.</summary>
    public bool SettingsButton { get; set; } = true;
    public bool UrlChecker { get; set; } = false;
    public bool Weather { get; set; } = true;

    /// <summary>Whether the bar element with this id (see <see cref="BarElementIds"/>) is switched on. Unknown ids count as on.</summary>
    public bool IsElementEnabled(string id) => id switch
    {
        BarElementIds.Desktops => Desktops,
        BarElementIds.Pomodoro => Pomodoro,
        BarElementIds.Weather => Weather,
        BarElementIds.Clock => Clock,
        BarElementIds.Date => Date,
        BarElementIds.Cpu => Cpu,
        BarElementIds.Gpu => Gpu,
        BarElementIds.Ram => Ram,
        BarElementIds.Network => Network,
        BarElementIds.SettingsButton => SettingsButton,
        BarElementIds.UrlChecker => UrlChecker,
        _ => true,
    };

    /// <summary>Switches the element with this id on or off. Unknown ids are ignored.</summary>
    public void SetElementEnabled(string id, bool enabled)
    {
        switch (id)
        {
            case BarElementIds.Desktops: Desktops = enabled; break;
            case BarElementIds.Pomodoro: Pomodoro = enabled; break;
            case BarElementIds.Weather: Weather = enabled; break;
            case BarElementIds.Clock: Clock = enabled; break;
            case BarElementIds.Date: Date = enabled; break;
            case BarElementIds.Cpu: Cpu = enabled; break;
            case BarElementIds.Gpu: Gpu = enabled; break;
            case BarElementIds.Ram: Ram = enabled; break;
            case BarElementIds.Network: Network = enabled; break;
            case BarElementIds.SettingsButton: SettingsButton = enabled; break;
            case BarElementIds.UrlChecker: UrlChecker = enabled; break;
        }
    }

    /// <summary>Whether the settings page with this tag exists. "general", "layout" and "info" always do; the others only while at least one of their elements is on.</summary>
    public bool IsPageEnabled(string tag) => tag switch
    {
        "desktops" => Desktops,
        "clock" => Clock || Date,
        "weather" => Weather,
        "pomodoro" => Pomodoro,
        "system" => Cpu || Gpu || Ram || Network,
        "urlchecker" => UrlChecker,
        _ => true,
    };
}

public sealed class UrlCheckerSettings
{
    public const int MaxUrls = 50;

    public List<string> Urls { get; set; } = ["https://google.com"];
    public int IntervalMinutes { get; set; } = 5;
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Trims <paramref name="input"/>, adds "https://" if no scheme is given and accepts only absolute http/https URLs with a host.
    /// </summary>
    public static bool TryNormalizeUrl(string? input, out string url)
    {
        url = "";
        var text = input?.Trim();
        if (string.IsNullOrEmpty(text)) return false;
        if (!text.Contains("://", StringComparison.Ordinal)) text = "https://" + text;
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
        if (string.IsNullOrEmpty(uri.Host)) return false;
        url = text;
        return true;
    }

    /// <summary>Drops invalid and duplicate URLs (case-insensitive, first wins), keeps at most <see cref="MaxUrls"/>, clamps the numbers.</summary>
    public void Normalize()
    {
        var cleaned = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in Urls ?? new List<string>())
        {
            if (!TryNormalizeUrl(raw, out var url) || !seen.Add(url)) continue;
            cleaned.Add(url);
            if (cleaned.Count >= MaxUrls) break;
        }
        Urls = cleaned;
        IntervalMinutes = Math.Clamp(IntervalMinutes, 1, 120);
        TimeoutSeconds = Math.Clamp(TimeoutSeconds, 2, 60);
    }
}

public sealed class AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public GeneralSettings General { get; set; } = new();
    public DesktopSettings Desktops { get; set; } = new();
    public ClockSettings Clock { get; set; } = new();
    public WeatherSettings Weather { get; set; } = new();
    public StatsSettings Stats { get; set; } = new();
    public PomodoroSettings Pomodoro { get; set; } = new();
    public LayoutSettings Layout { get; set; } = new();
    public FeatureSettings Features { get; set; } = new();
    public UrlCheckerSettings UrlChecker { get; set; } = new();

    /// <summary>Replaces null sections, invalid enum strings and clamps all numeric values to their valid range.</summary>
    public void Normalize()
    {
        SchemaVersion = CurrentSchemaVersion;
        General ??= new();
        Desktops ??= new();
        Clock ??= new();
        Weather ??= new();
        Stats ??= new();
        Pomodoro ??= new();
        Layout ??= new();
        Features ??= new();
        UrlChecker ??= new();

        General.Theme = OneOf(General.Theme, "System", "Light", "Dark");
        General.Monitors = OneOf(General.Monitors, "Primary", "All");
        General.BarHeight = Math.Clamp(General.BarHeight, 24, 56);
        General.FontSize = Math.Clamp(General.FontSize, 11, 18);
        General.BackgroundOpacity = double.IsNaN(General.BackgroundOpacity) ? 0.6 : Math.Clamp(General.BackgroundOpacity, 0.0, 1.0);

        Desktops.DotSize = Math.Clamp(Desktops.DotSize, 6, 14);

        if (string.IsNullOrWhiteSpace(Clock.DateFormat) || !DateFormats.TryFormat(Clock.DateFormat, DateTime.Now, System.Globalization.CultureInfo.CurrentCulture, out _))
            Clock.DateFormat = DateFormats.Default;

        Weather.Unit = OneOf(Weather.Unit, "Celsius", "Fahrenheit");
        Weather.IntervalMinutes = Math.Clamp(Weather.IntervalMinutes, 5, 120);

        Stats.NetworkUnit = OneOf(Stats.NetworkUnit, "Bytes", "Bits");
        Stats.NetworkScale = OneOf(Stats.NetworkScale, "Auto", "Kilo", "Mega");
        Stats.IntervalMs = Math.Clamp(Stats.IntervalMs, 500, 5000);
        Stats.WarnThreshold = Math.Clamp(Stats.WarnThreshold, 50, 100);
        Stats.CriticalThreshold = Math.Clamp(Stats.CriticalThreshold, 50, 100);
        if (Stats.CriticalThreshold < Stats.WarnThreshold) Stats.CriticalThreshold = Stats.WarnThreshold;

        Pomodoro.WorkMinutes = Math.Clamp(Pomodoro.WorkMinutes, 1, 99);
        Pomodoro.ShortBreakMinutes = Math.Clamp(Pomodoro.ShortBreakMinutes, 1, 99);
        Pomodoro.Sessions = Math.Clamp(Pomodoro.Sessions, 1, 12);
        Pomodoro.LongBreakMinutes = Math.Clamp(Pomodoro.LongBreakMinutes, 1, 99);

        UrlChecker.Normalize();

        Layout.Normalize();
        Layout.RemoveUnavailable(Features); // a switched-off feature never keeps a slot on the bar
    }

    public void CopyFrom(AppSettings other)
    {
        General = other.General;
        Desktops = other.Desktops;
        Clock = other.Clock;
        Weather = other.Weather;
        Stats = other.Stats;
        Pomodoro = other.Pomodoro;
        Layout = other.Layout;
        Features = other.Features;
        UrlChecker = other.UrlChecker;
        Normalize();
    }

    private static string OneOf(string? value, params string[] allowed)
    {
        foreach (var a in allowed)
            if (string.Equals(a, value, StringComparison.OrdinalIgnoreCase)) return a;
        return allowed[0];
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip)]
[JsonSerializable(typeof(AppSettings))]
public sealed partial class SettingsJsonContext : JsonSerializerContext
{
}
