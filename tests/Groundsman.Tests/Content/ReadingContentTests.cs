using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class ReadingContentTests
{
    [Fact]
    public void Shipped_reading_content_parses()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "readings.json"));

        var readings = ContentParser.ParseReadings(json);

        Assert.True(readings.MoistureProbeWidth > 0);
    }

    [Theory]
    [InlineData("{ \"moistureProbeWidth\": 0 }")]
    [InlineData("{ }")]
    public void Rejects_a_missing_or_non_positive_probe_width(string json)
    {
        var error = Assert.Throws<ContentException>(() => ContentParser.ParseReadings(json));

        Assert.Contains("moistureProbeWidth", error.Message);
    }
}
