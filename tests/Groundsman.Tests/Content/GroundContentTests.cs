using Groundsman.Core.Content;
using Groundsman.Core.Strips;

namespace Groundsman.Tests.Content;

public class GroundContentTests
{
    private const string Valid = @"{
        ""name"": ""Kestrel Lane"",
        ""ends"": [""Pavilion End"", ""Kestrel Road End""],
        ""centreStrips"": [2],
        ""strips"": [
            { ""number"": 1, ""loam"": ""county"", ""surfaceMoisture"": 24, ""subsurfaceMoisture"": 28 },
            { ""number"": 2, ""loam"": ""heavy"", ""surfaceMoisture"": 26, ""subsurfaceMoisture"": 30 }
        ]
    }";

    [Fact]
    public void Shipped_ground_content_parses_with_twelve_strips()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "ground.json"));

        var ground = ContentParser.ParseGround(json);

        Assert.Equal(12, ground.Strips.Count);
        Assert.Equal(new[] { 5, 6, 7, 8 }, ground.Strips.Where(s => ground.IsCentre(s.Id)).Select(s => s.Id.Number));
    }

    [Fact]
    public void Parses_every_field()
    {
        var ground = ContentParser.ParseGround(Valid);

        Assert.Equal("Kestrel Lane", ground.Name);
        Assert.Equal(new[] { "Pavilion End", "Kestrel Road End" }, ground.Ends);
        Assert.Equal(2, ground.Strips.Count);
        Assert.Equal(new StripId(2), ground.Strips[1].Id);
        Assert.Equal("heavy", ground.Strips[1].LoamId);
        Assert.Equal(26, ground.Strips[1].SurfaceMoisture);
        Assert.Equal(30, ground.Strips[1].SubsurfaceMoisture);
        Assert.True(ground.IsCentre(new StripId(2)));
        Assert.False(ground.IsCentre(new StripId(1)));
        Assert.Equal(0, ground.FromCentre(new StripId(2)));
        Assert.Equal(1, ground.FromCentre(new StripId(1)));
    }

    [Theory]
    [InlineData("\"name\": \"Kestrel Lane\"", "\"name\": \" \"", "name")]
    [InlineData("\"number\": 2", "\"number\": 3", "number")]
    [InlineData("[\"Pavilion End\", \"Kestrel Road End\"]", "[\"Pavilion End\"]", "ends")]
    [InlineData("\"loam\": \"heavy\"", "\"loam\": \"\"", "loam")]
    [InlineData("\"surfaceMoisture\": 26", "\"surfaceMoisture\": 101", "surfaceMoisture")]
    [InlineData("\"subsurfaceMoisture\": 30", "\"subsurfaceMoisture\": -1", "subsurfaceMoisture")]
    [InlineData("\"centreStrips\": [2]", "\"centreStrips\": [3]", "centreStrips")]
    [InlineData("\"centreStrips\": [2]", "\"centreStrips\": []", "centreStrips")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var error = Assert.Throws<ContentException>(() => ContentParser.ParseGround(Valid.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }

    [Fact]
    public void Rejects_a_ground_with_no_strips()
    {
        var json = @"{ ""name"": ""Kestrel Lane"", ""ends"": [""A"", ""B""], ""centreStrips"": [1], ""strips"": [] }";

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
