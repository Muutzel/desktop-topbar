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

    /// <summary>Digit-wide space (U+2007): pads the number to 3 characters without changing the text width.</summary>
    private const char Pad = '\u2007';

    /// <summary>Default display: bytes per second, automatic scale (KB/s, MB/s, GB/s).</summary>
    public static readonly NetworkDisplay DefaultDisplay = new(false, NetworkScale.Auto);

    /// <summary>
    /// Bytes: base 1024 (KB/s, MB/s, GB/s). Bits: base 1000 (kbit/s, Mbit/s, Gbit/s).
    /// The number is at least 3 characters wide (left-padded with figure spaces); in automatic scale the unit
    /// switches at 1000, so it never needs 4 digits. Values are truncated, not rounded.
    /// </summary>
    public static string Format(double bytesPerSecond, CultureInfo? culture = null) =>
        Format(bytesPerSecond, DefaultDisplay, culture);

    public static string Format(double bytesPerSecond, NetworkDisplay display, CultureInfo? culture = null)
    {
        var (number, unit) = Parts(bytesPerSecond, display, culture);
        return $"{number.PadLeft(3, Pad)} {unit}";
    }

    /// <summary>Number (unpadded, German decimal comma) and unit, for layouts with separate columns.</summary>
    public static (string Number, string Unit) Parts(double bytesPerSecond, NetworkDisplay display, CultureInfo? culture = null)
    {
        var c = culture ?? De;
        var value = Math.Max(bytesPerSecond, 0) * (display.Bits ? 8 : 1);
        var step = display.Bits ? 1000d : 1024d;
        var units = Units(display);

        var level = display.Scale switch { NetworkScale.Kilo => 1, NetworkScale.Mega => 2, _ => 1 };
        if (display.Scale == NetworkScale.Auto)
            while (level < 3 && value >= 1000 * Math.Pow(step, level)) level++;

        var scaled = value / Math.Pow(step, level);
        var number = level >= 2 && scaled < 10 ? OneDecimal(scaled, c) : Whole(scaled, c);
        return (number, units[level - 1]);
    }

    /// <summary>All units the display can show (used to reserve the width of the unit column).</summary>
    public static string[] UnitSamples(NetworkDisplay display)
    {
        var units = Units(display);
        return display.Scale switch { NetworkScale.Kilo => [units[0]], NetworkScale.Mega => [units[1]], _ => units };
    }

    private static string[] Units(NetworkDisplay display) =>
        display.Bits ? ["kbit/s", "Mbit/s", "Gbit/s"] : ["KB/s", "MB/s", "GB/s"];

    public static string FormatPair(double down, double up, CultureInfo? culture = null) =>
        FormatPair(down, up, DefaultDisplay, culture);

    public static string FormatPair(double down, double up, NetworkDisplay display, CultureInfo? culture = null) =>
        $"↓ {Format(down, display, culture)}  ↑ {Format(up, display, culture)}";

    private static string Whole(double v, CultureInfo c) => Math.Floor(v).ToString("0", c);

    private static string OneDecimal(double v, CultureInfo c) => (Math.Floor(v * 10 + 1e-9) / 10).ToString("0.0", c);
}

public enum NetworkScale { Auto, Kilo, Mega }

/// <summary>How the network rate is shown: bytes or bits per second, automatic or fixed kilo/mega scale.</summary>
public readonly record struct NetworkDisplay(bool Bits, NetworkScale Scale)
{
    /// <summary>Builds the display from the persisted (already normalized) setting strings.</summary>
    public static NetworkDisplay From(string? unit, string? scale) => new(
        string.Equals(unit, "Bits", StringComparison.OrdinalIgnoreCase),
        Enum.TryParse<NetworkScale>(scale, true, out var s) ? s : NetworkScale.Auto);
}
