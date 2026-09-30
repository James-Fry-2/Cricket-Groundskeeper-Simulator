using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;

namespace Groundsman.Tests.Content;

public class SeasonContentTests
{
    private const string Valid = @"{
        ""start"": ""2027-03-25"",
        ""fixtures"": [
            { ""start"": ""2027-04-16"", ""format"": ""fourDay"", ""opponent"": ""away"", ""strip"": 6 },
            { ""start"": ""2027-04-25"", ""format"": ""t20"", ""opponent"": ""away"", ""strip"": 8, ""televised"": true }
        ]
    }";

    [Fact]
    public void Shipped_season_content_parses_against_shipped_formats_and_teams()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "content");
        var formats = ContentParser.ParseFormats(File.ReadAllText(Path.Combine(directory, "formats.json")));
        var teams = ContentParser.ParseTeams(File.ReadAllText(Path.Combine(directory, "teams.json")));

        var season = ContentParser.ParseSeason(File.ReadAllText(Path.Combine(directory, "season.json")), formats, teams);

        Assert.True(season.Fixtures.Count >= 10);
        Assert.Contains(season.Fixtures, f => f.Format.Id == "t20");
        Assert.All(season.Fixtures, f => Assert.NotEqual(teams.HomeId, f.Opponent.Id));
    }

    [Fact]
    public void Parses_every_field()
    {
        var season = Parse(Valid);

        Assert.Equal(new DateTime(2027, 3, 25), season.Start);
        Assert.Equal(2, season.Fixtures.Count);
        var first = season.Fixtures[0];
        Assert.Equal(new DateTime(2027, 4, 16), first.Start);
        Assert.Same(TestFormats.FourDay, first.Format);
        Assert.Same(TestTeams.Opponent, first.Opponent);
        Assert.Equal(4, first.Days);
        Assert.Equal(new StripId(6), first.PresetStrip);
        Assert.Equal("2027-04-16", first.Id);
        Assert.False(first.Televised);
        Assert.True(season.Fixtures[1].Televised);
        Assert.Equal(new DateTime(2027, 4, 19), first.End);
        Assert.Same(TestFormats.T20, season.Fixtures[1].Format);
    }

    [Fact]
    public void A_fixture_needs_no_strip_since_the_player_assigns_one()
    {
        var season = Parse(Valid.Replace(", \"strip\": 6", ""));

        Assert.Null(season.Fixtures[0].PresetStrip);
    }

    [Fact]
    public void Shipped_fixtures_leave_the_strips_to_the_player_and_include_televised_matches()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "content");
        var formats = ContentParser.ParseFormats(File.ReadAllText(Path.Combine(directory, "formats.json")));
        var teams = ContentParser.ParseTeams(File.ReadAllText(Path.Combine(directory, "teams.json")));

        var season = ContentParser.ParseSeason(File.ReadAllText(Path.Combine(directory, "season.json")), formats, teams);

        Assert.All(season.Fixtures, f => Assert.Null(f.PresetStrip));
        Assert.Contains(season.Fixtures, f => f.Televised && f.Start.Month >= 8);
    }

    [Fact]
    public void Match_days_are_every_day_of_every_fixture()
    {
        Assert.Equal(
            new[] { new DateTime(2027, 4, 16), new DateTime(2027, 4, 17), new DateTime(2027, 4, 18), new DateTime(2027, 4, 19), new DateTime(2027, 4, 25) },
            Parse(Valid).MatchDays);
    }

    [Theory]
    [InlineData("\"start\": \"2027-03-25\"", "\"start\": \"25/03/2027\"", "start")]
    [InlineData("\"start\": \"2027-04-25\"", "\"start\": \"2027-04-19\"", "fixtures")]
    [InlineData("\"start\": \"2027-04-16\"", "\"start\": \"2027-03-24\"", "fixtures")]
    [InlineData("\"format\": \"t20\"", "\"format\": \"hundred\"", "hundred")]
    [InlineData("\"opponent\": \"away\", \"strip\": 8", "\"opponent\": \"nobody\", \"strip\": 8", "nobody")]
    [InlineData("\"opponent\": \"away\", \"strip\": 8", "\"opponent\": \"home\", \"strip\": 8", "home")]
    [InlineData("\"strip\": 8", "\"strip\": 0", "strip")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var error = Assert.Throws<ContentException>(() => Parse(Valid.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }

    [Fact]
    public void A_game_rejects_a_fixture_on_a_strip_the_ground_doesnt_have()
    {
        var fixture = new Fixture(new DateTime(2027, 4, 16), TestFormats.OneDay, new StripId(13), TestTeams.Opponent);

        var error = Assert.Throws<ContentException>(() => new GameSetup(TestContent.Content, new Groundsman.Core.Time.GameTime(2027, 4, 1, 7), new[] { fixture }, seed: 1));

        Assert.Contains("strip", error.Message);
    }

    [Fact]
    public void A_game_rejects_one_strip_set_for_two_fixtures_whose_build_ups_overlap()
    {
        var first = new Fixture(new DateTime(2027, 4, 16), TestFormats.OneDay, new StripId(3), TestTeams.Opponent);
        var second = new Fixture(new DateTime(2027, 4, 20), TestFormats.OneDay, new StripId(3), TestTeams.Opponent);

        var error = Assert.Throws<ContentException>(() => new GameSetup(TestContent.Content, new Groundsman.Core.Time.GameTime(2027, 4, 1, 7), new[] { first, second }, seed: 1));

        Assert.Contains("overlap", error.Message);
    }

    private static SeasonSettings Parse(string json) =>
        ContentParser.ParseSeason(json, TestFormats.All, new TeamsSettings(TestTeams.All, TestTeams.Home.Id));
}
