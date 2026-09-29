using System.Text.RegularExpressions;

namespace TopBar.Core.Stats;

/// <summary>
/// Aggregates PDH "GPU Engine(*)\Utilization Percentage" instances like the Task Manager:
/// sum per (luid, engtype) over all processes, then max over engtypes per luid, then max over luids.
/// </summary>
public static partial class GpuAggregator
{
    [GeneratedRegex(@"luid_(?<luid>0x[0-9A-Fa-f]+_0x[0-9A-Fa-f]+).*?engtype_(?<type>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex InstanceRegex();

    /// <summary>Per luid the utilisation (max over engine types). Empty when no instance could be parsed.</summary>
    public static Dictionary<string, double> PerAdapter(IEnumerable<(string Instance, double Value)> samples)
    {
        var sums = new Dictionary<(string Luid, string Type), double>();
        foreach (var (instance, value) in samples)
        {
            var m = InstanceRegex().Match(instance);
            if (!m.Success || double.IsNaN(value)) continue;
            var key = (m.Groups["luid"].Value.ToLowerInvariant(), m.Groups["type"].Value);
            sums[key] = sums.GetValueOrDefault(key) + value;
        }

        var result = new Dictionary<string, double>();
        foreach (var ((luid, _), sum) in sums)
            result[luid] = Math.Max(result.GetValueOrDefault(luid), sum);
        return result;
    }

    /// <summary>Utilisation of the busiest GPU, clamped to 0–100; null when there is no data.</summary>
    public static double? Compute(IEnumerable<(string Instance, double Value)> samples)
    {
        var perAdapter = PerAdapter(samples);
        if (perAdapter.Count == 0) return null;
        return Math.Clamp(perAdapter.Values.Max(), 0, 100);
    }
}
