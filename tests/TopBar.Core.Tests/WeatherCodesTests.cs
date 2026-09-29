using TopBar.Core.Weather;

namespace TopBar.Core.Tests;

public class WeatherCodesTests
{
    [Theory]
    [InlineData(0, "Klar", "weather_sunny", "weather_moon")]
    [InlineData(1, "Leicht bewölkt", "weather_partly_cloudy_day", "weather_partly_cloudy_night")]
    [InlineData(2, "Leicht bewölkt", "weather_partly_cloudy_day", "weather_partly_cloudy_night")]
    [InlineData(3, "Bewölkt", "weather_cloudy", "weather_cloudy")]
    [InlineData(45, "Nebel", "weather_fog", "weather_fog")]
    [InlineData(48, "Nebel", "weather_fog", "weather_fog")]
    [InlineData(51, "Nieselregen", "weather_drizzle", "weather_drizzle")]
    [InlineData(53, "Nieselregen", "weather_drizzle", "weather_drizzle")]
    [InlineData(55, "Nieselregen", "weather_drizzle", "weather_drizzle")]
    [InlineData(56, "Nieselregen", "weather_drizzle", "weather_drizzle")]
    [InlineData(57, "Nieselregen", "weather_drizzle", "weather_drizzle")]
    [InlineData(61, "Regen", "weather_rain", "weather_rain")]
    [InlineData(63, "Regen", "weather_rain", "weather_rain")]
    [InlineData(65, "Regen", "weather_rain", "weather_rain")]
    [InlineData(66, "Regen", "weather_rain", "weather_rain")]
    [InlineData(67, "Regen", "weather_rain", "weather_rain")]
    [InlineData(80, "Regen", "weather_rain", "weather_rain")]
    [InlineData(81, "Regen", "weather_rain", "weather_rain")]
    [InlineData(82, "Regen", "weather_rain", "weather_rain")]
    [InlineData(71, "Schnee", "weather_snow", "weather_snow")]
    [InlineData(73, "Schnee", "weather_snow", "weather_snow")]
    [InlineData(75, "Schnee", "weather_snow", "weather_snow")]
    [InlineData(77, "Schnee", "weather_snow", "weather_snow")]
    [InlineData(85, "Schnee", "weather_snow", "weather_snow")]
    [InlineData(86, "Schnee", "weather_snow", "weather_snow")]
    [InlineData(95, "Gewitter", "weather_thunderstorm", "weather_thunderstorm")]
    [InlineData(96, "Gewitter", "weather_thunderstorm", "weather_thunderstorm")]
    [InlineData(99, "Gewitter", "weather_thunderstorm", "weather_thunderstorm")]
    public void Table_DayAndNight(int code, string text, string day, string night)
    {
        var d = WeatherCodes.Get(code, true);
        var n = WeatherCodes.Get(code, false);
        Assert.Equal(text, d.Text);
        Assert.Equal(day, d.IconTag);
        Assert.Equal(night, n.IconTag);
        Assert.False(string.IsNullOrEmpty(d.Glyph));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(1000)]
    public void UnknownCode_FallsBack(int code)
    {
        var i = WeatherCodes.Get(code, true);
        Assert.Equal("Unbekannt", i.Text);
        Assert.Equal("weather_cloudy", i.IconTag);
    }

    [Fact]
    public void Glyph_IsCodepointFromFont()
    {
        Assert.Equal(char.ConvertFromUtf32(63649), WeatherCodes.Get(0, true).Glyph);
    }
}
