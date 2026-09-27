using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class ClimateContentTests
{
    private static string Shipped() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "climate.json"));

    [Fact]
    public void Shipped_climate_content_parses_with_twelve_months()
    {
        var climate = ContentParser.ParseClimate(Shipped());

        Assert.Equal(12, climate.Months.Count);
        Assert.Equal(1, climate.ForMonth(1).Month);
        Assert.Equal(12, climate.ForMonth(12).Month);
    }

    [Fact]
    public void Parses_the_shared_fields()
    {
        var climate = ContentParser.ParseClimate(Shipped());

        Assert.Equal(1.0, climate.RainDayThresholdMm);
        Assert.Equal(0.35, climate.WetDayPersistence);
        Assert.Equal(2, climate.RainSpellMinHours);
        Assert.Equal(8, climate.RainSpellMaxHours);
    }

    [Theory]
    [InlineData("\"wetDayPersistence\": 0.35", "\"wetDayPersistence\": 1.0", "wetDayPersistence")]
    [InlineData("\"temperatureAnomalyPersistence\": 0.7", "\"temperatureAnomalyPersistence\": -0.1", "temperatureAnomalyPersistence")]
    [InlineData("\"min\": 2", "\"min\": 9", "rainSpellHours")]
    [InlineData("\"max\": 8", "\"max\": 25", "rainSpellHours")]
    [InlineData("\"rainDays\": 12, \"rainTotalMm\": 62", "\"rainDays\": 32, \"rainTotalMm\": 62", "rainDays")]
    [InlineData("\"rainDays\": 12, \"rainTotalMm\": 62", "\"rainDays\": 12, \"rainTotalMm\": 10", "rainTotalMm")]
    [InlineData("\"sunshineHours\": 55,", "\"sunshineHours\": 300,", "sunshineHours")]
    [InlineData("\"daylightHours\": 8.3", "\"daylightHours\": 25", "daylightHours")]
    [InlineData("\"month\": 2,", "\"month\": 3,", "month")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var json = Shipped();
        Assert.Contains(original, json);

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseClimate(ReplaceFirst(json, original, replacement)));

        Assert.Contains(field, error.Message);
    }

    private static string ReplaceFirst(string text, string original, string replacement)
    {
        var index = text.IndexOf(original, StringComparison.Ordinal);
        return text.Substring(0, index) + replacement + text.Substring(index + original.Length);
    }
}
