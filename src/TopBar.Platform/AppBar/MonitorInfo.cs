using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.HiDpi;

namespace TopBar.Platform.AppBar;

/// <summary>A display monitor in physical pixels. <see cref="DeviceName"/> is the stable identity (e.g. \\.\DISPLAY1).</summary>
public sealed record MonitorInfo(nint Handle, string DeviceName, int Left, int Top, int Right, int Bottom, bool IsPrimary)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;

    /// <summary>DPI scale factor (1.0 = 96 dpi) for this monitor.</summary>
    public double DpiScale
    {
        get
        {
            try
            {
                var hr = PInvoke.GetDpiForMonitor(new HMONITOR(Handle), MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var dpiX, out _);
                return hr.Succeeded && dpiX > 0 ? dpiX / 96.0 : 1.0;
            }
            catch
            {
                return 1.0;
            }
        }
    }
}

public static unsafe class MonitorEnumerator
{
    public static IReadOnlyList<MonitorInfo> GetAll()
    {
        var list = new List<MonitorInfo>();
        PInvoke.EnumDisplayMonitors(HDC.Null, (RECT?)null, (hMon, hdc, rect, lParam) =>
        {
            var info = Query(hMon);
            if (info is not null) list.Add(info);
            return true;
        }, default);
        return list;
    }

    public static MonitorInfo? GetPrimary()
    {
        var all = GetAll();
        return all.FirstOrDefault(m => m.IsPrimary) ?? all.FirstOrDefault();
    }

    /// <summary>The monitor under the mouse cursor (nearest monitor if none).</summary>
    public static MonitorInfo? FromCursor()
    {
        if (!PInvoke.GetCursorPos(out var pt)) return GetPrimary();
        var h = PInvoke.MonitorFromPoint(pt, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        return Query(h) ?? GetPrimary();
    }

    public static MonitorInfo? FindByDeviceName(string deviceName) =>
        GetAll().FirstOrDefault(m => string.Equals(m.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));

    private static MonitorInfo? Query(HMONITOR hMon)
    {
        MONITORINFOEXW mi = default;
        mi.monitorInfo.cbSize = (uint)sizeof(MONITORINFOEXW);
        if (!PInvoke.GetMonitorInfo(hMon, (MONITORINFO*)&mi)) return null;
        var r = mi.monitorInfo.rcMonitor;
        const uint MONITORINFOF_PRIMARY = 1;
        return new MonitorInfo((nint)hMon.Value, mi.szDevice.ToString(), r.left, r.top, r.right, r.bottom,
            (mi.monitorInfo.dwFlags & MONITORINFOF_PRIMARY) != 0);
    }
}
