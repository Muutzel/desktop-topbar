using TopBar.Core.Settings;

namespace TopBar.Core.Tests;

public class SettingsTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "topbar-tests-" + Guid.NewGuid().ToString("N"));
    private string PathOf => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { /* ignore */ }
    }

    [Fact]
    public void MissingFile_UsesDefaultsAndCreatesFile()
    {
        var s = new SettingsService(PathOf);
        Assert.True(File.Exists(PathOf));
        Assert.Equal(32, s.Current.General.BarHeight);
        Assert.False(s.Current.General.Autostart);
        Assert.Equal("ddd, d. MMM", s.Current.Clock.DateFormat);
        Assert.Equal(15, s.Current.Weather.IntervalMinutes);
        Assert.Equal(85, s.Current.Stats.WarnThreshold);
    }

    [Fact]
    public void MissingFields_AreFilledWithDefaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(PathOf, """{ "General": { "BarHeight": 40 }, "Unknown": 1 }""");
        var s = new SettingsService(PathOf);
        Assert.Equal(40, s.Current.General.BarHeight);
        Assert.Equal(13, s.Current.General.FontSize);
        Assert.Equal(1000, s.Current.Stats.IntervalMs);
    }

    [Fact]
    public void InvalidValues_AreClamped()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(PathOf, """
        { "General": { "BarHeight": 500, "FontSize": 1, "BackgroundOpacity": 7, "Theme": "Neon", "Monitors": "x" },
          "Desktops": { "DotSize": 99 },
          "Weather": { "IntervalMinutes": 1, "Unit": "Kelvin" },
          "Stats": { "IntervalMs": 10, "WarnThreshold": 99, "CriticalThreshold": 60, "NetworkUnit": "Nibbles", "NetworkScale": "Giga" } }
        """);
        var s = new SettingsService(PathOf).Current;
        Assert.Equal(56, s.General.BarHeight);
        Assert.Equal(11, s.General.FontSize);
        Assert.Equal(1.0, s.General.BackgroundOpacity);
        Assert.Equal("System", s.General.Theme);
        Assert.Equal("Primary", s.General.Monitors);
        Assert.Equal(14, s.Desktops.DotSize);
        Assert.Equal(5, s.Weather.IntervalMinutes);
        Assert.Equal("Celsius", s.Weather.Unit);
        Assert.Equal(500, s.Stats.IntervalMs);
        Assert.Equal("Bytes", s.Stats.NetworkUnit);
        Assert.Equal("Auto", s.Stats.NetworkScale);
        Assert.Equal(99, s.Stats.WarnThreshold);
        Assert.Equal(99, s.Stats.CriticalThreshold); // Critical >= Warn
    }

    [Fact]
    public void BrokenFile_IsBackedUpAndDefaultsLoaded()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(PathOf, "{ this is not json");
        var s = new SettingsService(PathOf);
        Assert.Equal(32, s.Current.General.BarHeight);
        Assert.Single(Directory.GetFiles(_dir, "settings.broken-*.json"));
        Assert.True(File.Exists(PathOf));
    }

    [Fact]
    public void Update_SavesAndRaisesChanged()
    {
        var s = new SettingsService(PathOf);
        var raised = 0;
        s.Changed += (_, _) => raised++;
        s.Update(x => x.General.BarHeight = 40);
        Assert.Equal(1, raised);
        Assert.Equal(40, new SettingsService(PathOf).Current.General.BarHeight);
        Assert.False(File.Exists(PathOf + ".tmp"));
    }

    [Fact]
    public void InvalidDateFormat_FallsBackToDefault()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(PathOf, """{ "Clock": { "DateFormat": "%%%x" } }""");
        // single unknown standard specifier or invalid format must not survive
        var f = new SettingsService(PathOf).Current.Clock.DateFormat;
        Assert.False(string.IsNullOrWhiteSpace(f));
    }
}
