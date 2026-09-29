using System.Globalization;

namespace TopBar.Core.Formatting;

public static class ClockFormats
{
    public static string TimeFormat(bool use24h, bool showSeconds) => (use24h, showSeconds) switch
    {
        (true, false) => "HH:mm",
        (true, true) => "HH:mm:ss",
        (false, false) => "h:mm tt",
        (false, true) => "h:mm:ss tt",
    };

    /// <summary>12h formats use the invariant culture so AM/PM is always present (German cultures may have empty designators).</summary>
    public static string FormatTime(DateTime time, bool use24h, bool showSeconds) =>
        time.ToString(TimeFormat(use24h, showSeconds), CultureInfo.InvariantCulture);

    /// <summary>Time until the next full second (seconds shown) or minute.</summary>
    public static TimeSpan NextTickDelay(DateTime now, bool showSeconds)
    {
        var intoSecond = TimeSpan.FromTicks(now.Ticks % TimeSpan.TicksPerSecond);
        if (showSeconds) return TimeSpan.FromSeconds(1) - intoSecond;
        var intoMinute = TimeSpan.FromTicks(now.Ticks % TimeSpan.TicksPerMinute);
        return TimeSpan.FromMinutes(1) - intoMinute;
    }
}

public static class DateFormats
{
    public const string Default = "ddd, d. MMM";
    public static readonly IReadOnlyList<string> Presets = new[] { "ddd, d. MMM", "dd.MM.yyyy", "dddd, d. MMMM", "yyyy-MM-dd" };

    public static bool TryFormat(string? format, DateTime date, CultureInfo culture, out string result)
    {
        result = string.Empty;
        if (string.IsNullOrWhiteSpace(format)) return false;
        try
        {
            result = date.ToString(format, culture);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
