using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.System.Performance;

namespace TopBar.Platform.Stats;

public sealed class PdhException : Exception
{
    public PdhException(string what, uint status) : base($"PDH error 0x{status:X8}: {what}") => Status = status;
    public uint Status { get; }
}

/// <summary>Thin wrapper around a PDH query (English counter names, independent of the Windows language).</summary>
internal sealed unsafe class PdhQuery : IDisposable
{
    private const uint PDH_MORE_DATA = 0x800007D2;
    private const uint PDH_CSTATUS_VALID_DATA = 0, PDH_CSTATUS_NEW_DATA = 1;

    private readonly PdhCloseQuerySafeHandle _query;

    public PdhQuery()
    {
        var r = (uint)PInvoke.PdhOpenQuery(null, 0, out _query);
        if (r != 0) throw new PdhException("PdhOpenQuery", r);
    }

    public PDH_HCOUNTER AddCounter(string path)
    {
        var r = (uint)PInvoke.PdhAddEnglishCounter(_query, path, 0, out var counter);
        if (r != 0) throw new PdhException(path, r);
        return counter;
    }

    public void RemoveCounter(PDH_HCOUNTER counter) => PInvoke.PdhRemoveCounter(counter);

    public void Collect()
    {
        var r = (uint)PInvoke.PdhCollectQueryData(new PDH_HQUERY(_query.DangerousGetHandle()));
        if (r != 0) throw new PdhException("PdhCollectQueryData", r);
    }

    /// <summary>Formatted value of a single-instance counter, or null while no valid sample exists yet.</summary>
    public double? GetValue(PDH_HCOUNTER counter)
    {
        var r = (uint)PInvoke.PdhGetFormattedCounterValue(counter, PDH_FMT.PDH_FMT_DOUBLE, out _, out var value);
        if (r != 0) return null;
        if (value.CStatus != PDH_CSTATUS_VALID_DATA && value.CStatus != PDH_CSTATUS_NEW_DATA) return null;
        return value.Anonymous.doubleValue;
    }

    /// <summary>All instances of a wildcard counter. Null if no valid data is available yet.</summary>
    public List<(string Instance, double Value)>? GetArray(PDH_HCOUNTER counter)
    {
        uint size = 0;
        var r = (uint)PInvoke.PdhGetFormattedCounterArray(counter, PDH_FMT.PDH_FMT_DOUBLE, ref size, out uint count, out _);
        if (r != PDH_MORE_DATA || size == 0) return null;

        // Pinned (POH) buffer: the item strings point into this same buffer, so it must never move.
        var buffer = GC.AllocateArray<byte>((int)size, pinned: true);
        r = (uint)PInvoke.PdhGetFormattedCounterArray(counter, PDH_FMT.PDH_FMT_DOUBLE, ref size, out count,
            out Unsafe.As<byte, PDH_FMT_COUNTERVALUE_ITEM_W>(ref buffer[0]));
        if (r != 0) return null;

        var items = (PDH_FMT_COUNTERVALUE_ITEM_W*)Unsafe.AsPointer(ref buffer[0]);
        var result = new List<(string, double)>((int)count);
        for (var i = 0; i < count; i++)
        {
            var status = items[i].FmtValue.CStatus;
            if (status != PDH_CSTATUS_VALID_DATA && status != PDH_CSTATUS_NEW_DATA) continue;
            result.Add((items[i].szName.ToString(), items[i].FmtValue.Anonymous.doubleValue));
        }
        return result;
    }

    public void Dispose() => _query.Dispose();
}
