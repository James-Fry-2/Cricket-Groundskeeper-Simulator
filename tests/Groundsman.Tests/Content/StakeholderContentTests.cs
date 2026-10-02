using Groundsman.Core.Content;
using Groundsman.Core.Pressures;

namespace Groundsman.Tests.Content;

public class StakeholderContentTests
{
    private static string Shipped => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "stakeholders.json"));

    [Fact]
    public void Shipped_stakeholder_content_parses()
    {
        var settings = ContentParser.ParseStakeholders(Shipped);

        Assert.Equal(50, settings.StartingSatisfaction);
        Assert.Equal(new[] { "fourDay", "oneDay", "t20" }, settings.CaptainCharacters.Keys.OrderBy(k => k, StringComparer.Ordinal));
        Assert.Equal(2, settings.CaptainCharacters["fourDay"][RequestKind.Green]);
        Assert.True(settings.CaptainAnswers.NotDelivered < settings.CaptainAnswers.Declined);
        Assert.True(settings.BoardAnswers.Delivered > 0);
        Assert.True(settings.Unfit < settings.Unsatisfactory);
    }

    [Theory]
    [InlineData("\"green\": 2", "\"seaming\": 2", "seaming")]
    [InlineData("\"green\": 2", "\"green\": -2", "fourDay")]
    [InlineData("\"requestChance\": 0.6", "\"requestChance\": 1.6", "captain.requestChance")]
    [InlineData("\"startingSatisfaction\": 50", "\"startingSatisfaction\": 150", "startingSatisfaction")]
    [InlineData("\"homeWin\": 3,", "", "homeWin")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var json = Shipped;
        Assert.Contains(original, json);

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseStakeholders(json.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }
}
