using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class TeamContentTests
{
    private static string Shipped() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "teams.json"));

    [Fact]
    public void Shipped_teams_parse_with_a_home_side_and_opponents()
    {
        var teams = ContentParser.ParseTeams(Shipped());

        Assert.Equal("kestrelshire", teams.HomeId);
        Assert.True(teams.Teams.Count >= 4);
        var home = teams.Teams.Single(t => t.Id == teams.HomeId);
        Assert.Equal("Kestrelshire", home.Name);
        Assert.Equal(0.75, home.Attack.Seam);
        Assert.Equal(0.25, home.Attack.Spin, 9);
    }

    [Theory]
    [InlineData("\"batting\": 62", "\"batting\": 101", "batting")]
    [InlineData("\"bowling\": 64", "\"bowling\": -1", "bowling")]
    [InlineData("\"seam\": 0.75", "\"seam\": 1.2", "seam")]
    [InlineData("\"leftArm\": 0.2", "\"leftArm\": -0.1", "leftArm")]
    [InlineData("\"heavyFooted\": 0.5", "\"heavyFooted\": 2", "heavyFooted")]
    [InlineData("\"home\": \"kestrelshire\"", "\"home\": \"nobody\"", "home")]
    [InlineData("\"id\": \"fenwick\"", "\"id\": \"saltmarsh\"", "saltmarsh")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var json = Shipped();
        Assert.Contains(original, json);

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseTeams(json.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }
}
