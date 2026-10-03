using TopBar.Core.Settings;

namespace TopBar.Core.Tests;

public class FeatureTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "topbar-tests-" + Guid.NewGuid().ToString("N"));
    private string PathOf => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        try { Directory.Delete(_dir, true); } catch { /* ignore */ }
    }

    [Fact]
    public void Defaults_EverythingOn_ExceptUrlChecker()
    {
        var f = new FeatureSettings();
        foreach (var id in BarElementIds.All.Where(id => id != BarElementIds.UrlChecker))
            Assert.True(f.IsElementEnabled(id), id);
        Assert.False(f.IsElementEnabled(BarElementIds.UrlChecker));
    }

    [Fact]
    public void EveryElementId_HasAFeatureSwitch()
    {
        // Switching one element off must change exactly that element: guards against an id missing in IsElementEnabled.
        foreach (var id in BarElementIds.All)
        {
            var on = new FeatureSettings { UrlChecker = true };
            Assert.True(on.IsElementEnabled(id), id);
            var off = Without(on, id);
            Assert.False(off.IsElementEnabled(id), id);
            Assert.All(BarElementIds.All.Where(other => other != id), other => Assert.True(off.IsElementEnabled(other), other));
        }
    }

    private static FeatureSettings Without(FeatureSettings f, string id)
    {
        switch (id)
        {
            case BarElementIds.Desktops: f.Desktops = false; break;
            case BarElementIds.Pomodoro: f.Pomodoro = false; break;
            case BarElementIds.Weather: f.Weather = false; break;
            case BarElementIds.Clock: f.Clock = false; break;
            case BarElementIds.Date: f.Date = false; break;
            case BarElementIds.Cpu: f.Cpu = false; break;
            case BarElementIds.Gpu: f.Gpu = false; break;
            case BarElementIds.Ram: f.Ram = false; break;
            case BarElementIds.Network: f.Network = false; break;
            case BarElementIds.SettingsButton: f.SettingsButton = false; break;
            case BarElementIds.UrlChecker: f.UrlChecker = false; break;
            default: throw new ArgumentException(id);
        }
        return f;
    }

    [Fact]
    public void SetElementEnabled_RoundTripsForEveryElement()
    {
        foreach (var id in BarElementIds.All)
        {
            var f = new FeatureSettings();
            f.SetElementEnabled(id, true);
            Assert.True(f.IsElementEnabled(id), id);
            f.SetElementEnabled(id, false);
            Assert.False(f.IsElementEnabled(id), id);
        }
        new FeatureSettings().SetElementEnabled("Nope", false); // unknown ids are ignored
    }

    [Fact]
    public void Pages_FollowTheirElements()
    {
        var f = new FeatureSettings();
        Assert.True(f.IsPageEnabled("general"));
        Assert.True(f.IsPageEnabled("layout"));
        Assert.True(f.IsPageEnabled("info"));
        Assert.True(f.IsPageEnabled("pomodoro"));
        Assert.False(f.IsPageEnabled("urlchecker"));

        f.Clock = false;
        Assert.True(f.IsPageEnabled("clock"));  // the date is still on
        f.Date = false;
        Assert.False(f.IsPageEnabled("clock"));

        f.Cpu = f.Gpu = f.Ram = false;
        Assert.True(f.IsPageEnabled("system")); // network is still on
        f.Network = false;
        Assert.False(f.IsPageEnabled("system"));

        f.Pomodoro = false;
        Assert.False(f.IsPageEnabled("pomodoro"));
    }

    [Fact]
    public void Normalize_RemovesSwitchedOffElementsFromTheLayout_AndOffersThemNowhere()
    {
        var a = new AppSettings();
        a.Features.Pomodoro = false;
        a.Normalize();
        Assert.False(a.Layout.IsPlaced(BarElementIds.Pomodoro));
        Assert.DoesNotContain(BarElementIds.Pomodoro, a.Layout.Unplaced(a.Features));
        Assert.True(a.Layout.IsPlaced(BarElementIds.Clock));
    }

    [Fact]
    public void SettingsFile_IsWrittenCompletelyAtStart()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(PathOf, """{ "General": { "BarHeight": 40 } }""");
        _ = new SettingsService(PathOf);
        var text = File.ReadAllText(PathOf);
        Assert.Contains("\"Features\"", text);
        Assert.Contains("\"UrlChecker\": false", text);
        Assert.Contains("\"Pomodoro\": true", text);
        Assert.Contains("\"BarHeight\": 40", text);
    }

    [Fact]
    public void SwitchedOffElement_SurvivesReload_AsOff()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(PathOf, """{ "Features": { "Weather": false } }""");
        var s = new SettingsService(PathOf);
        Assert.False(s.Current.Features.Weather);
        Assert.False(s.Current.Layout.IsPlaced(BarElementIds.Weather));
        Assert.False(new SettingsService(PathOf).Current.Features.Weather);
    }
}
