using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Match;

public class MatchDayViewTests
{
    private static readonly StripId MatchStrip = new StripId(6);

    private static Game GameAt(GameTime until, params Fixture[] fixtures)
    {
        var game = new Game(new GameSetup(TestContent.Content, new GameTime(2027, 6, 1, 7), fixtures, 1));
        while (game.View.Now < until)
        {
            game.Advance();
        }
        Assert.Equal(until, game.View.Now);
        return game;
    }

    private static Fixture FourDay(int day) => new Fixture(new DateTime(2027, 6, day), TestFormats.FourDay, MatchStrip, TestTeams.Opponent);

    [Fact]
    public void There_is_no_interval_away_from_a_match()
    {
        Assert.Null(GameAt(new GameTime(2027, 6, 9, 7), FourDay(10)).View.Interval);
    }

    [Fact]
    public void The_morning_of_a_match_day_is_before_play_with_cleaning_allowed()
    {
        var interval = GameAt(new GameTime(2027, 6, 11, 8), FourDay(10)).View.Interval;

        Assert.NotNull(interval);
        Assert.Equal("Before play", interval!.Name);
        Assert.Equal(MatchStrip, interval.Strip);
        Assert.True(interval.CanClean);
        Assert.False(interval.CanFill);
    }

    [Fact]
    public void Breaks_are_named_from_the_format()
    {
        Assert.Equal("Lunch", GameAt(new GameTime(2027, 6, 11, 13), FourDay(10)).View.Interval!.Name);
        Assert.Equal("Tea", GameAt(new GameTime(2027, 6, 11, 16), FourDay(10)).View.Interval!.Name);
    }

    [Fact]
    public void Filling_is_offered_at_stumps_with_more_cricket_to_come_only()
    {
        var stumps = GameAt(new GameTime(2027, 6, 11, 18), FourDay(10)).View.Interval!;
        Assert.Equal("Stumps", stumps.Name);
        Assert.True(stumps.CanFill);

        Assert.Null(GameAt(new GameTime(2027, 6, 13, 18), FourDay(10)).View.Interval);
    }

    [Fact]
    public void Every_match_played_is_kept_oldest_first()
    {
        var first = new Fixture(new DateTime(2027, 6, 3), TestFormats.OneDay, new StripId(2), TestTeams.Opponent);
        var second = new Fixture(new DateTime(2027, 6, 6), TestFormats.OneDay, new StripId(3), TestTeams.Opponent);

        var view = GameAt(new GameTime(2027, 6, 8, 7), first, second).View;

        Assert.Equal(new[] { first, second }, view.Matches.Select(m => m.Fixture));
        Assert.All(view.Matches, m => Assert.True(m.Finished));
        Assert.Same(view.Matches[1].Fixture, view.LatestMatch!.Fixture);
    }
}
