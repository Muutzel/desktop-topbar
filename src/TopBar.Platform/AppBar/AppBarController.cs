using Microsoft.Extensions.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace TopBar.Platform.AppBar;

/// <summary>
/// Registers a window as a top-edge AppBar (like the taskbar) so maximized windows start below it,
/// and handles the related system messages (position change, fullscreen apps, explorer restart, DPI/display change).
/// </summary>
public sealed unsafe class AppBarController : IDisposable
{
    // SHAppBarMessage commands / notifications / edges
    private const uint ABM_NEW = 0, ABM_REMOVE = 1, ABM_QUERYPOS = 2, ABM_SETPOS = 3, ABM_ACTIVATE = 6, ABM_WINDOWPOSCHANGED = 9;
    private const int ABN_POSCHANGED = 1, ABN_FULLSCREENAPP = 2;
    private const uint ABE_TOP = 1;

    // window messages
    private const uint WM_ACTIVATE = 0x0006, WM_SETTINGCHANGE = 0x001A, WM_GETMINMAXINFO = 0x0024,
        WM_NCCALCSIZE = 0x0083, WM_NCPAINT = 0x0085, WM_WINDOWPOSCHANGED = 0x0047, WM_DISPLAYCHANGE = 0x007E, WM_DPICHANGED = 0x02E0;
    private const int SPI_SETWORKAREA = 0x002F;

    private static readonly List<AppBarController> Active = new();

    private readonly HWND _hwnd;
    private readonly Func<MonitorInfo?> _getMonitor;
    private readonly Func<int> _getBarHeightDip;
    private readonly ILogger _log;
    private SUBCLASSPROC? _subclassProc; // keep the delegate alive
    private uint _callbackMessage;
    private uint _taskbarCreatedMessage;
    private bool _registered;
    private bool _inSetPos;
    private bool _disposed;

    /// <summary>True while a fullscreen app covers this bar's monitor (the window should be hidden).</summary>
    public event Action<bool>? FullscreenChanged;
    /// <summary>Display configuration changed (monitors added/removed, resolution). The owner should re-evaluate its monitors.</summary>
    public event Action? DisplayChanged;
    /// <summary>The bar's pixel rectangle after positioning.</summary>
    public RectPx CurrentRect { get; private set; }

    public AppBarController(nint hwnd, Func<MonitorInfo?> getMonitor, Func<int> getBarHeightDip, ILogger log)
    {
        _hwnd = new HWND(hwnd);
        _getMonitor = getMonitor;
        _getBarHeightDip = getBarHeightDip;
        _log = log;
    }

    public readonly record struct RectPx(int Left, int Top, int Right, int Bottom);

    public void Register()
    {
        _callbackMessage = PInvoke.RegisterWindowMessage("TopBar_AppBarCallback");
        _taskbarCreatedMessage = PInvoke.RegisterWindowMessage("TaskbarCreated");
        _subclassProc = SubclassProc;
        PInvoke.SetWindowSubclass(_hwnd, _subclassProc, 1, 0);
        lock (Active) Active.Add(this);
        RegisterWithShell();
        UpdatePosition();
    }

    private void RegisterWithShell()
    {
        var abd = NewData();
        abd.uCallbackMessage = _callbackMessage;
        PInvoke.SHAppBarMessage(ABM_NEW, &abd);
        _registered = true;
    }

    private APPBARDATA NewData()
    {
        var abd = default(APPBARDATA);
        abd.cbSize = (uint)sizeof(APPBARDATA);
        abd.hWnd = _hwnd;
        return abd;
    }

    /// <summary>Reserves the top edge of the monitor and moves the window there.</summary>
    public void UpdatePosition()
    {
        if (_disposed || _inSetPos) return;
        var monitor = _getMonitor();
        if (monitor is null) return;
        _inSetPos = true;
        try
        {
            var heightPx = (int)Math.Round(_getBarHeightDip() * monitor.DpiScale);
            var abd = NewData();
            abd.uEdge = ABE_TOP;
            abd.rc = new RECT { left = monitor.Left, top = monitor.Top, right = monitor.Right, bottom = monitor.Top + heightPx };
            PInvoke.SHAppBarMessage(ABM_QUERYPOS, &abd);
            abd.rc.bottom = abd.rc.top + heightPx;
            PInvoke.SHAppBarMessage(ABM_SETPOS, &abd);

            var rc = abd.rc;
            PInvoke.SetWindowPos(_hwnd, new HWND(new IntPtr(-1)) /* HWND_TOPMOST */, rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top,
                SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
            CurrentRect = new RectPx(rc.left, rc.top, rc.right, rc.bottom);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "AppBar: positioning failed");
        }
        finally
        {
            _inSetPos = false;
        }
    }

    private LRESULT SubclassProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        try
        {
            // No non-client area at all: the whole window is client area and nothing draws a frame/bevel around it.
            if (msg == WM_NCCALCSIZE || msg == WM_NCPAINT) return new LRESULT(0);

            if (msg == WM_GETMINMAXINFO)
            {
                // A borderless window would otherwise be limited by the system minimum track size (~39 px high).
                var mm = (MINMAXINFO*)lParam.Value;
                mm->ptMinTrackSize = default;
                return new LRESULT(0);
            }

            if (msg == _callbackMessage && _callbackMessage != 0)
            {
                switch ((int)wParam.Value)
                {
                    case ABN_POSCHANGED:
                        UpdatePosition();
                        break;
                    case ABN_FULLSCREENAPP:
                        OnFullscreenApp(lParam.Value != 0);
                        break;
                }
                return new LRESULT(0);
            }

            if (msg == _taskbarCreatedMessage && _taskbarCreatedMessage != 0)
            {
                _log.LogInformation("AppBar: explorer restarted, re-registering");
                if (_registered) { var abd = NewData(); PInvoke.SHAppBarMessage(ABM_REMOVE, &abd); }
                RegisterWithShell();
                UpdatePosition();
                return new LRESULT(0);
            }

            switch (msg)
            {
                case WM_ACTIVATE:
                {
                    var abd = NewData();
                    PInvoke.SHAppBarMessage(ABM_ACTIVATE, &abd);
                    break;
                }
                case WM_WINDOWPOSCHANGED:
                {
                    var abd = NewData();
                    PInvoke.SHAppBarMessage(ABM_WINDOWPOSCHANGED, &abd);
                    break;
                }
                case WM_DISPLAYCHANGE:
                case WM_DPICHANGED:
                {
                    var result = PInvoke.DefSubclassProc(hWnd, msg, wParam, lParam);
                    UpdatePosition();
                    if (msg == WM_DISPLAYCHANGE) DisplayChanged?.Invoke();
                    return result;
                }
                case WM_SETTINGCHANGE when (int)wParam.Value == SPI_SETWORKAREA:
                {
                    var result = PInvoke.DefSubclassProc(hWnd, msg, wParam, lParam);
                    UpdatePosition();
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "AppBar: message handling failed (msg {Msg:X})", msg);
        }
        return PInvoke.DefSubclassProc(hWnd, msg, wParam, lParam);
    }

    private void OnFullscreenApp(bool entering)
    {
        try
        {
            if (!entering) { FullscreenChanged?.Invoke(false); UpdatePosition(); return; }
            var monitor = _getMonitor();
            FullscreenChanged?.Invoke(monitor is not null && ForegroundCovers(monitor));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "AppBar: fullscreen handling failed");
        }
    }

    private bool ForegroundCovers(MonitorInfo m)
    {
        var fg = PInvoke.GetForegroundWindow();
        if (fg == HWND.Null || fg == _hwnd) return false;

        var buffer = new char[256];
        int n;
        fixed (char* p = buffer) n = PInvoke.GetClassName(fg, new PWSTR(p), buffer.Length);
        var cls = new string(buffer, 0, Math.Max(0, n));
        if (cls is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd") return false;

        RECT r;
        if (!PInvoke.GetWindowRect(fg, &r)) return false;
        return r.left <= m.Left && r.top <= m.Top && r.right >= m.Right && r.bottom >= m.Bottom;
    }

    /// <summary>Releases the reserved space. Safe to call repeatedly.</summary>
    public void Remove()
    {
        if (!_registered) return;
        _registered = false;
        try
        {
            var abd = NewData();
            PInvoke.SHAppBarMessage(ABM_REMOVE, &abd);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "AppBar: ABM_REMOVE failed");
        }
    }

    /// <summary>Best-effort removal of all bars (crash / exit paths).</summary>
    public static void RemoveAll()
    {
        AppBarController[] copy;
        lock (Active) copy = Active.ToArray();
        foreach (var c in copy) c.Remove();
    }

    public void Dispose()
    {
        if (_disposed) return;
        Remove();
        _disposed = true;
        lock (Active) Active.Remove(this);
        try { PInvoke.RemoveWindowSubclass(_hwnd, _subclassProc!, 1); } catch { /* window may already be gone */ }
    }
}
