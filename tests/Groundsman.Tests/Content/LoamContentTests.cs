using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class LoamContentTests
{
    private const string Valid = @"{
        ""loams"": [
            {
                ""id"": ""county"", ""name"": ""County loam"", ""clayPercent"": 30,
                ""saturation"": 42, ""fieldCapacity"": 32, ""airDry"": 6,
                ""surfaceDrainageRate"": 0.08, ""subsurfaceDrainageRate"": 0.02, ""capillaryRate"": 0.01,
                ""crackingTendency"": 0.5
            }
        ]
    }";

    [Fact]
    public void Shipped_loam_content_parses()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "loams.json"));

        Assert.NotEmpty(ContentParser.ParseLoams(json));
    }

    [Fact]
    public void Parses_every_field()
    {
        var loam = Assert.Single(ContentParser.ParseLoams(Valid));

        Assert.Equal("county", loam.Id);
        Assert.Equal("County loam", loam.Name);
        Assert.Equal(30, loam.ClayPercent);
        Assert.Equal(42, loam.Saturation);
        Assert.Equal(32, loam.FieldCapacity);
        Assert.Equal(6, loam.AirDry);
        Assert.Equal(0.08, loam.SurfaceDrainageRate);
        Assert.Equal(0.02, loam.SubsurfaceDrainageRate);
        Assert.Equal(0.01, loam.CapillaryRate);
        Assert.Equal(0.5, loam.CrackingTendency);
    }

    [Theory]
    [InlineData("\"clayPercent\": 30", "\"clayPercent\": 101", "clayPercent")]
    [InlineData("\"saturation\": 42", "\"saturation\": 30", "fieldCapacity")]
    [InlineData("\"airDry\": 6", "\"airDry\": 33", "airDry")]
    [InlineData("\"airDry\": 6", "\"airDry\": -1", "airDry")]
    [InlineData("\"surfaceDrainageRate\": 0.08", "\"surfaceDrainageRate\": 1.5", "surfaceDrainageRate")]
    [InlineData("\"capillaryRate\": 0.01", "\"capillaryRate\": -0.1", "capillaryRate")]
    [InlineData("\"crackingTendency\": 0.5", "\"crackingTendency\": 2", "crackingTendency")]
    [InlineData("\"id\": \"county\"", "\"id\": \"\"", "id")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var error = Assert.Throws<ContentException>(() => ContentParser.ParseLoams(Valid.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }

    [Fact]
    public void Rejects_two_loams_with_the_same_id()
    {
        var loam = Valid.Substring(Valid.IndexOf('{', 1), Valid.LastIndexOf('}', Valid.LastIndexOf(']')) - Valid.IndexOf('{', 1) + 1);
        var json = $"{{ \"loams\": [ {loam}, {loam} ] }}";

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseLoams(json));

        Assert.Contains("county", error.Message);
    }
}
