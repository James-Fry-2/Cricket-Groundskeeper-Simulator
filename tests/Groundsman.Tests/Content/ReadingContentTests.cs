using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class ReadingContentTests
{
    private static string Shipped() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "readings.json"));

    [Fact]
    public void Shipped_reading_content_parses()
    {
        var readings = ContentParser.ParseReadings(Shipped());

        Assert.Equal(8, readings.MoistureProbeWidth);
        Assert.Equal(0.08, readings.MoistureProbeMissRate);
        Assert.Equal(2.5, readings.FeelJudgementSd);
        Assert.Equal(new[] { "dry", "damp", "wet" }, readings.FeelBands.Select(b => b.Word));
        Assert.Equal(1.0, readings.WidenPerDay);
        Assert.Equal(0.5, readings.WidenPerMmWater);
    }

    [Theory]
    [InlineData("\"width\": 8", "\"width\": 0", "width")]
    [InlineData("\"missRate\": 0.08", "\"missRate\": 1.2", "missRate")]
    [InlineData("\"judgementSd\": 2.5", "\"judgementSd\": -1", "judgementSd")]
    [InlineData("\"from\": 16, \"to\": 26", "\"from\": 17, \"to\": 26", "bands")]
    [InlineData("\"from\": 0, \"to\": 16", "\"from\": 1, \"to\": 16", "bands")]
    [InlineData("\"from\": 26, \"to\": 50", "\"from\": 26, \"to\": 26", "bands")]
    [InlineData("\"word\": \"damp\"", "\"word\": \"\"", "word")]
    [InlineData("\"widenPerDay\": 1.0", "\"widenPerDay\": -1", "widenPerDay")]
    [InlineData("\"widenPerMmWater\": 0.5", "\"widenPerMmWater\": -1", "widenPerMmWater")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var json = Shipped();
        Assert.Contains(original, json);

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseReadings(json.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }
}
