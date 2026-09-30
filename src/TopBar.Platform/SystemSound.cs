using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace TopBar.Platform;

public static class SystemSound
{
    /// <summary>Plays the default system notification sound (what the user set for "Asterisk"). Never throws.</summary>
    public static void Notify()
    {
        try { PInvoke.MessageBeep(MESSAGEBOX_STYLE.MB_ICONASTERISK); }
        catch { /* a missing sound must never break the timer */ }
    }
}
