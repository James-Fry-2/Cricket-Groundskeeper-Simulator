using Groundsman.Core.Time;

namespace Groundsman.Tests.Time;

public class PaceRulesTests
{
    private static PaceRules Rules(params DateTime[] matchDays) =>
        new PaceRules(new PaceContext(TestCalendar.Settings, OneDayFixtures(matchDays)));

    private static Groundsman.Core.Fixture[] OneDayFixtures(params DateTime[] days) =>
        days.Select(d => new Groundsman.Core.Fixture(d, TestFormats.OneDay, new Groundsman.Core.Strips.StripId(1), TestTeams.Opponent)).ToArray();

    [Fact]
    public void Off_season_steps_a_week()
    {
        var next = Rules().NextDecisionPoint(new GameTime(2027, 1, 4, 7));

        Assert.Equal(new GameTime(2027, 1, 11, 7), next);
    }

    [Fact]
    public void Off_season_week_stops_at_the_season_start()
    {
        var next = Rules().NextDecisionPoint(new GameTime(2027, 3, 28, 7));

        Assert.Equal(new GameTime(2027, 4, 1, 7), next);
    }

    [Fact]
    public void Off_season_week_stops_at_a_pre_season_match_and_its_prep()
    {
        var next = Rules(new DateTime(2027, 3, 20)).NextDecisionPoint(new GameTime(2027, 3, 14, 7));

        Assert.Equal(new GameTime(2027, 3, 17, 7), next);
    }

    [Fact]
    public void In_season_steps_a_day()
    {
        var next = Rules().NextDecisionPoint(new GameTime(2027, 5, 10, 7));

        Assert.Equal(new GameTime(2027, 5, 11, 7), next);
    }

    [Fact]
    public void Last_day_of_season_steps_into_the_off_season_then_weekly()
    {
        var rules = Rules();

        var first = rules.NextDecisionPoint(new GameTime(2027, 9, 30, 7));
        var second = rules.NextDecisionPoint(first);

        Assert.Equal(new GameTime(2027, 10, 1, 7), first);
        Assert.Equal(new GameTime(2027, 10, 8, 7), second);
    }

    [Fact]
    public void Final_prep_days_split_into_morning_and_afternoon()
    {
        var rules = Rules(new DateTime(2027, 5, 20));
        var sequence = Walk(rules, new GameTime(2027, 5, 16, 7), 7);

        Assert.Equal(new[]
        {
            new GameTime(2027, 5, 17, 7),
            new GameTime(2027, 5, 17, 13),
            new GameTime(2027, 5, 18, 7),
            new GameTime(2027, 5, 18, 13),
            new GameTime(2027, 5, 19, 7),
            new GameTime(2027, 5, 19, 13),
            new GameTime(2027, 5, 20, 8),
        }, sequence);
    }

    [Fact]
    public void Match_day_steps_through_sessions_then_next_morning()
    {
        var rules = Rules(new DateTime(2027, 5, 20));
        var sequence = Walk(rules, new GameTime(2027, 5, 20, 8), 4);

        Assert.Equal(new[]
        {
            new GameTime(2027, 5, 20, 13),
            new GameTime(2027, 5, 20, 16),
            new GameTime(2027, 5, 20, 18),
            new GameTime(2027, 5, 21, 7),
        }, sequence);
    }

    [Fact]
    public void Multi_day_match_goes_from_close_of_play_to_the_next_morning_session()
    {
        var next = Rules(new DateTime(2027, 5, 20), new DateTime(2027, 5, 21)).NextDecisionPoint(new GameTime(2027, 5, 20, 18));

        Assert.Equal(new GameTime(2027, 5, 21, 8), next);
    }

    [Fact]
    public void A_time_between_decision_points_moves_to_the_next_one_that_day()
    {
        var rules = Rules(new DateTime(2027, 5, 20));

        Assert.Equal(new GameTime(2027, 5, 20, 16), rules.NextDecisionPoint(new GameTime(2027, 5, 20, 14)));
        Assert.Equal(new GameTime(2027, 1, 4, 7), rules.NextDecisionPoint(new GameTime(2027, 1, 4, 0)));
    }

    [Fact]
    public void Next_decision_point_is_always_later_and_at_most_a_week_away()
    {
        var rules = Rules(new DateTime(2027, 3, 25), new DateTime(2027, 5, 20), new DateTime(2027, 5, 21), new DateTime(2027, 9, 30));
        var start = new GameTime(2027, 1, 1, 0);

        for (var hour = 0; hour < 24 * 365; hour++)
        {
            var now = start.AddHours(hour);
            var next = rules.NextDecisionPoint(now);

            Assert.True(next > now, $"{next} is not after {now}");
            Assert.True(now.HoursUntil(next) <= 24 * 7, $"{next} is more than a week after {now}");
        }
    }

    [Fact]
    public void Classifies_days_by_the_finest_pace_that_applies()
    {
        var context = new PaceContext(TestCalendar.Settings, OneDayFixtures(new DateTime(2027, 3, 25)));

        Assert.Equal(DayPace.OffSeason, context.PaceOn(new DateTime(2027, 3, 21)));
        Assert.Equal(DayPace.FinalPrep, context.PaceOn(new DateTime(2027, 3, 22)));
        Assert.Equal(DayPace.FinalPrep, context.PaceOn(new DateTime(2027, 3, 24)));
        Assert.Equal(DayPace.MatchDay, context.PaceOn(new DateTime(2027, 3, 25)));
        Assert.Equal(DayPace.OffSeason, context.PaceOn(new DateTime(2027, 3, 26)));
        Assert.Equal(DayPace.InSeason, context.PaceOn(new DateTime(2027, 4, 1)));
        Assert.Equal(DayPace.InSeason, context.PaceOn(new DateTime(2027, 9, 30)));
        Assert.Equal(DayPace.OffSeason, context.PaceOn(new DateTime(2027, 10, 1)));
    }

    [Fact]
    public void Match_day_turns_follow_the_fixtures_format()
    {
        var t20 = new Groundsman.Core.Fixture(new DateTime(2027, 6, 10), TestFormats.T20, new Groundsman.Core.Strips.StripId(1), TestTeams.Opponent);
        var rules = new PaceRules(new PaceContext(TestCalendar.Settings, new[] { t20 }));

        Assert.Equal(new[]
        {
            new GameTime(2027, 6, 10, 18),
            new GameTime(2027, 6, 10, 20),
            new GameTime(2027, 6, 10, 22),
            new GameTime(2027, 6, 11, 7),
        }, Walk(rules, new GameTime(2027, 6, 10, 8), 4));
    }

    private static GameTime[] Walk(PaceRules rules, GameTime from, int steps)
    {
        var points = new GameTime[steps];
        var now = from;
        for (var i = 0; i < steps; i++)
        {
            now = rules.NextDecisionPoint(now);
            points[i] = now;
        }
        return points;
    }
}
