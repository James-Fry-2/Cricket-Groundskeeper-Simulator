using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class MoistureContentTests
{
    [Fact]
    public void Shipped_moisture_content_parses()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "moisture.json"));

        var moisture = ContentParser.ParseMoisture(json);

        Assert.True(moisture.SurfaceDepthMm > 0);
        Assert.True(moisture.SubsurfaceDepthMm > 0);
    }

    [Theory]
    [InlineData("\"surfaceDepthMm\": 25", "\"surfaceDepthMm\": 0", "surfaceDepthMm")]
    [InlineData("\"subsurfaceDepthMm\": 75", "\"subsurfaceDepthMm\": -5", "subsurfaceDepthMm")]
    [InlineData("\"perDegreeMm\": 0.004", "\"perDegreeMm\": -1", "perDegreeMm")]
    [InlineData("\"windFactorPerKph\": 0.02", "\"windFactorPerKph\": -1", "windFactorPerKph")]
    [InlineData("\"perSunshineHourMm\": 0.2", "\"perSunshineHourMm\": -1", "perSunshineHourMm")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        const string valid = @"{
            ""surfaceDepthMm"": 25,
            ""subsurfaceDepthMm"": 75,
            ""evaporation"": { ""perDegreeMm"": 0.004, ""windFactorPerKph"": 0.02, ""perSunshineHourMm"": 0.2 }
        }";

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseMoisture(valid.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }
}
