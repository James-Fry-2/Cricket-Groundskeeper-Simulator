using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class ForecastContentTests
{
    private static string Shipped() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "forecast.json"));

    [Fact]
    public void Shipped_forecast_content_parses()
    {
        var forecast = ContentParser.ParseForecast(Shipped());

        Assert.Equal(7, forecast.Days);
        Assert.Equal(1.5, forecast.RangeSpreads);
        Assert.Equal(1.5, forecast.RainErrorSd);
        Assert.Equal(0.3, forecast.MaxTemperatureGrowthPerDay);
    }

    [Theory]
    [InlineData("\"days\": 7", "\"days\": 0", "days")]
    [InlineData("\"rangeSpreads\": 1.5", "\"rangeSpreads\": 0", "rangeSpreads")]
    [InlineData("\"errorSd\": 1.5", "\"errorSd\": -1", "rain.errorSd")]
    [InlineData("\"growthPerDay\": 0.3", "\"growthPerDay\": -0.1", "maxTemperature.growthPerDay")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var json = Shipped();
        Assert.Contains(original, json);

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseForecast(json.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }
}
