using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class CoverContentTests
{
    [Fact]
    public void Shipped_cover_content_parses()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "covers.json"));

        var covers = ContentParser.ParseCovers(json);

        Assert.True(covers.Count > 0);
    }

    [Theory]
    [InlineData("{ \"count\": -1, \"evaporationFactor\": 0.3 }", "count")]
    [InlineData("{ \"count\": 4, \"evaporationFactor\": 1.5 }", "evaporationFactor")]
    [InlineData("{ \"count\": 4 }", "evaporationFactor")]
    public void Rejects_invalid_values_naming_the_field(string json, string field)
    {
        var error = Assert.Throws<ContentException>(() => ContentParser.ParseCovers(json));

        Assert.Contains(field, error.Message);
    }
}
