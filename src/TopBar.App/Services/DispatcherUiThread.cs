using Microsoft.UI.Dispatching;
using TopBar.Platform;

namespace TopBar.App.Services;

internal sealed class DispatcherUiThread : IUiThread
{
    private readonly DispatcherQueue _queue;

    public DispatcherUiThread(DispatcherQueue queue) => _queue = queue;

    public void Post(Action action)
    {
        if (_queue.HasThreadAccess) action();
        else _queue.TryEnqueue(() => action());
    }

    public DispatcherQueue Queue => _queue;
}
