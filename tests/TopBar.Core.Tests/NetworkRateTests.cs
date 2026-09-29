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
    [InlineData(0, "0 KB/s")]
    [InlineData(1023, "0 KB/s")]
    [InlineData(1024, "1 KB/s")]
    [InlineData(80 * 1024, "80 KB/s")]
    [InlineData(1024 * 1024 - 1, "1023 KB/s")]
    [InlineData(1024 * 1024, "1,0 MB/s")]
    [InlineData(1.5 * 1024 * 1024, "1,5 MB/s")]
    [InlineData(10 * 1024 * 1024 - 1, "9,9 MB/s")]
    [InlineData(10 * 1024 * 1024, "10 MB/s")]
    [InlineData(250.7 * 1024 * 1024, "250 MB/s")]
    [InlineData(1073741824.0, "1,0 GB/s")]
    [InlineData(2.5 * 1024 * 1024 * 1024, "2,5 GB/s")]
    public void Format_AllStages(double bps, string expected) =>
        Assert.Equal(expected, NetworkRate.Format(bps, De));

    [Fact]
    public void FormatPair_UsesArrows() =>
        Assert.Equal("↓ 1,5 MB/s  ↑ 80 KB/s", NetworkRate.FormatPair(1.5 * 1024 * 1024, 80 * 1024, De));
}
