using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TopBar.Platform;

/// <summary>Restartable base class for services with one background loop. Errors are logged, never rethrown.</summary>
public abstract class LoopService : IHostedService, IDisposable
{
    private CancellationTokenSource? _cts;
    private Task? _loop;
    protected ILogger Log { get; }
    private readonly HashSet<string> _logged = new();

    protected LoopService(ILogger log) => Log = log;

    protected abstract string ModuleName { get; }

    protected virtual void OnStarting() { }
    protected virtual void OnStopped() { }
    protected abstract Task RunAsync(CancellationToken ct);

    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        if (_cts is not null) return Task.CompletedTask;
        try { OnStarting(); }
        catch (Exception ex) { Log.LogError(ex, "{Module}: start failed", ModuleName); }
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        _loop = Task.Run(async () =>
        {
            try { await RunAsync(ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { /* stopping */ }
            catch (Exception ex) { Log.LogError(ex, "{Module}: loop terminated", ModuleName); }
        }, ct);
        return Task.CompletedTask;
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken)
    {
        var cts = _cts;
        if (cts is null) return;
        _cts = null;
        try { cts.Cancel(); } catch (ObjectDisposedException) { }
        try { if (_loop is not null) await _loop.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false); }
        catch (Exception ex) when (ex is TimeoutException or OperationCanceledException) { }
        cts.Dispose();
        try { OnStopped(); }
        catch (Exception ex) { Log.LogError(ex, "{Module}: stop failed", ModuleName); }
    }

    /// <summary>Logs an error only the first time per key (no per-tick log spam).</summary>
    protected void LogOnce(string key, Exception ex, string message)
    {
        lock (_logged)
            if (!_logged.Add(key)) return;
        Log.LogWarning(ex, "{Module}: {Message}", ModuleName, message);
    }

    protected void ClearLogged(string key)
    {
        lock (_logged) _logged.Remove(key);
    }

    public void Dispose()
    {
        StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
