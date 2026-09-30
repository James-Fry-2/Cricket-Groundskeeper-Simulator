using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class WearContentTests
{
    [Fact]
    public void Shipped_wear_content_parses()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "wear.json"));

        var settings = ContentParser.ParseWear(json);

        Assert.InRange(settings.NeighbourShare, 0.01, 1);
    }

    [Fact]
    public void Rejects_a_neighbour_share_outside_zero_to_one()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "wear.json"));
        var shipped = ContentParser.ParseWear(json).NeighbourShare.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseWear(json.Replace($"\"neighbourShare\": {shipped}", "\"neighbourShare\": 1.5")));

        Assert.Contains("neighbourShare", error.Message);
    }
}
