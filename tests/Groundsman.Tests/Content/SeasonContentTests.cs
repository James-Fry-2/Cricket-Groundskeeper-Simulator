using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class SeasonContentTests
{
    private const string Valid = @"{
        ""start"": ""2027-03-25"",
        ""matchDays"": [""2027-04-16"", ""2027-04-17""]
    }";

    [Fact]
    public void Shipped_season_content_parses()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "season.json"));

        var season = ContentParser.ParseSeason(json);

        Assert.NotEmpty(season.MatchDays);
    }

    [Fact]
    public void Parses_every_field()
    {
        var season = ContentParser.ParseSeason(Valid);

        Assert.Equal(new DateTime(2027, 3, 25), season.Start);
        Assert.Equal(new[] { new DateTime(2027, 4, 16), new DateTime(2027, 4, 17) }, season.MatchDays);
    }

    [Theory]
    [InlineData("\"2027-03-25\"", "\"25/03/2027\"", "start")]
    [InlineData("\"2027-04-17\"", "\"2027-04-16\"", "matchDays")]
    [InlineData("\"2027-04-16\"", "\"2027-03-24\"", "matchDays")]
    [InlineData("\"2027-04-17\"", "\"April\"", "matchDays")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var error = Assert.Throws<ContentException>(() => ContentParser.ParseSeason(Valid.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }
}
