using Microsoft.Extensions.Logging;
using TopBar.App.ViewModels;
using TopBar.App.Views;
using TopBar.Core.Services;
using TopBar.Platform.AppBar;

namespace TopBar.App.Services;

/// <summary>Creates one <see cref="BarWindow"/> per configured monitor and keeps them in sync with the display configuration.</summary>
internal sealed class BarManager
{
    private readonly ISettingsService _settings;
    private readonly BarViewModels _viewModels;
    private readonly ILogger _log;
    private readonly Dictionary<string, BarWindow> _windows = new();
    private readonly Debouncer _reconcileDebounce = new(400);
    private string _lastMonitorsSetting;

    public BarManager(ISettingsService settings, BarViewModels viewModels, ILogger log)
    {
        _settings = settings;
        _viewModels = viewModels;
        _log = log;
        _lastMonitorsSetting = settings.Current.General.Monitors;
        settings.Changed += (_, s) =>
        {
            if (s.General.Monitors != _lastMonitorsSetting)
            {
                _lastMonitorsSetting = s.General.Monitors;
                AppController.UiThread.Post(Reconcile);
            }
        };
    }

    public void Start() => Reconcile();

    private void ScheduleReconcile() => _reconcileDebounce.Run(Reconcile);

    /// <summary>Opens bars for missing monitors, closes bars of removed monitors, repositions the rest.</summary>
    public void Reconcile()
    {
        try
        {
            var all = MonitorEnumerator.GetAll();
            var wanted = _settings.Current.General.Monitors == "All"
                ? all.ToList()
                : all.Where(m => m.IsPrimary).Take(1).ToList();
            if (wanted.Count == 0 && all.Count > 0) wanted.Add(all[0]);
            var wantedNames = wanted.Select(m => m.DeviceName).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var name in _windows.Keys.Where(k => !wantedNames.Contains(k)).ToList())
            {
                _log.LogInformation("Closing bar on {Monitor}", name);
                _windows[name].CloseForReal();
                _windows.Remove(name);
            }

            foreach (var monitor in wanted)
            {
                if (_windows.TryGetValue(monitor.DeviceName, out var existing))
                {
                    existing.UpdatePosition();
                    continue;
                }
                _log.LogInformation("Opening bar on {Monitor}", monitor.DeviceName);
                var window = new BarWindow(monitor.DeviceName, _settings, _viewModels, _log);
                window.DisplayChanged += ScheduleReconcile;
                _windows[monitor.DeviceName] = window;
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "BarManager: reconcile failed");
        }
    }

    public void CloseAll()
    {
        foreach (var window in _windows.Values.ToList())
        {
            try { window.CloseForReal(); }
            catch (Exception ex) { _log.LogWarning(ex, "BarManager: closing bar failed"); }
        }
        _windows.Clear();
        AppBarController.RemoveAll(); // belt and braces: never leave a reserved strip behind
    }
}
