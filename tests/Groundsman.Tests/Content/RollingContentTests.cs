using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class RollingContentTests
{
    private static string Read(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", file));

    [Fact]
    public void Shipped_rollers_parse_lightest_first()
    {
        var rollers = ContentParser.ParseRollers(Read("rollers.json"));

        Assert.Equal(new[] { "light", "medium", "heavy" }, rollers.Select(r => r.Id));
        Assert.True(rollers[2].CompactionPerHour > rollers[0].CompactionPerHour);
        Assert.Equal(0.8, rollers[2].OveruseAbove);
    }

    [Fact]
    public void Shipped_compaction_content_parses()
    {
        var compaction = ContentParser.ParseCompaction(Read("compaction.json"));

        Assert.Equal(0.55, compaction.StartingCompaction);
        Assert.Equal(5, compaction.MinRollingMinutes);
        Assert.Equal(120, compaction.MaxRollingMinutes);
        Assert.Equal(0.7, compaction.HardnessDryWeight);
    }

    [Theory]
    [InlineData("\"compactionPerHour\": 0.04", "\"compactionPerHour\": -1", "compactionPerHour")]
    [InlineData("\"wetDamagePerHour\": 0.01", "\"wetDamagePerHour\": -1", "wetDamagePerHour")]
    [InlineData("\"overuseAbove\": 0.8", "\"overuseAbove\": 1.5", "overuseAbove")]
    [InlineData("\"id\": \"heavy\"", "\"id\": \"light\"", "light")]
    public void Rejects_invalid_rollers(string original, string replacement, string field)
    {
        var json = Read("rollers.json");
        Assert.Contains(original, json);

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseRollers(json.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }

    [Theory]
    [InlineData("\"compaction\": 0.55", "\"compaction\": 1.2", "compaction")]
    [InlineData("\"max\": 120", "\"max\": 4", "rollingMinutes")]
    [InlineData("\"dryWeight\": 0.7", "\"dryWeight\": 2", "dryWeight")]
    [InlineData("\"clayReference\": 30", "\"clayReference\": 0", "clayReference")]
    public void Rejects_invalid_compaction_values(string original, string replacement, string field)
    {
        var json = Read("compaction.json");
        Assert.Contains(original, json);

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseCompaction(json.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }
}
