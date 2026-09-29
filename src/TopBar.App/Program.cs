using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using TopBar.App.Services;

namespace TopBar.App;

public static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        if (RedirectToExistingInstance()) return 0;

        Microsoft.UI.Xaml.Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
        return 0;
    }

    /// <summary>Single instance: a second start forwards its activation to the first instance and exits.</summary>
    private static bool RedirectToExistingInstance()
    {
        var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        var keyInstance = AppInstance.FindOrRegisterForKey("TopBar.SingleInstance");

        if (keyInstance.IsCurrent)
        {
            keyInstance.Activated += (_, e) => AppController.OnRedirectedActivation(IsAutostart(e));
            return false;
        }

        // Must not block the STA thread while the redirect runs.
        var done = new Semaphore(0, 1);
        Task.Run(() =>
        {
            try { keyInstance.RedirectActivationToAsync(activationArgs).AsTask().Wait(); }
            finally { done.Release(); }
        });
        done.WaitOne();
        return true;
    }

    private static bool IsAutostart(AppActivationArguments e)
    {
        try
        {
            if (e.Kind == ExtendedActivationKind.Launch && e.Data is Windows.ApplicationModel.Activation.ILaunchActivatedEventArgs launch)
                return launch.Arguments?.Contains("--autostart", StringComparison.OrdinalIgnoreCase) == true;
        }
        catch { /* treat as manual start */ }
        return false;
    }
}
