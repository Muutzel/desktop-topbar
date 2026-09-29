using System.Diagnostics;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using Windows.Win32;
using Windows.Win32.System.Performance;
using Windows.Win32.System.SystemInformation;
using TopBar.Core.Services;
using TopBar.Core.Stats;

namespace TopBar.Platform.Stats;

/// <summary>Samples CPU, GPU, RAM and network on a background loop. Values that are not available stay null / hidden.</summary>
public sealed class SystemStatsService : LoopService, ISystemStatsService
{
    private const string CpuPath = @"\Processor Information(_Total)\% Processor Utility";
    private const string CpuFallbackPath = @"\Processor(_Total)\% Processor Time";
    private const string GpuPath = @"\GPU Engine(*)\Utilization Percentage";
    private static readonly TimeSpan GpuRebuildInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan NicRefreshInterval = TimeSpan.FromSeconds(10);
    private static readonly string[] VirtualMarkers = { "Hyper-V", "VirtualBox", "VMware", "WSL", "vEthernet", "TAP" };

    private readonly ISettingsService _settings;
    private volatile SystemStats _current = SystemStats.Empty;
    private volatile IReadOnlyList<(string Id, string Name)> _nicList = Array.Empty<(string, string)>();

    public SystemStatsService(ISettingsService settings, ILogger<SystemStatsService> log) : base(log) => _settings = settings;

    protected override string ModuleName => "SystemStats";

    public SystemStats Current => _current;
    public IReadOnlyList<(string Id, string Name)> NetworkInterfaces => _nicList;
    public event EventHandler? Changed;

    protected override async Task RunAsync(CancellationToken ct)
    {
        PdhQuery? query = null;
        PDH_HCOUNTER? cpuCounter = null;
        PDH_HCOUNTER? gpuCounter = null;
        double? lastGpu = null;
        var lastGpuRebuild = DateTime.MinValue;

        try { query = new PdhQuery(); }
        catch (Exception ex) { LogOnce("pdh-open", ex, "PDH not available, CPU/GPU hidden"); }

        if (query is not null)
        {
            try { cpuCounter = query.AddCounter(CpuPath); }
            catch (Exception)
            {
                try { cpuCounter = query.AddCounter(CpuFallbackPath); }
                catch (Exception ex2) { LogOnce("cpu-add", ex2, "CPU counter not available"); }
            }
        }

        var nicState = new Dictionary<string, (ulong Rx, ulong Tx)>();
        var nics = new List<NetworkInterface>();
        var lastNicRefresh = DateTime.MinValue;
        var stopwatch = Stopwatch.StartNew();
        var lastSample = TimeSpan.Zero;

        try
        {
            // Priming sample: rate counters need two samples.
            try { query?.Collect(); } catch (Exception ex) { LogOnce("collect", ex, "PdhCollectQueryData failed"); }

            while (!ct.IsCancellationRequested)
            {
                var interval = TimeSpan.FromMilliseconds(_settings.Current.Stats.IntervalMs);
                await Task.Delay(interval, ct).ConfigureAwait(false);

                var now = stopwatch.Elapsed;
                var elapsed = now - lastSample;
                lastSample = now;
                var stats = _settings.Current.Stats;

                // ---- PDH: CPU + GPU ----
                double? cpu = null;
                PDH_HCOUNTER? pendingGpu = null;
                if (query is not null)
                {
                    if (stats.ShowGpu && DateTime.UtcNow - lastGpuRebuild >= GpuRebuildInterval)
                    {
                        lastGpuRebuild = DateTime.UtcNow;
                        try { pendingGpu = query.AddCounter(GpuPath); ClearLogged("gpu-add"); }
                        catch (Exception ex) { LogOnce("gpu-add", ex, "GPU counters not available, GPU hidden"); }
                    }

                    try { query.Collect(); }
                    catch (Exception ex) { LogOnce("collect", ex, "PdhCollectQueryData failed"); }

                    if (cpuCounter is { } cc)
                    {
                        try { cpu = query.GetValue(cc) is { } v ? Math.Clamp(v, 0, 100) : null; }
                        catch (Exception ex) { LogOnce("cpu-read", ex, "CPU read failed"); }
                    }

                    if (gpuCounter is { } gc)
                    {
                        try
                        {
                            var array = query.GetArray(gc);
                            if (array is not null) lastGpu = GpuAggregator.Compute(array) ?? lastGpu;
                        }
                        catch (Exception ex) { LogOnce("gpu-read", ex, "GPU read failed"); }
                    }

                    // Swap in the freshly added counter only after the old one was read; it has 2 samples from the next round on.
                    if (pendingGpu is { } pg)
                    {
                        try { if (gpuCounter is { } old) query.RemoveCounter(old); } catch { /* ignore */ }
                        gpuCounter = pg;
                    }
                    if (!stats.ShowGpu && gpuCounter is { } g2)
                    {
                        try { query.RemoveCounter(g2); } catch { /* ignore */ }
                        gpuCounter = null;
                        lastGpu = null;
                    }
                }

                // ---- RAM ----
                double ramPercent = 0; ulong ramUsed = 0, ramTotal = 0;
                try { (ramPercent, ramUsed, ramTotal) = ReadMemory(); }
                catch (Exception ex) { LogOnce("ram", ex, "RAM read failed"); }

                // ---- Network ----
                double down = 0, up = 0; var nicNames = "";
                try
                {
                    if (DateTime.UtcNow - lastNicRefresh >= NicRefreshInterval)
                    {
                        lastNicRefresh = DateTime.UtcNow;
                        nics = NetworkInterface.GetAllNetworkInterfaces()
                            .Where(n => n.OperationalStatus == OperationalStatus.Up).ToList();
                        _nicList = nics.Where(IsConsidered).Select(n => (n.Id, n.Name)).ToList();
                    }

                    var wanted = stats.NetworkInterfaceId is { } id
                        ? nics.Where(n => n.Id == id).ToList()
                        : nics.Where(IsConsidered).ToList();
                    var seen = new HashSet<string>();
                    foreach (var nic in wanted)
                    {
                        var s = nic.GetIPStatistics();
                        var rx = (ulong)Math.Max(0, s.BytesReceived);
                        var tx = (ulong)Math.Max(0, s.BytesSent);
                        seen.Add(nic.Id);
                        if (nicState.TryGetValue(nic.Id, out var prev))
                        {
                            down += NetworkRate.BytesPerSecond(prev.Rx, rx, elapsed);
                            up += NetworkRate.BytesPerSecond(prev.Tx, tx, elapsed);
                        }
                        nicState[nic.Id] = (rx, tx);
                    }
                    foreach (var stale in nicState.Keys.Where(k => !seen.Contains(k)).ToList()) nicState.Remove(stale);
                    nicNames = string.Join(", ", wanted.Select(n => n.Name));
                }
                catch (Exception ex) { LogOnce("net", ex, "Network read failed"); }

                _current = new SystemStats(cpu, lastGpu, ramPercent, ramUsed, ramTotal, down, up, nicNames);
                try { Changed?.Invoke(this, EventArgs.Empty); }
                catch (Exception ex) { LogOnce("changed", ex, "Changed handler failed"); }
            }
        }
        finally
        {
            query?.Dispose();
        }
    }

    private static unsafe (double Percent, ulong Used, ulong Total) ReadMemory()
    {
        MEMORYSTATUSEX ms = default;
        ms.dwLength = (uint)sizeof(MEMORYSTATUSEX);
        if (!PInvoke.GlobalMemoryStatusEx(&ms)) return (0, 0, 0);
        var used = ms.ullTotalPhys - ms.ullAvailPhys;
        return (ms.ullTotalPhys == 0 ? 0 : used * 100.0 / ms.ullTotalPhys, used, ms.ullTotalPhys);
    }

    private static bool IsConsidered(NetworkInterface n)
    {
        if (n.OperationalStatus != OperationalStatus.Up) return false;
        if (n.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) return false;
        var text = n.Description + " " + n.Name;
        return !VirtualMarkers.Any(m => text.Contains(m, StringComparison.OrdinalIgnoreCase));
    }
}
