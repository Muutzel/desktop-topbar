using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using TopBar.Core.Formatting;
using TopBar.Core.Services;

namespace TopBar.Platform.Services;

public sealed class ClockService : LoopService, IClockService
{
    private readonly ISettingsService _settings;
    private readonly WakeableDelay _delay = new();

    public ClockService(ISettingsService settings, ILogger<ClockService> log) : base(log) => _settings = settings;

    protected override string ModuleName => "Clock";

    public DateTime Now => DateTime.Now;
    public event EventHandler? Tick;

    protected override void OnStarting()
    {
        _settings.Changed += OnSettingsChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    protected override void OnStopped()
    {
        _settings.Changed -= OnSettingsChanged;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
    }

    private void OnSettingsChanged(object? sender, Core.Settings.AppSettings e) => _delay.Wake();

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume) _delay.Wake();
    }

    protected override async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { Tick?.Invoke(this, EventArgs.Empty); }
            catch (Exception ex) { LogOnce("tick", ex, "Tick handler failed"); }

            // Wait exactly until the next boundary; +5 ms so we never fire a hair before the wall clock changes.
            var delay = ClockFormats.NextTickDelay(DateTime.Now, _settings.Current.Clock.ShowSeconds) + TimeSpan.FromMilliseconds(5);
            await _delay.DelayAsync(delay, ct).ConfigureAwait(false);
        }
    }
}
