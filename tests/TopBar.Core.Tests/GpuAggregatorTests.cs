using TopBar.Core.Stats;

namespace TopBar.Core.Tests;

public class GpuAggregatorTests
{
    private static string Inst(int pid, string luid, int eng, string type) =>
        $"pid_{pid}_luid_{luid}_phys_0_eng_{eng}_engtype_{type}";

    private const string Gpu0 = "0x00000000_0x0000C3A1";
    private const string Gpu1 = "0x00000000_0x0000D4B2";

    [Fact]
    public void NoSamples_IsNull() => Assert.Null(GpuAggregator.Compute(Array.Empty<(string, double)>()));

    [Fact]
    public void UnparsableInstances_AreIgnored() =>
        Assert.Null(GpuAggregator.Compute(new[] { ("garbage", 50.0) }));

    [Fact]
    public void SumsProcessesPerEngineType()
    {
        var v = GpuAggregator.Compute(new[]
        {
            (Inst(1, Gpu0, 0, "3D"), 10.0),
            (Inst(2, Gpu0, 0, "3D"), 15.0),
        });
        Assert.Equal(25.0, v);
    }

    [Fact]
    public void TakesMaxOverEngineTypes_NotSum()
    {
        var v = GpuAggregator.Compute(new[]
        {
            (Inst(1, Gpu0, 0, "3D"), 30.0),
            (Inst(1, Gpu0, 1, "VideoDecode"), 45.0),
            (Inst(2, Gpu0, 1, "VideoDecode"), 5.0),
        });
        Assert.Equal(50.0, v);
    }

    [Fact]
    public void MultipleGpus_ReturnsBusiest()
    {
        var samples = new[]
        {
            (Inst(1, Gpu0, 0, "3D"), 20.0),
            (Inst(1, Gpu1, 0, "3D"), 70.0),
        };
        var per = GpuAggregator.PerAdapter(samples);
        Assert.Equal(2, per.Count);
        Assert.Equal(70.0, GpuAggregator.Compute(samples));
    }

    [Fact]
    public void Result_IsClampedTo100() =>
        Assert.Equal(100.0, GpuAggregator.Compute(new[] { (Inst(1, Gpu0, 0, "3D"), 80.0), (Inst(2, Gpu0, 0, "3D"), 80.0) }));
}
