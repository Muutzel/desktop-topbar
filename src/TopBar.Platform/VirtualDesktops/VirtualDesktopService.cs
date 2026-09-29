using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using TopBar.Core.Services;

namespace TopBar.Platform.VirtualDesktops;

/// <summary>
/// Virtual desktops via VirtualDesktopAccessor.dll. Must be started on the UI thread (creates a message-only window
/// that receives the change hook). All DLL calls happen on the UI thread through <see cref="IUiThread"/>.
/// </summary>
public sealed class VirtualDesktopService : IVirtualDesktopService, IHostedService, IDisposable
{
    private const uint HookMessage = 0x8000 + 42; // WM_APP + 42
    private static readonly TimeSpan PollNormal = TimeSpan.FromMilliseconds(1000);
    private static readonly TimeSpan PollFallback = TimeSpan.FromMilliseconds(250);

    private readonly IUiThread _ui;
    private readonly ILogger<VirtualDesktopService> _log;
    private readonly object _lock = new();

    private IReadOnlyList<DesktopInfo> _desktops = Array.Empty<DesktopInfo>();
    private int _current;
    private HWND _hwnd;
    private SUBCLASSPROC? _proc;
    private Timer? _timer;
    private bool _hookRegistered;
    private bool _running;

    public VirtualDesktopService(IUiThread ui, ILogger<VirtualDesktopService> log)
    {
        _ui = ui;
        _log = log;
    }

    public bool IsAvailable { get; private set; }
    public IReadOnlyList<DesktopInfo> Desktops { get { lock (_lock) return _desktops; } }
    public int CurrentIndex { get { lock (_lock) return _current; } }
    public event EventHandler? Changed;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_running) return Task.CompletedTask;
        _running = true;
        try
        {
            IsAvailable = Probe();
            if (!IsAvailable) return Task.CompletedTask;

            CreateMessageWindow();
            try
            {
                _hookRegistered = _hwnd != HWND.Null && VdaNative.RegisterPostMessageHook(HwndValue, HookMessage) >= 0;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "VirtualDesktops: RegisterPostMessageHook failed");
            }
            if (!_hookRegistered) _log.LogWarning("VirtualDesktops: hook not available, polling every 250 ms");

            Refresh();
            var period = _hookRegistered ? PollNormal : PollFallback;
            _timer = new Timer(_ => _ui.Post(Refresh), null, period, period);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "VirtualDesktops: start failed");
            IsAvailable = false;
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_running) return Task.CompletedTask;
        _running = false;
        _timer?.Dispose();
        _timer = null;
        try { if (_hookRegistered && _hwnd != HWND.Null) VdaNative.UnregisterPostMessageHook(HwndValue); }
        catch (Exception ex) { _log.LogWarning(ex, "VirtualDesktops: unregister failed"); }
        _hookRegistered = false;
        DestroyMessageWindow();
        return Task.CompletedTask;
    }

    private unsafe nint HwndValue => (nint)_hwnd.Value;

    private bool Probe()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, VdaNative.DllName);
            if (!NativeLibrary.TryLoad(path, out _))
            {
                _log.LogError("VirtualDesktops: {Dll} not found next to the executable", VdaNative.DllName);
                return false;
            }
            var count = VdaNative.GetDesktopCount();
            if (count <= 0)
            {
                _log.LogError("VirtualDesktops: probe returned {Count} (Windows build not supported by the DLL?)", count);
                return false;
            }
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or SEHException or BadImageFormatException)
        {
            _log.LogError(ex, "VirtualDesktops: DLL probe failed");
            return false;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "VirtualDesktops: DLL probe failed");
            return false;
        }
    }

    public void SwitchTo(int index)
    {
        if (!IsAvailable) return;
        try { VdaNative.GoToDesktopNumber(index); }
        catch (Exception ex) { _log.LogWarning(ex, "VirtualDesktops: switch failed"); }
    }

    private void Refresh()
    {
        if (!_running || !IsAvailable) return;
        try
        {
            var count = VdaNative.GetDesktopCount();
            var current = VdaNative.GetCurrentDesktopNumber();
            if (count <= 0 || current < 0) return; // transient error

            var list = new List<DesktopInfo>(count);
            for (var i = 0; i < count; i++) list.Add(new DesktopInfo(i, ReadName(i)));

            bool changed;
            lock (_lock)
            {
                changed = current != _current || list.Count != _desktops.Count || !list.SequenceEqual(_desktops);
                if (changed) { _desktops = list; _current = current; }
            }
            if (changed) Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "VirtualDesktops: refresh failed");
        }
    }

    private static string ReadName(int index)
    {
        var buffer = new byte[1024];
        var r = VdaNative.GetDesktopName(index, buffer, (nuint)buffer.Length);
        if (r < 0) return "";
        var len = Array.IndexOf(buffer, (byte)0);
        return len <= 0 ? "" : Encoding.UTF8.GetString(buffer, 0, len);
    }

    private void CreateMessageWindow()
    {
        unsafe
        {
            _hwnd = PInvoke.CreateWindowEx(0, "STATIC", "TopBarVdaListener", 0, 0, 0, 0, 0,
                new HWND(new IntPtr(-3)) /* HWND_MESSAGE */, null, null, null);
        }
        if (_hwnd == HWND.Null) return;
        _proc = SubclassProc;
        PInvoke.SetWindowSubclass(_hwnd, _proc, 1, 0);
    }

    private void DestroyMessageWindow()
    {
        if (_hwnd == HWND.Null) return;
        try
        {
            PInvoke.RemoveWindowSubclass(_hwnd, _proc!, 1);
            PInvoke.DestroyWindow(_hwnd);
        }
        catch (Exception ex) { _log.LogWarning(ex, "VirtualDesktops: destroying listener window failed"); }
        _hwnd = HWND.Null;
    }

    private LRESULT SubclassProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam, nuint id, nuint data)
    {
        if (msg == HookMessage)
        {
            Refresh(); // already on the UI thread
            return new LRESULT(0);
        }
        return PInvoke.DefSubclassProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose() => StopAsync(CancellationToken.None).GetAwaiter().GetResult();
}
