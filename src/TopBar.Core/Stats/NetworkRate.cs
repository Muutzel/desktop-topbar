using System.Globalization;

namespace TopBar.Core.Stats;

public static class NetworkRate
{
    private static readonly CultureInfo De = new("de-DE");

    /// <summary>Bytes per second between two counter values. A negative delta (adapter reset) counts as 0.</summary>
    public static double BytesPerSecond(ulong previous, ulong current, TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero || current < previous) return 0;
        return (current - previous) / elapsed.TotalSeconds;
    }

    /// <summary>Base 1024, German decimal comma. Values are truncated, not rounded, so "1.024 KB/s" never appears.</summary>
    public static string Format(double bytesPerSecond, CultureInfo? culture = null)
    {
        var c = culture ?? De;
        const double KB = 1024, MB = KB * 1024, GB = MB * 1024;
        if (bytesPerSecond < KB) return "0 KB/s";
        if (bytesPerSecond < MB) return $"{Math.Floor(bytesPerSecond / KB).ToString("0", c)} KB/s";
        if (bytesPerSecond < 10 * MB) return $"{OneDecimal(bytesPerSecond / MB, c)} MB/s";
        if (bytesPerSecond < GB) return $"{Math.Floor(bytesPerSecond / MB).ToString("0", c)} MB/s";
        return $"{OneDecimal(bytesPerSecond / GB, c)} GB/s";
    }

    public static string FormatPair(double down, double up, CultureInfo? culture = null) =>
        $"↓ {Format(down, culture)}  ↑ {Format(up, culture)}";

    private static string OneDecimal(double v, CultureInfo c) => (Math.Floor(v * 10 + 1e-9) / 10).ToString("0.0", c);
}
