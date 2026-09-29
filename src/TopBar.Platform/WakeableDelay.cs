namespace TopBar.Platform;

/// <summary>A delay that can be cut short with <see cref="Wake"/> (e.g. on settings change or resume from standby).</summary>
internal sealed class WakeableDelay
{
    private CancellationTokenSource? _current;
    private volatile bool _signaled;

    public async Task DelayAsync(TimeSpan delay, CancellationToken ct)
    {
        if (_signaled) { _signaled = false; return; }
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _current = cts;
        try
        {
            if (_signaled) { _signaled = false; return; }
            await Task.Delay(delay, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _signaled = false;
        }
        finally
        {
            _current = null;
        }
    }

    public void Wake()
    {
        _signaled = true;
        try { _current?.Cancel(); } catch (ObjectDisposedException) { /* ignore */ }
    }
}
