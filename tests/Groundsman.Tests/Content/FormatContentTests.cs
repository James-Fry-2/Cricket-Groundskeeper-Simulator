using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class FormatContentTests
{
    private static string Shipped() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "formats.json"));

    [Fact]
    public void Shipped_formats_parse()
    {
        var formats = ContentParser.ParseFormats(Shipped());

        Assert.Equal(new[] { "fourDay", "oneDay", "t20" }, formats.Select(f => f.Id));
        var fourDay = formats[0];
        Assert.Equal(4, fourDay.Days);
        Assert.Equal(2, fourDay.InningsPerSide);
        Assert.Null(fourDay.OversPerInnings);
        Assert.Equal(96, fourDay.OversPerDay);
        Assert.Equal(3.2, fourDay.RunsPerOver);
        Assert.Equal(0.3, formats[2].WicketsPerOver);
        Assert.Equal(3, fourDay.Sessions.Count);
        Assert.Equal(new[] { 8, 13, 16, 18 }, fourDay.DecisionHours);
        Assert.Equal(20, formats[2].OversPerInnings);
        Assert.Equal(18, formats[2].Sessions[0].Start);
        Assert.Equal(new[] { "Lunch", "Tea", "Stumps" }, fourDay.BreakNames);
    }

    [Theory]
    [InlineData("\"days\": 4", "\"days\": 0", "days")]
    [InlineData("\"inningsPerSide\": 2", "\"inningsPerSide\": 3", "inningsPerSide")]
    [InlineData("\"oversPerInnings\": 50", "\"oversPerInnings\": 0", "oversPerInnings")]
    [InlineData("\"oversPerDay\": 96,", "", "overs")]
    [InlineData("{ \"start\": 14, \"end\": 16 }", "{ \"start\": 12, \"end\": 16 }", "sessions")]
    [InlineData("{ \"start\": 18, \"end\": 20 }", "{ \"start\": 18, \"end\": 18 }", "sessions")]
    [InlineData("[8, 14, 18]", "[12, 14, 18]", "decisionHours")]
    [InlineData("[\"Lunch\", \"Tea\", \"Stumps\"]", "[\"Lunch\"]", "breakNames")]
    [InlineData("[8, 14, 18]", "[8, 18, 14]", "decisionHours")]
    [InlineData("\"id\": \"t20\"", "\"id\": \"oneDay\"", "oneDay")]
    [InlineData("\"runsPerOver\": 3.2", "\"runsPerOver\": 0", "runsPerOver")]
    [InlineData("\"wicketsPerOver\": 0.07", "\"wicketsPerOver\": -1", "wicketsPerOver")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var json = Shipped();
        Assert.Contains(original, json);

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseFormats(ReplaceFirst(json, original, replacement)));

        Assert.Contains(field, error.Message);
    }

    private static string ReplaceFirst(string text, string original, string replacement)
    {
        var index = text.IndexOf(original, StringComparison.Ordinal);
        return text.Substring(0, index) + replacement + text.Substring(index + original.Length);
    }
}
