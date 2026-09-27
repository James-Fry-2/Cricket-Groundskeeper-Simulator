using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class ScoringContentTests
{
    private static string Shipped() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "scoring.json"));

    [Fact]
    public void Shipped_scoring_content_parses()
    {
        var scoring = ContentParser.ParseScoring(Shipped());

        Assert.Equal(24, scoring.SubsurfaceMin);
        Assert.Equal(30, scoring.SubsurfaceMax);
        Assert.Equal(22, scoring.SurfaceMax);
    }

    [Theory]
    [InlineData("\"subsurfaceMax\": 30", "\"subsurfaceMax\": 20", "subsurfaceMax")]
    [InlineData("\"surfaceMax\": 22", "\"surfaceMax\": 0", "surfaceMax")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var error = Assert.Throws<ContentException>(() => ContentParser.ParseScoring(Shipped().Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }

    [Theory]
    [InlineData(26, 18, true, MatchMorningMiss.None)]
    [InlineData(22, 18, false, MatchMorningMiss.SubsurfaceDry)]
    [InlineData(32, 18, false, MatchMorningMiss.SubsurfaceWet)]
    [InlineData(26, 23, false, MatchMorningMiss.SurfaceWet)]
    public void Judges_a_match_morning(double subsurface, double surface, bool onTarget, MatchMorningMiss miss)
    {
        var scoring = ContentParser.ParseScoring(Shipped());

        Assert.Equal(onTarget, scoring.IsOnTarget(surface, subsurface));
        Assert.Equal(miss, scoring.Judge(surface, subsurface));
    }
}
