using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class RatingContentTests
{
    private static string Shipped() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "rating.json"));

    [Fact]
    public void Shipped_rating_content_parses()
    {
        var rating = ContentParser.ParseRating(Shipped());

        Assert.Equal(3.0, rating.UnfitBelow);
        Assert.Equal(6.5, rating.UnsatisfactoryConsistencyBelow);
        Assert.Equal(8.5, rating.VeryGoodConsistencyAtLeast);
        Assert.Equal(1, rating.UnsatisfactoryDemerits);
        Assert.Equal(3, rating.UnfitDemerits);
        Assert.Equal(5, rating.WindowYears);
        Assert.Equal(5, rating.BanAt);
    }

    [Theory]
    [InlineData("\"abandonBelow\": 2.5", "\"abandonBelow\": 3.5", "abandonBelow")]
    [InlineData("\"consistencyBelow\": 6.5", "\"consistencyBelow\": 2", "consistencyBelow")]
    [InlineData("\"consistencyAtLeast\": 8.5", "\"consistencyAtLeast\": 6", "consistencyAtLeast")]
    [InlineData("\"bowlersOversShare\": 0.5", "\"bowlersOversShare\": 1.5", "bowlersOversShare")]
    [InlineData("\"windowYears\": 5", "\"windowYears\": 0", "windowYears")]
    [InlineData("\"flat\": ", "\"level\": ", "flat")]
    [InlineData("{value}/10): {cause}\",\n    \"abandoned\"", "{value}/10): {reason}\",\n    \"abandoned\"", "reason")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var json = Shipped();
        Assert.Contains(original, json);

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseRating(json.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }
}
