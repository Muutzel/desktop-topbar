using CommunityToolkit.Mvvm.ComponentModel;
using TopBar.Core.Services;
using TopBar.Platform;

namespace TopBar.App.ViewModels;

public sealed partial class DesktopsViewModel : ObservableObject
{
    private readonly IVirtualDesktopService _desktops;
    private readonly ISettingsService _settings;

    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private IReadOnlyList<DesktopInfo> _items = Array.Empty<DesktopInfo>();
    [ObservableProperty] private int _currentIndex;

    public event Action? Updated;

    internal DesktopsViewModel(IVirtualDesktopService desktops, ISettingsService settings, IUiThread ui)
    {
        _desktops = desktops;
        _settings = settings;
        desktops.Changed += (_, _) => ui.Post(Update);
        settings.Changed += (_, _) => ui.Post(Update);
        Update();
    }

    public void Update()
    {
        IsVisible = _settings.Current.Desktops.Show && _desktops.IsAvailable;
        Items = _desktops.Desktops;
        CurrentIndex = _desktops.CurrentIndex;
        Updated?.Invoke();
    }

    public void SwitchTo(int index)
    {
        if (index >= 0 && index < Items.Count) _desktops.SwitchTo(index);
    }

    /// <summary>Mouse wheel: -1 = previous, +1 = next. Does not wrap around.</summary>
    public void Scroll(int direction)
    {
        if (!_settings.Current.Desktops.ScrollSwitch) return;
        var target = CurrentIndex + direction;
        if (target < 0 || target >= Items.Count) return;
        _desktops.SwitchTo(target);
    }

    public static string DisplayName(DesktopInfo d) => string.IsNullOrWhiteSpace(d.Name) ? $"Desktop {d.Index + 1}" : d.Name;
}

public sealed class BarViewModels
{
    public ClockViewModel Clock { get; }
    public WeatherViewModel Weather { get; }
    public StatsViewModel Stats { get; }
    public DesktopsViewModel Desktops { get; }

    internal BarViewModels(IClockService clock, IWeatherService weather, ISystemStatsService stats, IVirtualDesktopService desktops, ISettingsService settings, IUiThread ui)
    {
        Clock = new ClockViewModel(clock, settings, ui);
        Weather = new WeatherViewModel(weather, settings, ui);
        Stats = new StatsViewModel(stats, settings, ui);
        Desktops = new DesktopsViewModel(desktops, settings, ui);
    }

    /// <summary>Re-reads everything after services were restarted.</summary>
    public void RefreshAll()
    {
        Clock.Update(); Weather.Update(); Stats.Update(); Desktops.Update();
    }
}
