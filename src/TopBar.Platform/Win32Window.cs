using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace TopBar.Platform;

public static class Win32Window
{
    /// <summary>Adds WS_EX_TOOLWINDOW so the window is not shown in the taskbar or Alt+Tab.</summary>
    public static void MakeToolWindow(nint hwnd)
    {
        var h = new HWND(hwnd);
        var style = PInvoke.GetWindowLong(h, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        PInvoke.SetWindowLong(h, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (int)style | (int)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
    }
}
