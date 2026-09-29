using System.Globalization;
using TopBar.Core.Formatting;

namespace TopBar.Core.Tests;

public class FormattingTests
{
    private static readonly DateTime T = new(2026, 9, 29, 14, 32, 5);

    [Theory]
    [InlineData(true, false, "HH:mm")]
    [InlineData(true, true, "HH:mm:ss")]
    [InlineData(false, false, "h:mm tt")]
    [InlineData(false, true, "h:mm:ss tt")]
    public void TimeFormat_Strings(bool h24, bool sec, string expected) =>
        Assert.Equal(expected, ClockFormats.TimeFormat(h24, sec));

    [Fact]
    public void FormatTime_AllVariants()
    {
        Assert.Equal("14:32", ClockFormats.FormatTime(T, true, false));
        Assert.Equal("14:32:05", ClockFormats.FormatTime(T, true, true));
        Assert.Equal("2:32 PM", ClockFormats.FormatTime(T, false, false));
        Assert.Equal("2:32:05 PM", ClockFormats.FormatTime(T, false, true));
    }

    [Fact]
    public void NextTickDelay_Minute()
    {
        var now = new DateTime(2026, 1, 1, 10, 0, 45, 250);
        Assert.Equal(TimeSpan.FromMilliseconds(14750), ClockFormats.NextTickDelay(now, false));
    }

    [Fact]
    public void NextTickDelay_Second()
    {
        var now = new DateTime(2026, 1, 1, 10, 0, 45, 250);
        Assert.Equal(TimeSpan.FromMilliseconds(750), ClockFormats.NextTickDelay(now, true));
    }

    [Fact]
    public void NextTickDelay_ExactBoundary_WaitsFullPeriod()
    {
        var now = new DateTime(2026, 1, 1, 10, 0, 0, 0);
        Assert.Equal(TimeSpan.FromMinutes(1), ClockFormats.NextTickDelay(now, false));
    }

    [Theory]
    [InlineData("ddd, d. MMM", "Di., 29. Sep.")]
    [InlineData("dd.MM.yyyy", "29.09.2026")]
    [InlineData("dddd, d. MMMM", "Dienstag, 29. September")]
    [InlineData("yyyy-MM-dd", "2026-09-29")]
    public void DateFormat_Presets(string fmt, string expected)
    {
        Assert.True(DateFormats.TryFormat(fmt, T, new CultureInfo("de-DE"), out var result));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("%")]
    [InlineData("y y y \\")]
    public void DateFormat_Invalid(string fmt) =>
        Assert.False(DateFormats.TryFormat(fmt, T, CultureInfo.InvariantCulture, out _));
}
