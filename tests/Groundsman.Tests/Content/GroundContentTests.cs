using Groundsman.Core.Content;
using Groundsman.Core.Strips;

namespace Groundsman.Tests.Content;

public class GroundContentTests
{
    private const string Valid = @"{
        ""name"": ""Kestrel Lane"",
        ""saturation"": 40,
        ""strips"": [
            { ""number"": 1, ""surfaceMoisture"": 24, ""subsurfaceMoisture"": 28 },
            { ""number"": 2, ""surfaceMoisture"": 26, ""subsurfaceMoisture"": 30 }
        ]
    }";

    [Fact]
    public void Shipped_ground_content_parses_with_twelve_strips()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "ground.json"));

        var ground = ContentParser.ParseGround(json);

        Assert.Equal(12, ground.Strips.Count);
    }

    [Fact]
    public void Parses_every_field()
    {
        var ground = ContentParser.ParseGround(Valid);

        Assert.Equal("Kestrel Lane", ground.Name);
        Assert.Equal(40, ground.Saturation);
        Assert.Equal(2, ground.Strips.Count);
        Assert.Equal(new StripId(2), ground.Strips[1].Id);
        Assert.Equal(26, ground.Strips[1].SurfaceMoisture);
        Assert.Equal(30, ground.Strips[1].SubsurfaceMoisture);
    }

    [Theory]
    [InlineData("\"name\": \"Kestrel Lane\"", "\"name\": \" \"", "name")]
    [InlineData("\"saturation\": 40", "\"saturation\": 0", "saturation")]
    [InlineData("\"saturation\": 40", "\"saturation\": 101", "saturation")]
    [InlineData("\"number\": 2", "\"number\": 3", "number")]
    [InlineData("\"surfaceMoisture\": 26", "\"surfaceMoisture\": 41", "surfaceMoisture")]
    [InlineData("\"subsurfaceMoisture\": 30", "\"subsurfaceMoisture\": -1", "subsurfaceMoisture")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var error = Assert.Throws<ContentException>(() => ContentParser.ParseGround(Valid.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }

    [Fact]
    public void Rejects_a_ground_with_no_strips()
    {
        var json = @"{ ""name"": ""Kestrel Lane"", ""saturation"": 40, ""strips"": [] }";

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseGround(json));

        Assert.Contains("strips", error.Message);
    }

    [Fact]
    public void Rejects_a_strip_missing_a_field()
    {
        var json = Valid.Replace(", \"subsurfaceMoisture\": 28", "");

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseGround(json));

        Assert.Contains("subsurfaceMoisture", error.Message);
    }
}
