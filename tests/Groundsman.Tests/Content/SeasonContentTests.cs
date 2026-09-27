using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;

namespace Groundsman.Tests.Content;

public class SeasonContentTests
{
    private const string Valid = @"{
        ""start"": ""2027-03-25"",
        ""fixtures"": [
            { ""start"": ""2027-04-16"", ""days"": 4, ""strip"": 6 },
            { ""start"": ""2027-04-25"", ""days"": 1, ""strip"": 8 }
        ]
    }";

    [Fact]
    public void Shipped_season_content_parses()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "season.json"));

        var season = ContentParser.ParseSeason(json);

        Assert.True(season.Fixtures.Count >= 10);
    }

    [Fact]
    public void Parses_every_field()
    {
        var season = ContentParser.ParseSeason(Valid);

        Assert.Equal(new DateTime(2027, 3, 25), season.Start);
        Assert.Equal(2, season.Fixtures.Count);
        var first = season.Fixtures[0];
        Assert.Equal(new DateTime(2027, 4, 16), first.Start);
        Assert.Equal(4, first.Days);
        Assert.Equal(new StripId(6), first.Strip);
        Assert.Equal(new DateTime(2027, 4, 19), first.End);
    }

    [Fact]
    public void Match_days_are_every_day_of_every_fixture()
    {
        var season = ContentParser.ParseSeason(Valid);

        Assert.Equal(
            new[] { new DateTime(2027, 4, 16), new DateTime(2027, 4, 17), new DateTime(2027, 4, 18), new DateTime(2027, 4, 19), new DateTime(2027, 4, 25) },
            season.MatchDays);
    }

    [Theory]
    [InlineData("\"start\": \"2027-03-25\"", "\"start\": \"25/03/2027\"", "start")]
    [InlineData("\"start\": \"2027-04-25\"", "\"start\": \"2027-04-19\"", "fixtures")]
    [InlineData("\"start\": \"2027-04-16\"", "\"start\": \"2027-03-24\"", "fixtures")]
    [InlineData("\"days\": 4", "\"days\": 0", "days")]
    [InlineData("\"strip\": 8", "\"strip\": 0", "strip")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var error = Assert.Throws<ContentException>(() => ContentParser.ParseSeason(Valid.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }

    [Fact]
    public void A_game_rejects_a_fixture_on_a_strip_the_ground_doesnt_have()
    {
        var fixture = new Fixture(new DateTime(2027, 4, 16), 1, new StripId(13));

        var error = Assert.Throws<ContentException>(() => new GameSetup(TestContent.Content, new Groundsman.Core.Time.GameTime(2027, 4, 1, 7), new[] { fixture }, seed: 1));

        Assert.Contains("strip", error.Message);
    }
}
