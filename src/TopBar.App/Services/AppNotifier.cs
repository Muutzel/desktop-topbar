using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using TopBar.Platform;

namespace TopBar.App.Services;

/// <summary>Windows toast notifications (Windows App SDK, works unpackaged). Falls back to the system sound if a toast cannot be shown.</summary>
internal static class AppNotifier
{
    private static bool _registered;

    public static void Initialize()
    {
        try
        {
            AppNotificationManager.Default.NotificationInvoked += (_, _) => { }; // clicking the toast needs no action
            AppNotificationManager.Default.Register();
            _registered = true;
        }
        catch (Exception ex)
        {
            AppController.LogWarning(ex, "Toast notifications could not be registered");
        }
    }

    public static void Shutdown()
    {
        if (!_registered) return;
        try { AppNotificationManager.Default.Unregister(); }
        catch { /* ignore */ }
    }

    public static void Show(string title, string text)
    {
        try
        {
            if (!_registered) throw new InvalidOperationException("not registered");
            var toast = new AppNotificationBuilder().AddText(title).AddText(text).BuildNotification();
            AppNotificationManager.Default.Show(toast);
        }
        catch (Exception ex)
        {
            AppController.LogWarning(ex, "Toast failed, using system sound");
            SystemSound.Notify();
        }
    }
}
