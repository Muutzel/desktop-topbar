namespace TopBar.Core.Weather;

public sealed record WeatherInfo(string Text, string IconTag, string Glyph);

/// <summary>WMO weather code → German text + Fluent System Icons glyph. Codepoints from FluentSystemIcons-Regular.json (20 px, regular).</summary>
public static class WeatherCodes
{
    // Codepoints taken from fonts/FluentSystemIcons-Regular.json (ic_fluent_<tag>_20_regular).
    private static readonly Dictionary<string, int> Codepoints = new()
    {
        ["weather_sunny"] = 63649,
        ["weather_moon"] = 63613,
        ["weather_partly_cloudy_day"] = 63616,
        ["weather_partly_cloudy_night"] = 63619,
        ["weather_cloudy"] = 63598,
        ["weather_fog"] = 63604,
        ["weather_drizzle"] = 60990,
        ["weather_rain"] = 63622,
        ["weather_snow"] = 63634,
        ["weather_thunderstorm"] = 63652,
    };

    public static WeatherInfo Get(int code, bool isDay)
    {
        var (text, day, night) = code switch
        {
            0 => ("Klar", "weather_sunny", "weather_moon"),
            1 or 2 => ("Leicht bewölkt", "weather_partly_cloudy_day", "weather_partly_cloudy_night"),
            3 => ("Bewölkt", "weather_cloudy", "weather_cloudy"),
            45 or 48 => ("Nebel", "weather_fog", "weather_fog"),
            51 or 53 or 55 or 56 or 57 => ("Nieselregen", "weather_drizzle", "weather_drizzle"),
            61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 => ("Regen", "weather_rain", "weather_rain"),
            71 or 73 or 75 or 77 or 85 or 86 => ("Schnee", "weather_snow", "weather_snow"),
            95 or 96 or 99 => ("Gewitter", "weather_thunderstorm", "weather_thunderstorm"),
            _ => ("Unbekannt", "weather_cloudy", "weather_cloudy"),
        };
        var tag = isDay ? day : night;
        return new WeatherInfo(text, tag, char.ConvertFromUtf32(Codepoints[tag]));
    }

    public static int Codepoint(string iconTag) => Codepoints[iconTag];
}
