using System.Text.Json;
using System.Text.Json.Serialization;
using TopBar.Core.Formatting;

namespace TopBar.Core.Settings;

public sealed class GeneralSettings
{
    public bool Autostart { get; set; } = true;
    public string Theme { get; set; } = "System";       // System | Light | Dark
    public string Monitors { get; set; } = "Primary";   // Primary | All
    public int BarHeight { get; set; } = 32;
    public int FontSize { get; set; } = 13;
    public double BackgroundOpacity { get; set; } = 0.6;
}

public sealed class DesktopSettings
{
    public bool Show { get; set; } = true;
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
    public bool Show { get; set; } = true;
    public string? LocationName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Unit { get; set; } = "Celsius";       // Celsius | Fahrenheit
    public int IntervalMinutes { get; set; } = 15;

    public bool HasLocation => Latitude.HasValue && Longitude.HasValue;
}

public sealed class StatsSettings
{
    public bool ShowCpu { get; set; } = true;
    public bool ShowGpu { get; set; } = true;
    public bool ShowRam { get; set; } = true;
    public bool ShowNetwork { get; set; } = true;
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
    }

    public void CopyFrom(AppSettings other)
    {
        General = other.General;
        Desktops = other.Desktops;
        Clock = other.Clock;
        Weather = other.Weather;
        Stats = other.Stats;
        Pomodoro = other.Pomodoro;
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
