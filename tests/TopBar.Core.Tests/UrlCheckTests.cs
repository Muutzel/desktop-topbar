using TopBar.Core.Settings;
using TopBar.Core.UrlCheck;

namespace TopBar.Core.Tests;

public class UrlCheckTests
{
    [Theory]
    [InlineData(200, true)]
    [InlineData(204, true)]
    [InlineData(299, true)]
    [InlineData(199, false)]
    [InlineData(301, false)]
    [InlineData(401, false)]
    [InlineData(404, false)]
    [InlineData(503, false)]
    public void IsOk_AcceptsOnly2xx(int status, bool expected) => Assert.Equal(expected, UrlCheckRules.IsOk(status));

    private static UrlCheckResult Result(bool ok, int failures) =>
        new("https://x.test", ok, ok ? 200 : 503, ok ? UrlFailure.None : UrlFailure.Http, failures, DateTimeOffset.Now);

    [Fact]
    public void FirstFailure_IsPending_NotAProblem()
    {
        var r = Result(false, 1);
        Assert.False(r.IsProblem);
        var snap = new UrlCheckSnapshot(false, [r], null);
        Assert.False(snap.HasProblem);
        Assert.True(snap.HasPending);
        Assert.Equal(1, snap.OkCount);
    }

    [Fact]
    public void SecondFailureInARow_IsAProblem()
    {
        var snap = new UrlCheckSnapshot(false, [Result(true, 0), Result(false, UrlCheckRules.FailuresUntilProblem)], null);
        Assert.True(snap.HasProblem);
        Assert.False(snap.HasPending);
        Assert.Equal(1, snap.ProblemCount);
        Assert.Equal(1, snap.OkCount);
        Assert.Equal(2, snap.Total);
    }

    [Fact]
    public void Empty_HasNothing()
    {
        Assert.Equal(0, UrlCheckSnapshot.Empty.Total);
        Assert.False(UrlCheckSnapshot.Empty.HasProblem);
    }

    [Theory]
    [InlineData("https://google.com", "https://google.com")]
    [InlineData("  intranet.local/health ", "https://intranet.local/health")]
    [InlineData("http://10.0.0.5:8080/ping", "http://10.0.0.5:8080/ping")]
    public void TryNormalizeUrl_AcceptsAndCompletes(string input, string expected)
    {
        Assert.True(UrlCheckerSettings.TryNormalizeUrl(input, out var url));
        Assert.Equal(expected, url);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("ftp://example.com")]
    [InlineData("file:///C:/x")]
    [InlineData("https://")]
    public void TryNormalizeUrl_RejectsInvalid(string? input) => Assert.False(UrlCheckerSettings.TryNormalizeUrl(input, out _));

    [Fact]
    public void Normalize_DropsInvalidAndDuplicates_AndClamps()
    {
        var s = new UrlCheckerSettings
        {
            Urls = ["https://a.test", "HTTPS://A.TEST", "nope://x", "", "b.test"],
            IntervalMinutes = 0,
            TimeoutSeconds = 999,
        };
        s.Normalize();
        Assert.Equal(["https://a.test", "https://b.test"], s.Urls);
        Assert.Equal(1, s.IntervalMinutes);
        Assert.Equal(60, s.TimeoutSeconds);
    }

    [Fact]
    public void Normalize_LimitsTheNumberOfUrls()
    {
        var s = new UrlCheckerSettings { Urls = Enumerable.Range(0, 200).Select(i => $"https://h{i}.test").ToList() };
        s.Normalize();
        Assert.Equal(UrlCheckerSettings.MaxUrls, s.Urls.Count);
    }

    [Fact]
    public void Defaults_FeatureOff_OneGoogleUrl()
    {
        var a = new AppSettings();
        Assert.False(a.Features.UrlChecker);
        Assert.Equal(["https://google.com"], a.UrlChecker.Urls);
    }

    [Fact]
    public void AppSettingsNormalize_RemovesCheckerFromLayoutWhileFeatureIsOff()
    {
        var a = new AppSettings();
        a.Layout.Place(BarElementIds.UrlChecker, BarZone.Left);
        a.Normalize();
        Assert.False(a.Layout.IsPlaced(BarElementIds.UrlChecker));

        a.Features.UrlChecker = true;
        a.Layout.Place(BarElementIds.UrlChecker, BarZone.Left);
        a.Normalize();
        Assert.True(a.Layout.IsPlaced(BarElementIds.UrlChecker));
    }
}
