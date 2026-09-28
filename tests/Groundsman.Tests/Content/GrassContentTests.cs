using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class GrassContentTests
{
    private static string Shipped() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "grass.json"));

    [Fact]
    public void Shipped_grass_content_parses()
    {
        var grass = ContentParser.ParseGrass(Shipped());

        Assert.Equal(85, grass.StartingCover);
        Assert.Equal(15, grass.StartingHeightMm);
        Assert.Equal(18, grass.OptimumTemperature);
        Assert.Equal(1.2, grass.HeightPerDayMm);
        Assert.Equal(95, grass.MaxCover);
        Assert.Equal(0.34, grass.ScalpShare);
    }

    [Theory]
    [InlineData("\"cover\": 85", "\"cover\": 99", "cover")]
    [InlineData("\"optimum\": 18", "\"optimum\": 4", "temperature")]
    [InlineData("\"max\": 30", "\"max\": 15", "temperature")]
    [InlineData("\"heightPerDayMm\": 1.2", "\"heightPerDayMm\": -1", "heightPerDayMm")]
    [InlineData("\"maxCover\": 95", "\"maxCover\": 101", "maxCover")]
    [InlineData("\"waterBelow\": 0.15", "\"waterBelow\": 1.5", "waterBelow")]
    [InlineData("\"minHeightMm\": 3", "\"minHeightMm\": 0", "minHeightMm")]
    [InlineData("\"scalpShare\": 0.34", "\"scalpShare\": 1", "scalpShare")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var json = Shipped();
        Assert.Contains(original, json);

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseGrass(json.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }
}
