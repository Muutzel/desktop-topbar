using CommunityToolkit.Mvvm.ComponentModel;
using TopBar.Core.Services;
using TopBar.Core.Stats;
using TopBar.Platform;

namespace TopBar.App.ViewModels;

public sealed partial class StatEntry : ObservableObject
{
    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private string _text = "";
    [ObservableProperty] private string _tooltip = "";
    [ObservableProperty] private StatFormat.Level _level;
    /// <summary>Optional table cells (number, unit, number, unit). Empty = plain <see cref="Text"/>.</summary>
    [ObservableProperty] private string[] _cells = [];
    /// <summary>Units the unit column must be able to hold (width reservation).</summary>
    [ObservableProperty] private string[] _unitSamples = [];
}

public sealed partial class StatsViewModel : ObservableObject
{
    private readonly ISystemStatsService _stats;
    private readonly ISettingsService _settings;

    public StatEntry Cpu { get; } = new();
    public StatEntry Gpu { get; } = new();
    public StatEntry Ram { get; } = new();
    public StatEntry Network { get; } = new();

    [ObservableProperty] private bool _showLabels = true;

    public event Action? Updated;

    internal StatsViewModel(ISystemStatsService stats, ISettingsService settings, IUiThread ui)
    {
        _stats = stats;
        _settings = settings;
        stats.Changed += (_, _) => ui.Post(Update);
        settings.Changed += (_, _) => ui.Post(Update);
        Update();
    }

    public void Update()
    {
        var s = _stats.Current;
        var c = _settings.Current.Stats;
        ShowLabels = c.ShowLabels;

        Fill(Cpu, c.ShowCpu, s.CpuPercent, "CPU-Auslastung", c);
        // GPU is hidden entirely when PDH delivers no GPU counters.
        Fill(Gpu, c.ShowGpu && s.GpuPercent.HasValue, s.GpuPercent, "GPU-Auslastung", c);

        Ram.IsVisible = c.ShowRam;
        Ram.Text = StatFormat.Percent(s.RamPercent);
        Ram.Tooltip = $"Arbeitsspeicher: {StatFormat.Memory(s.RamUsedBytes, s.RamTotalBytes)}";
        Ram.Level = StatFormat.GetLevel(s.RamPercent, c.WarnThreshold, c.CriticalThreshold);

        Network.IsVisible = c.ShowNetwork;
        var display = NetworkDisplay.From(c.NetworkUnit, c.NetworkScale);
        Network.Text = NetworkRate.FormatPair(s.DownBytesPerSec, s.UpBytesPerSec, display);
        var down = NetworkRate.Parts(s.DownBytesPerSec, display);
        var up = NetworkRate.Parts(s.UpBytesPerSec, display);
        Network.Cells = [down.Number, down.Unit, up.Number, up.Unit];
        Network.UnitSamples = NetworkRate.UnitSamples(display);
        Network.Tooltip = string.IsNullOrEmpty(s.NetworkDescription) ? "Netzwerk" : s.NetworkDescription;
        Network.Level = StatFormat.Level.Normal;

        Updated?.Invoke();
    }

    private static void Fill(StatEntry entry, bool visible, double? value, string tooltipPrefix, Core.Settings.StatsSettings c)
    {
        entry.IsVisible = visible;
        if (value is { } v)
        {
            entry.Text = StatFormat.Percent(v);
            entry.Tooltip = $"{tooltipPrefix}: {StatFormat.Percent(v)}";
            entry.Level = StatFormat.GetLevel(v, c.WarnThreshold, c.CriticalThreshold);
        }
        else
        {
            entry.Text = "– %"; // first sample not available yet
            entry.Tooltip = tooltipPrefix;
            entry.Level = StatFormat.Level.Normal;
        }
    }
}
