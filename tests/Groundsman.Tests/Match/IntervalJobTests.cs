using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Match;

public class IntervalJobTests
{
    private static readonly StripId MatchStrip = new StripId(6);

    private static Game GameAt(FormatSettings format, GameTime until)
    {
        var fixture = new Fixture(new DateTime(2027, 6, 10), format, MatchStrip, TestTeams.Opponent);
        var game = new Game(new GameSetup(TestContent.Content, new GameTime(2027, 6, 1, 7), new[] { fixture }, 1));
        while (game.View.Now < until)
        {
            game.Advance();
        }
        Assert.Equal(until, game.View.Now);
        return game;
    }

    [Fact]
    public void Footholes_can_be_cleaned_at_a_break_in_any_format_for_a_small_gain()
    {
        var game = GameAt(TestFormats.OneDay, new GameTime(2027, 6, 10, 13));
        game.Square.Get(MatchStrip).Footholes = 0.3;

        Assert.True(game.Submit(new CleanFootholes(MatchStrip)).Accepted);
        game.Wear.RunHour(game.View.Now);

        Assert.True(game.Square.Get(MatchStrip).Footholes <= 0.3 * (1 - TestWear.Settings.CleanShare) + 1e-9);
    }

    [Fact]
    public void Footholes_can_be_filled_at_close_of_play_in_a_multi_day_match()
    {
        var game = GameAt(TestFormats.FourDay, new GameTime(2027, 6, 11, 18));
        game.Square.Get(MatchStrip).Footholes = 0.5;

        Assert.True(game.Submit(new FillFootholes(MatchStrip)).Accepted);
        game.Wear.RunHour(game.View.Now);

        Assert.True(game.Square.Get(MatchStrip).Footholes <= 0.5 * (1 - TestWear.Settings.FillShare) + 1e-9);
    }

    [Fact]
    public void Filling_is_refused_during_play_breaks_and_in_one_day_cricket()
    {
        Assert.False(GameAt(TestFormats.FourDay, new GameTime(2027, 6, 11, 13)).Submit(new FillFootholes(MatchStrip)).Accepted);
        Assert.False(GameAt(TestFormats.FourDay, new GameTime(2027, 6, 13, 18)).Submit(new FillFootholes(MatchStrip)).Accepted);
        Assert.False(GameAt(TestFormats.OneDay, new GameTime(2027, 6, 10, 18)).Submit(new FillFootholes(MatchStrip)).Accepted);
    }

    [Fact]
    public void Cleaning_and_filling_are_only_for_the_match_strip_during_a_match()
    {
        var before = GameAt(TestFormats.FourDay, new GameTime(2027, 6, 9, 7));
        Assert.False(before.Submit(new CleanFootholes(MatchStrip)).Accepted);

        var during = GameAt(TestFormats.FourDay, new GameTime(2027, 6, 11, 18));
        Assert.False(during.Submit(new CleanFootholes(new StripId(5))).Accepted);
        Assert.False(during.Submit(new FillFootholes(new StripId(5))).Accepted);
    }

    [Fact]
    public void Full_end_repairs_and_cover_orders_wait_until_the_match_is_over()
    {
        var game = GameAt(TestFormats.FourDay, new GameTime(2027, 6, 11, 18));

        var repair = game.Submit(new RepairEnds(MatchStrip));
        var cover = game.Submit(new CoverStrip(MatchStrip));

        Assert.False(repair.Accepted);
        Assert.Contains("Law 9", repair.Reason);
        Assert.False(cover.Accepted);
        Assert.True(game.Submit(new RepairEnds(new StripId(5))).Accepted);
    }

    [Fact]
    public void Interval_jobs_cost_hours()
    {
        var game = GameAt(TestFormats.FourDay, new GameTime(2027, 6, 11, 18));

        game.Submit(new CleanFootholes(MatchStrip, TestStaff.Sam));
        game.Submit(new FillFootholes(MatchStrip, TestStaff.Sam));

        Assert.Equal(8 - TestStaff.Settings.CleanFootholesHours - TestStaff.Settings.FillFootholesHours, game.View.Staff[1].HoursLeft, 9);
    }
}
