using System.Globalization;
using TopBar.Core.Services;

namespace TopBar.Core.Weather;

public static class WeatherFormatter
{
    private static readonly CultureInfo De = new("de-DE");

    public static string Temperature(double value, bool fahrenheit) =>
        $"{Math.Round(value, MidpointRounding.AwayFromZero).ToString("0", De)} °{(fahrenheit ? "F" : "C")}";

    public static string Tooltip(WeatherSnapshot s, bool fahrenheit)
    {
        var info = WeatherCodes.Get(s.WeatherCode, s.IsDay);
        var lines = new List<string>
        {
            info.Text,
            $"Gefühlt {Temperature(s.ApparentTemperature, fahrenheit)}",
            $"Min {Temperature(s.TempMin, fahrenheit)} / Max {Temperature(s.TempMax, fahrenheit)}",
            $"Regen {s.PrecipitationProbability} %",
            $"Wind {Math.Round(s.WindSpeed, MidpointRounding.AwayFromZero).ToString("0", De)} km/h",
        };
        if (s.IsStale) lines.Add($"Stand: {s.FetchedAt.ToLocalTime():HH:mm}");
        return string.Join("\n", lines);
    }
}
