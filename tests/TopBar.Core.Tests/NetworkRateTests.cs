using System.Globalization;
using TopBar.Core.Stats;

namespace TopBar.Core.Tests;

public class NetworkRateTests
{
    private static readonly CultureInfo De = new("de-DE");

    [Fact]
    public void Delta_IsDividedByElapsedTime() =>
        Assert.Equal(1000, NetworkRate.BytesPerSecond(1000, 3000, TimeSpan.FromSeconds(2)));

    [Fact]
    public void NegativeDelta_IsZero() =>
        Assert.Equal(0, NetworkRate.BytesPerSecond(5000, 100, TimeSpan.FromSeconds(1)));

    [Fact]
    public void ZeroElapsed_IsZero() =>
        Assert.Equal(0, NetworkRate.BytesPerSecond(0, 100, TimeSpan.Zero));

    [Theory]
    [InlineData(0, "\u2007\u20070 KB/s")]
    [InlineData(1023, "\u2007\u20070 KB/s")]
    [InlineData(1024, "\u2007\u20071 KB/s")]
    [InlineData(80 * 1024, "\u200780 KB/s")]
    [InlineData(999 * 1024, "999 KB/s")]
    [InlineData(1000 * 1024, "0,9 MB/s")]
    [InlineData(1024 * 1024, "1,0 MB/s")]
    [InlineData(1.5 * 1024 * 1024, "1,5 MB/s")]
    [InlineData(10 * 1024 * 1024 - 1, "9,9 MB/s")]
    [InlineData(10 * 1024 * 1024, "\u200710 MB/s")]
    [InlineData(250.7 * 1024 * 1024, "250 MB/s")]
    [InlineData(1000.0 * 1024 * 1024, "0,9 GB/s")]
    [InlineData(1073741824.0, "1,0 GB/s")]
    [InlineData(2.5 * 1024 * 1024 * 1024, "2,5 GB/s")]
    [InlineData(25.0 * 1024 * 1024 * 1024, "\u200725 GB/s")]
    public void Format_AllStages(double bps, string expected) =>
        Assert.Equal(expected, NetworkRate.Format(bps, De));

    [Fact]
    public void FormatPair_UsesArrows() =>
        Assert.Equal("↓ 1,5 MB/s  ↑ \u200780 KB/s", NetworkRate.FormatPair(1.5 * 1024 * 1024, 80 * 1024, De));

    private static readonly NetworkDisplay Auto = new(true, NetworkScale.Auto);

    [Theory]
    [InlineData(0, "\u2007\u20070 kbit/s")]
    [InlineData(125, "\u2007\u20071 kbit/s")]             // 1000 bit/s
    [InlineData(12_500, "100 kbit/s")]
    [InlineData(124_999, "999 kbit/s")]
    [InlineData(125_000, "1,0 Mbit/s")]                     // 1 Mbit/s
    [InlineData(1_250_000, "\u200710 Mbit/s")]             // 10 Mbit/s
    [InlineData(12_500_000, "100 Mbit/s")]
    [InlineData(125_000_000, "1,0 Gbit/s")]                 // 1 Gbit/s
    public void Format_Bits_Auto(double bytesPerSecond, string expected) =>
        Assert.Equal(expected, NetworkRate.Format(bytesPerSecond, Auto, De));

    [Fact]
    public void Format_Bits_FixedKilo_NeverSwitches() =>
        Assert.Equal("10000 kbit/s", NetworkRate.Format(1_250_000, new NetworkDisplay(true, NetworkScale.Kilo), De));

    [Fact]
    public void Format_Bits_FixedMega() =>
        Assert.Equal("0,1 Mbit/s", NetworkRate.Format(15_000, new NetworkDisplay(true, NetworkScale.Mega), De));

    [Fact]
    public void Format_Bytes_FixedMega() =>
        Assert.Equal("0,0 MB/s", NetworkRate.Format(0, new NetworkDisplay(false, NetworkScale.Mega), De));

    [Theory]
    [InlineData(0, "0", "KB/s")]
    [InlineData(80 * 1024, "80", "KB/s")]
    [InlineData(1.5 * 1024 * 1024, "1,5", "MB/s")]
    [InlineData(250.7 * 1024 * 1024, "250", "MB/s")]
    public void Parts_AreUnpadded(double bps, string number, string unit) =>
        Assert.Equal((number, unit), NetworkRate.Parts(bps, NetworkRate.DefaultDisplay, De));

    [Fact]
    public void UnitSamples_FollowScale()
    {
        Assert.Equal(new[] { "kbit/s", "Mbit/s", "Gbit/s" }, NetworkRate.UnitSamples(new NetworkDisplay(true, NetworkScale.Auto)));
        Assert.Equal(new[] { "KB/s" }, NetworkRate.UnitSamples(new NetworkDisplay(false, NetworkScale.Kilo)));
        Assert.Equal(new[] { "Mbit/s" }, NetworkRate.UnitSamples(new NetworkDisplay(true, NetworkScale.Mega)));
    }

    [Theory]
    [InlineData("Bits", "Kilo", true, NetworkScale.Kilo)]
    [InlineData("bytes", "auto", false, NetworkScale.Auto)]
    [InlineData(null, "kaputt", false, NetworkScale.Auto)]
    public void NetworkDisplay_From(string? unit, string? scale, bool bits, NetworkScale expectedScale) =>
        Assert.Equal(new NetworkDisplay(bits, expectedScale), NetworkDisplay.From(unit, scale));
}
