using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using TopBar.Core.Services;
using TopBar.Core.Settings;
using TopBar.Core.UrlCheck;

namespace TopBar.Platform.Services;

/// <summary>
/// Calls the configured URLs one round at a time (GET, headers only, redirects followed). A URL counts as reachable on a 2xx answer.
/// Idle (no network traffic) while the feature is off, the element is not on the bar, or the list is empty.
/// </summary>
public sealed class UrlCheckService : LoopService, IUrlCheckService
{
    private static readonly HttpClient Http = CreateClient();

    /// <summary>Re-check after a first failure, so a real problem is confirmed (or a hiccup cleared) quickly.</summary>
    private static readonly TimeSpan PendingRetry = TimeSpan.FromSeconds(15);
    /// <summary>Re-check cadence while a URL is a confirmed problem (recovery should show up soon), at most the normal interval.</summary>
    private static readonly TimeSpan ProblemRetry = TimeSpan.FromMinutes(1);
    /// <summary>After standby the network (VPN, Wi-Fi) is often not back yet.</summary>
    private static readonly TimeSpan ResumeDelay = TimeSpan.FromSeconds(10);
    private const int MaxParallel = 8;

    private readonly ISettingsService _settings;
    private readonly WakeableDelay _delay = new();
    private readonly Dictionary<string, int> _failures = new(StringComparer.OrdinalIgnoreCase); // touched by the loop only
    private volatile UrlCheckSnapshot _snapshot = UrlCheckSnapshot.Empty;
    private string _lastKey = "";

    public UrlCheckService(ISettingsService settings, ILogger<UrlCheckService> log) : base(log) => _settings = settings;

    protected override string ModuleName => "UrlCheck";

    public event EventHandler? Changed;

    public UrlCheckSnapshot Current => _snapshot;

    public void CheckNow() => _delay.Wake();

    private static HttpClient CreateClient()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),      // pick up DNS changes
            DefaultProxyCredentials = CredentialCache.DefaultCredentials, // corporate proxies with Windows authentication
        };
        var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan }; // the timeout is applied per request from the settings
        client.DefaultRequestHeaders.UserAgent.ParseAdd("TopBar/1.0");
        return client;
    }

    protected override void OnStarting()
    {
        _lastKey = Key(_settings.Current);
        _settings.Changed += OnSettingsChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    protected override void OnStopped()
    {
        _settings.Changed -= OnSettingsChanged;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
    }

    /// <summary>Everything that decides what is called and whether anything is called at all (so unrelated settings do not trigger requests).</summary>
    private static string Key(AppSettings s) =>
        (s.Features.UrlChecker && s.Layout.IsPlaced(BarElementIds.UrlChecker) ? "on|" : "off|")
        + s.UrlChecker.TimeoutSeconds + "|" + string.Join('\n', s.UrlChecker.Urls);

    private void OnSettingsChanged(object? sender, AppSettings e)
    {
        var key = Key(e);
        if (key == _lastKey) return;
        _lastKey = key;
        _delay.Wake();
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode != PowerModes.Resume) return;
        _ = Task.Delay(ResumeDelay).ContinueWith(_ => _delay.Wake(), TaskScheduler.Default);
    }

    private bool IsActive()
    {
        var s = _settings.Current;
        return s.Features.UrlChecker && s.Layout.IsPlaced(BarElementIds.UrlChecker) && s.UrlChecker.Urls.Count > 0;
    }

    protected override async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (!IsActive())
            {
                _failures.Clear();
                if (_snapshot != UrlCheckSnapshot.Empty) Publish(UrlCheckSnapshot.Empty);
                await _delay.DelayAsync(TimeSpan.FromHours(1), ct).ConfigureAwait(false);
                continue;
            }

            var wait = await RunRoundAsync(ct).ConfigureAwait(false);
            await _delay.DelayAsync(wait, ct).ConfigureAwait(false);
        }
    }

    /// <summary>One round over all URLs. Returns how long to wait before the next one.</summary>
    private async Task<TimeSpan> RunRoundAsync(CancellationToken ct)
    {
        var cfg = _settings.Current.UrlChecker;
        var urls = cfg.Urls.ToList();
        var timeout = TimeSpan.FromSeconds(cfg.TimeoutSeconds);
        var interval = TimeSpan.FromMinutes(cfg.IntervalMinutes);

        // Show the "running" state but keep the old results of URLs that are still in the list.
        var kept = _snapshot.Results.Where(r => urls.Contains(r.Url, StringComparer.OrdinalIgnoreCase)).ToList();
        Publish(new UrlCheckSnapshot(true, kept, _snapshot.LastCompleted));

        using var gate = new SemaphoreSlim(MaxParallel);
        var checks = await Task.WhenAll(urls.Select(async url =>
        {
            await gate.WaitAsync(ct).ConfigureAwait(false);
            try { return await CheckOneAsync(url, timeout, ct).ConfigureAwait(false); }
            finally { gate.Release(); }
        })).ConfigureAwait(false);

        foreach (var stale in _failures.Keys.Where(k => !urls.Contains(k, StringComparer.OrdinalIgnoreCase)).ToList())
            _failures.Remove(stale);

        var now = DateTimeOffset.Now;
        var results = new List<UrlCheckResult>(checks.Length);
        foreach (var c in checks)
        {
            var failures = c.Ok ? 0 : _failures.GetValueOrDefault(c.Url) + 1;
            _failures[c.Url] = failures;
            results.Add(new UrlCheckResult(c.Url, c.Ok, c.Status, c.Failure, failures, now));
        }

        var snapshot = new UrlCheckSnapshot(false, results, now);
        Publish(snapshot);

        if (snapshot.HasPending) return PendingRetry;
        if (snapshot.HasProblem) return interval < ProblemRetry ? interval : ProblemRetry;
        return interval;
    }

    private async Task<(string Url, bool Ok, int? Status, UrlFailure Failure)> CheckOneAsync(string url, TimeSpan timeout, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            var code = (int)response.StatusCode;
            ClearLogged("fail:" + url);
            return UrlCheckRules.IsOk(code) ? (url, true, code, UrlFailure.None) : (url, false, code, UrlFailure.Http);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // stopping
        }
        catch (OperationCanceledException ex)
        {
            LogOnce("fail:" + url, ex, $"{url}: timeout");
            return (url, false, null, UrlFailure.Timeout);
        }
        catch (HttpRequestException ex)
        {
            LogOnce("fail:" + url, ex, $"{url}: request failed");
            return (url, false, null, Classify(ex));
        }
        catch (Exception ex)
        {
            LogOnce("fail:" + url, ex, $"{url}: unexpected error");
            return (url, false, null, UrlFailure.Other);
        }
    }

    private static UrlFailure Classify(HttpRequestException ex) => ex.HttpRequestError switch
    {
        HttpRequestError.NameResolutionError => UrlFailure.NameResolution,
        HttpRequestError.SecureConnectionError => UrlFailure.Tls,
        HttpRequestError.ConnectionError => UrlFailure.Connection,
        _ => UrlFailure.Other,
    };

    private void Publish(UrlCheckSnapshot snapshot)
    {
        _snapshot = snapshot;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
