using TopBar.Core.Settings;
using TopBar.Core.UrlCheck;

namespace TopBar.Core.Services;

public interface ISettingsService
{
    AppSettings Current { get; }
    /// <summary>Applies the change, saves immediately and raises <see cref="Changed"/>.</summary>
    void Update(Action<AppSettings> change);
    event EventHandler<AppSettings> Changed;
}

public sealed record DesktopInfo(int Index, string Name);

public interface IVirtualDesktopService
{
    /// <summary>False when the DLL or the Windows build does not fit.</summary>
    bool IsAvailable { get; }
    IReadOnlyList<DesktopInfo> Desktops { get; }
    int CurrentIndex { get; }
    void SwitchTo(int index);
    /// <summary>Count, names or current desktop changed.</summary>
    event EventHandler Changed;
}

public sealed record WeatherSnapshot(
    double Temperature, double ApparentTemperature, int WeatherCode, bool IsDay,
    double WindSpeed, double TempMax, double TempMin, int PrecipitationProbability,
    DateTimeOffset FetchedAt, bool IsStale);

public sealed record GeoLocation(string Name, string? Admin1, string Country, double Latitude, double Longitude)
{
    public string DisplayName => string.Join(", ", new[] { Name, Admin1, Country }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

public interface IWeatherService
{
    /// <summary>Null until the first successful fetch.</summary>
    WeatherSnapshot? Current { get; }
    Task<IReadOnlyList<GeoLocation>> SearchLocationAsync(string query, CancellationToken ct);
    Task RefreshAsync(CancellationToken ct);
    event EventHandler Changed;
}

public sealed record SystemStats(
    double? CpuPercent, double? GpuPercent,
    double RamPercent, ulong RamUsedBytes, ulong RamTotalBytes,
    double DownBytesPerSec, double UpBytesPerSec,
    string NetworkDescription = "")
{
    public static SystemStats Empty { get; } = new(null, null, 0, 0, 0, 0, 0);
}

public interface ISystemStatsService
{
    SystemStats Current { get; }
    /// <summary>Interfaces considered for the network value (Id, display name).</summary>
    IReadOnlyList<(string Id, string Name)> NetworkInterfaces { get; }
    event EventHandler Changed;
}

public interface IClockService
{
    DateTime Now { get; }
    /// <summary>Raised exactly on the second (seconds shown) or minute change.</summary>
    event EventHandler Tick;
}

public interface IAutostartService
{
    bool IsEnabled { get; }
    void SetEnabled(bool enabled);
    void RepairPathIfNeeded();
}

public interface IUrlCheckService
{
    UrlCheckSnapshot Current { get; }
    /// <summary>Runs a check round as soon as possible (no effect while the feature is off or no URL is configured).</summary>
    void CheckNow();
    /// <summary>A round started, finished, or the URL list changed.</summary>
    event EventHandler Changed;
}
