using System.Globalization;

namespace TopBar.Core.Stats;

public static class StatFormat
{
    private static readonly CultureInfo De = new("de-DE");

    public static string Percent(double value) => $"{Math.Round(Math.Clamp(value, 0, 100), MidpointRounding.AwayFromZero).ToString("0", De)} %";

    public static string Memory(ulong usedBytes, ulong totalBytes)
    {
        const double GB = 1024d * 1024 * 1024;
        return $"{(usedBytes / GB).ToString("0.0", De)} / {(totalBytes / GB).ToString("0.0", De)} GB";
    }

    public enum Level { Normal, Warning, Critical }

    public static Level GetLevel(double value, int warn, int critical) =>
        value >= critical ? Level.Critical : value >= warn ? Level.Warning : Level.Normal;
}
