using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class CommentaryContentTests
{
    private static string Shipped() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "commentary.json"));

    [Fact]
    public void Shipped_commentary_parses_with_every_event_and_cause()
    {
        var commentary = ContentParser.ParseCommentary(Shipped());

        Assert.All(CommentarySettings.EventIds, id => Assert.False(string.IsNullOrWhiteSpace(commentary.Event(id).Text)));
        Assert.All(CommentarySettings.CauseIds, id => Assert.False(string.IsNullOrWhiteSpace(commentary.Cause(id))));
        Assert.Equal(6, commentary.Event("seam").Threshold);
    }

    [Theory]
    [InlineData("{batting} are struggling", "{battingSide} are struggling", "battingSide")]
    [InlineData("\"seam\": {", "\"swing\": {", "seam")]
    [InlineData("\"hard\": ", "\"soft\": ", "hard")]
    public void Rejects_unknown_placeholders_and_missing_or_unknown_ids(string original, string replacement, string field)
    {
        var json = Shipped();
        Assert.Contains(original, json);

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseCommentary(json.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }
}
