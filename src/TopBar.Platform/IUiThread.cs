namespace TopBar.Platform;

/// <summary>Posts work to the UI thread (implemented in the App with the DispatcherQueue).</summary>
public interface IUiThread
{
    void Post(Action action);
}
