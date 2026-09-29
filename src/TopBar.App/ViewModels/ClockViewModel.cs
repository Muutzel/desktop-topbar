using CommunityToolkit.Mvvm.ComponentModel;
using TopBar.Core.Formatting;
using TopBar.Core.Services;
using TopBar.Platform;

namespace TopBar.App.ViewModels;

public sealed partial class ClockViewModel : ObservableObject
{
    private readonly IClockService _clock;
    private readonly ISettingsService _settings;

    [ObservableProperty] private string _time = "";
    [ObservableProperty] private string _date = "";

    /// <summary>Raised once after each refresh (on the UI thread).</summary>
    public event Action? Updated;

    internal ClockViewModel(IClockService clock, ISettingsService settings, IUiThread ui)
    {
        _clock = clock;
        _settings = settings;
        clock.Tick += (_, _) => ui.Post(Update);
        settings.Changed += (_, _) => ui.Post(Update);
        Update();
    }

    public void Update()
    {
        var now = _clock.Now;
        var c = _settings.Current.Clock;
        Time = ClockFormats.FormatTime(now, c.Use24h, c.ShowSeconds);
        Date = DateFormats.TryFormat(c.DateFormat, now, System.Globalization.CultureInfo.CurrentCulture, out var d)
            ? d
            : DateFormats.TryFormat(DateFormats.Default, now, System.Globalization.CultureInfo.CurrentCulture, out var fallback) ? fallback : "";
        Updated?.Invoke();
    }
}
