using Microsoft.UI.Dispatching;

namespace TopBar.App.Services;

/// <summary>Runs an action once after a quiet period (UI thread).</summary>
internal sealed class Debouncer
{
    private readonly DispatcherQueueTimer _timer;
    private Action? _pending;

    public Debouncer(int milliseconds = 300)
    {
        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(milliseconds);
        _timer.IsRepeating = false;
        _timer.Tick += (_, _) =>
        {
            var action = _pending;
            _pending = null;
            action?.Invoke();
        };
    }

    public void Run(Action action)
    {
        _pending = action;
        _timer.Stop();
        _timer.Start();
    }

    /// <summary>Runs a pending action immediately (e.g. when a page is closing).</summary>
    public void Flush()
    {
        _timer.Stop();
        var action = _pending;
        _pending = null;
        action?.Invoke();
    }
}
