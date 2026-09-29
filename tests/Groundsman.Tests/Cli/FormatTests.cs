using Groundsman.Cli;
using Groundsman.Core.Readings;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Cli;

public class FormatTests
{
    [Theory]
    [InlineData(21.2, 29.2, "21–30%")]
    [InlineData(22.0, 30.0, "22–30%")]
    [InlineData(21.9, 29.9, "21–30%")]
    public void Ranges_round_outwards_so_the_truth_stays_inside(double low, double high, string expected)
    {
        Assert.Equal(expected, Format.Percent(new ValueRange(low, high)));
    }

    [Theory]
    [InlineData(0.0, 0.0, "dry")]
    [InlineData(0.0, 2.3, "0–3 mm")]
    [InlineData(4.6, 11.2, "4–12 mm")]
    public void Rain_ranges_round_outwards_and_read_dry_when_nothing_is_expected(double low, double high, string expected)
    {
        Assert.Equal(expected, Format.Rain(new ValueRange(low, high)));
    }

    [Theory]
    [InlineData(0.02, "0%")]
    [InlineData(0.34, "30%")]
    [InlineData(0.36, "40%")]
    [InlineData(0.999, "100%")]
    public void Chance_of_rain_rounds_to_the_nearest_ten(double chance, string expected)
    {
        Assert.Equal(expected, Format.Chance(chance));
    }

    [Fact]
    public void Temperature_ranges_round_outwards()
    {
        Assert.Equal("17–22°C", Format.Temperature(new ValueRange(17.6, 21.2)));
    }

    [Theory]
    [InlineData(0, "today")]
    [InlineData(1, "yesterday")]
    [InlineData(5, "5 days ago")]
    public void Ages_read_in_days(int days, string expected)
    {
        var now = new GameTime(2027, 5, 10, 7);

        Assert.Equal(expected, Format.Age(now.AddDays(-days).AtHour(13), now));
    }

    [Fact]
    public void Weather_shows_gauge_temperature_and_wind()
    {
        var text = Format.Weather(new Groundsman.Core.WeatherObservation(3.26, 11.4, 17.6, 9.6, 22.4));

        Assert.Equal("Rain in the last 24 hours: 3.3 mm. Now 11°C, wind 18 km/h. Yesterday 10–22°C.", text);
    }

    [Fact]
    public void Weather_leaves_out_yesterday_when_there_was_none()
    {
        var text = Format.Weather(new Groundsman.Core.WeatherObservation(0, 11.4, 17.6, null, null));

        Assert.Equal("Rain in the last 24 hours: 0.0 mm. Now 11°C, wind 18 km/h.", text);
    }

    [Fact]
    public void Next_match_names_the_day_and_strip()
    {
        var fixture = new Groundsman.Core.Fixture(new DateTime(2027, 5, 20), TestFormats.FourDay, new Groundsman.Core.Strips.StripId(6), TestTeams.Opponent);

        Assert.Equal("Four-day v Test Visitors, Thu 20 May on strip 6 (in 10 days)", Format.NextMatch(fixture, new GameTime(2027, 5, 10, 7)));
        Assert.Equal("Four-day v Test Visitors, Thu 20 May on strip 6 (in 1 day)", Format.NextMatch(fixture, new GameTime(2027, 5, 19, 7)));
        Assert.Equal("Four-day v Test Visitors, day 2 of 4 on strip 6", Format.NextMatch(fixture, new GameTime(2027, 5, 21, 8)));
        Assert.Equal("none scheduled", Format.NextMatch(null, new GameTime(2027, 5, 21, 8)));
    }

    [Theory]
    [InlineData(312, 8, 96.0, false, "Kestrelshire 312-8 (96 ov)")]
    [InlineData(412, 6, 101.3, true, "Kestrelshire 412-6d (101.3 ov)")]
    [InlineData(187, 10, 54.2, false, "Kestrelshire 187 all out (54.2 ov)")]
    public void Innings_read_like_a_scoreboard(int runs, int wickets, double overs, bool declared, string expected)
    {
        Assert.Equal(expected, Format.Innings(new Groundsman.Core.Match.InningsView("Kestrelshire", runs, wickets, overs, declared)));
    }

    [Theory]
    [InlineData(Groundsman.Core.Match.PitchGrade.VeryGood, "Very good")]
    [InlineData(Groundsman.Core.Match.PitchGrade.Unfit, "Unfit")]
    public void Grades_read_as_words(Groundsman.Core.Match.PitchGrade grade, string expected)
    {
        Assert.Equal(expected, Format.Grade(grade));
    }

    [Fact]
    public void Times_show_day_date_and_hour()
    {
        Assert.Equal("Thu 25 Mar 2027, 07:00", Format.Time(new GameTime(2027, 3, 25, 7)));
    }

    [Fact]
    public void Intervals_say_what_can_be_done_on_the_match_strip()
    {
        var strip = new Groundsman.Core.Strips.StripId(6);

        Assert.Equal("Tea. On strip 6 you can clean the footholes (clean 6).",
            Format.Interval(new Groundsman.Core.Match.IntervalView("Tea", strip, canClean: true, canFill: false)));
        Assert.Equal("Stumps. On strip 6 you can clean the footholes (clean 6) or fill them for the night (fill 6).",
            Format.Interval(new Groundsman.Core.Match.IntervalView("Stumps", strip, canClean: true, canFill: true)));
    }

    [Theory]
    [InlineData(0, "0 demerits")]
    [InlineData(1, "1 demerit")]
    [InlineData(3, "3 demerits")]
    public void Demerits_are_counted_in_words(int count, string expected)
    {
        Assert.Equal(expected, Format.Demerits(count));
    }

    [Fact]
    public void The_scoreboard_leaves_off_an_innings_with_no_overs_faced()
    {
        var fixture = new Groundsman.Core.Fixture(new DateTime(2027, 5, 20), TestFormats.FourDay, new Groundsman.Core.Strips.StripId(6), TestTeams.Opponent);
        var first = new Groundsman.Core.Match.InningsView("Kestrelshire", 242, 10, 88.4, false);
        var next = new Groundsman.Core.Match.InningsView("Test Visitors", 0, 0, 0, false);
        var match = new Groundsman.Core.Match.MatchView(fixture, new[] { first, next }, false, null, Array.Empty<Groundsman.Core.Match.CommentaryLine>(), null);

        Assert.Equal(new[] { first }, Format.Started(match));
    }
}
