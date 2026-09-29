using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;

namespace TopBar.Platform;

public static unsafe class Win32Window
{
    /// <summary>Adds WS_EX_TOOLWINDOW so the window is not shown in the taskbar or Alt+Tab.</summary>
    public static void MakeToolWindow(nint hwnd)
    {
        var h = new HWND(hwnd);
        var style = PInvoke.GetWindowLong(h, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        // Also clear the classic 3D edge styles: they draw a light/dark bevel around the whole window.
        var bevel = (int)(WINDOW_EX_STYLE.WS_EX_WINDOWEDGE | WINDOW_EX_STYLE.WS_EX_CLIENTEDGE |
                          WINDOW_EX_STYLE.WS_EX_DLGMODALFRAME | WINDOW_EX_STYLE.WS_EX_STATICEDGE);
        PInvoke.SetWindowLong(h, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, ((int)style | (int)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW) & ~bevel);
        PInvoke.SetWindowPos(h, HWND.Null, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER |
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);
    }

    /// <summary>Strips caption/border/thick-frame styles that WinUI leaves on the window; the non-client frame is what draws the light outline.</summary>
    public static uint StripFrame(nint hwnd)
    {
        var h = new HWND(hwnd);
        // WS_POPUP only: no caption, no (thick) frame, no DWM-drawn window border.
        var popup = unchecked((int)((uint)WINDOW_STYLE.WS_POPUP | (uint)WINDOW_STYLE.WS_VISIBLE));
        PInvoke.SetWindowLong(h, WINDOW_LONG_PTR_INDEX.GWL_STYLE, popup);
        PInvoke.SetWindowPos(h, HWND.Null, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER |
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);
        return (uint)PInvoke.GetWindowLong(h, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
    }

    /// <summary>Removes the 1px DWM border and rounded corners (Windows 11) so the bar sits flush with the screen edge.</summary>
    public static (int Border, int Corner) RemoveBorderAndCorners(nint hwnd)
    {
        var h = new HWND(hwnd);
        uint noBorder = 0xFFFFFFFE; // DWMWA_COLOR_NONE
        var r1 = PInvoke.DwmSetWindowAttribute(h, DWMWINDOWATTRIBUTE.DWMWA_BORDER_COLOR, &noBorder, sizeof(uint));
        int square = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DONOTROUND;
        var r2 = PInvoke.DwmSetWindowAttribute(h, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, &square, sizeof(int));
        return (r1.Value, r2.Value);
    }
}
