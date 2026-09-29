using TopBar.Core.Stats;

namespace TopBar.Core.Tests;

public class StatFormatTests
{
    [Theory]
    [InlineData(12.4, "12 %")]
    [InlineData(99.6, "100 %")]
    [InlineData(150, "100 %")]
    [InlineData(-3, "0 %")]
    public void Percent(double v, string expected) => Assert.Equal(expected, StatFormat.Percent(v));

    [Fact]
    public void Memory_UsesGermanDecimals() =>
        Assert.Equal("14,2 / 31,9 GB", StatFormat.Memory((ulong)(14.2 * 1024 * 1024 * 1024), (ulong)(31.9 * 1024 * 1024 * 1024)));

    [Theory]
    [InlineData(50, StatFormat.Level.Normal)]
    [InlineData(85, StatFormat.Level.Warning)]
    [InlineData(94.9, StatFormat.Level.Warning)]
    [InlineData(95, StatFormat.Level.Critical)]
    public void Level(double v, StatFormat.Level expected) => Assert.Equal(expected, StatFormat.GetLevel(v, 85, 95));
}
