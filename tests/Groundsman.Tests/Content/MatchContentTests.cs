using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class MatchContentTests
{
    private static string Shipped() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "match.json"));

    [Fact]
    public void Shipped_match_content_parses()
    {
        var match = ContentParser.ParseMatch(Shipped());

        Assert.Equal(0.8, match.StrengthScale);
        Assert.Equal(2.0, match.UnevenWickets);
        Assert.Equal(250, match.DeclarationLead);
        Assert.Equal(6, match.TossBowlFirstSeamAbove);
    }

    [Theory]
    [InlineData("\"strengthScale\": 0.8", "\"strengthScale\": -1", "strengthScale")]
    [InlineData("\"uneven\": 2.0", "\"uneven\": -1", "wickets.uneven")]
    [InlineData("\"dead\": 0.3", "\"dead\": 1.5", "runs.dead")]
    [InlineData("\"spread\": 0.35", "\"spread\": -0.1", "spread")]
    [InlineData("\"fromDay\": 3", "\"fromDay\": 0", "fromDay")]
    [InlineData("\"rainRestartLoss\": 0.5", "\"rainRestartLoss\": 2", "rainRestartLoss")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var error = Assert.Throws<ContentException>(() => ContentParser.ParseMatch(Shipped().Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }
}
