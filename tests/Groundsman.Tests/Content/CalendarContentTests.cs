using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class CalendarContentTests
{
    private const string Valid = @"{
        ""seasonStart"": ""04-01"",
        ""seasonEnd"": ""09-30"",
        ""morningHour"": 7,
        ""afternoonHour"": 13,
        ""offSeasonStepDays"": 7,
        ""finalPrepDays"": 3
    }";

    [Fact]
    public void Shipped_calendar_content_parses()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "calendar.json"));

        var settings = ContentParser.ParseCalendar(json);

        Assert.True(settings.SeasonStart.CompareTo(settings.SeasonEnd) < 0);
    }

    [Fact]
    public void Parses_every_field()
    {
        var settings = ContentParser.ParseCalendar(Valid);

        Assert.Equal(new MonthDay(4, 1), settings.SeasonStart);
        Assert.Equal(new MonthDay(9, 30), settings.SeasonEnd);
        Assert.Equal(7, settings.MorningHour);
        Assert.Equal(13, settings.AfternoonHour);
        Assert.Equal(7, settings.OffSeasonStepDays);
        Assert.Equal(3, settings.FinalPrepDays);
    }

    [Theory]
    [InlineData("\"morningHour\": 7", "\"morningHour\": 24", "morningHour")]
    [InlineData("\"afternoonHour\": 13", "\"afternoonHour\": 7", "afternoonHour")]
    [InlineData("\"offSeasonStepDays\": 7", "\"offSeasonStepDays\": 0", "offSeasonStepDays")]
    [InlineData("\"finalPrepDays\": 3", "\"finalPrepDays\": -1", "finalPrepDays")]
    [InlineData("\"seasonEnd\": \"09-30\"", "\"seasonEnd\": \"03-01\"", "seasonEnd")]
    [InlineData("\"seasonStart\": \"04-01\"", "\"seasonStart\": \"02-29\"", "seasonStart")]
    [InlineData("\"seasonStart\": \"04-01\"", "\"seasonStart\": \"April\"", "seasonStart")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var json = Valid.Replace(original, replacement);

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseCalendar(json));

        Assert.Contains(field, error.Message);
    }

    [Fact]
    public void Rejects_a_missing_field()
    {
        var json = Valid.Replace(",\n        \"finalPrepDays\": 3", "");

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseCalendar(json));

        Assert.Contains("finalPrepDays", error.Message);
    }

    [Fact]
    public void Rejects_an_unknown_field()
    {
        var json = Valid.Replace("\"finalPrepDays\"", "\"finalPrepDayz\"");

        Assert.Throws<ContentException>(() => ContentParser.ParseCalendar(json));
    }
}
