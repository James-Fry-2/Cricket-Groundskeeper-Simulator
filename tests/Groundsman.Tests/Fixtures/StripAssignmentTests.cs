using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Fixtures;

public class StripAssignmentTests
{
    private static readonly Fixture Open = new Fixture(new DateTime(2027, 6, 20), TestFormats.OneDay, null, TestTeams.Opponent);

    private static Game GameAt(GameTime start, params Fixture[] fixtures) =>
        new Game(new GameSetup(TestContent.Content, start, fixtures.Length == 0 ? new[] { Open } : fixtures, 1));

    private static void AdvanceTo(Game game, DateTime date)
    {
        while (game.View.Now.Date < date)
        {
            game.Advance();
        }
    }

    private static FixtureView ViewOf(Game game, Fixture fixture) => game.View.Fixtures.Single(f => f.Fixture == fixture);

    [Fact]
    public void A_fixture_is_known_by_its_start_date()
    {
        Assert.Equal("2027-06-20", Open.Id);
    }

    [Fact]
    public void A_fixture_without_a_strip_starts_unassigned_and_locks_ten_days_out()
    {
        var view = ViewOf(GameAt(new GameTime(2027, 6, 1, 7)), Open);

        Assert.Null(view.Strip);
        Assert.False(view.Locked);
        Assert.Equal(new DateTime(2027, 6, 10), view.LocksOn);
    }

    [Fact]
    public void A_strip_can_be_assigned_and_changed_before_the_lock_at_no_cost_in_hours()
    {
        var game = GameAt(new GameTime(2027, 6, 1, 7));

        Assert.True(game.Submit(new AssignStrip(Open.Id, new StripId(4))).Accepted);
        Assert.Equal(new StripId(4), ViewOf(game, Open).Strip);
        Assert.True(game.Submit(new AssignStrip(Open.Id, new StripId(7))).Accepted);
        Assert.Equal(new StripId(7), ViewOf(game, Open).Strip);
        Assert.All(game.View.Staff, s => Assert.Equal(s.HoursPerDay, s.HoursLeft));
    }

    [Fact]
    public void Assignment_is_refused_for_an_unknown_fixture_or_strip()
    {
        var game = GameAt(new GameTime(2027, 6, 1, 7));

        Assert.False(game.Submit(new AssignStrip("2027-06-21", new StripId(4))).Accepted);
        Assert.False(game.Submit(new AssignStrip(Open.Id, new StripId(13))).Accepted);
        Assert.Null(ViewOf(game, Open).Strip);
    }

    [Fact]
    public void Assignment_is_refused_once_the_strip_is_locked()
    {
        var game = GameAt(new GameTime(2027, 6, 1, 7));
        game.Submit(new AssignStrip(Open.Id, new StripId(4)));
        AdvanceTo(game, new DateTime(2027, 6, 10));

        var result = game.Submit(new AssignStrip(Open.Id, new StripId(5)));

        Assert.True(ViewOf(game, Open).Locked);
        Assert.False(result.Accepted);
        Assert.Contains("locked", result.Reason);
        Assert.Equal(new StripId(4), ViewOf(game, Open).Strip);
    }

    [Fact]
    public void A_strip_cant_be_booked_for_fixtures_whose_build_ups_overlap()
    {
        var close = new Fixture(new DateTime(2027, 6, 26), TestFormats.OneDay, null, TestTeams.Opponent);
        var later = new Fixture(new DateTime(2027, 7, 10), TestFormats.OneDay, null, TestTeams.Opponent);
        var game = GameAt(new GameTime(2027, 6, 1, 7), Open, close, later);

        Assert.True(game.Submit(new AssignStrip(Open.Id, new StripId(4))).Accepted);
        var clash = game.Submit(new AssignStrip(close.Id, new StripId(4)));
        Assert.False(clash.Accepted);
        Assert.Contains(Open.Id, clash.Reason);
        Assert.True(game.Submit(new AssignStrip(later.Id, new StripId(4))).Accepted);
    }

    [Fact]
    public void At_the_lock_an_unassigned_fixture_gets_the_longest_rested_free_strip_with_a_notice()
    {
        var earlier = new Fixture(new DateTime(2027, 6, 3), TestFormats.OneDay, new StripId(1), TestTeams.Opponent);
        var game = GameAt(new GameTime(2027, 6, 1, 7), earlier, Open);
        AdvanceTo(game, new DateTime(2027, 6, 10));

        Assert.Equal(new StripId(2), ViewOf(game, Open).Strip);
        var notice = Assert.IsType<StripLockedNotice>(Assert.Single(game.View.Notices));
        Assert.Same(Open, notice.Fixture.Fixture);
        Assert.True(notice.ByDefault);

        game.Advance();
        Assert.Empty(game.View.Notices);
    }

    [Fact]
    public void The_default_prefers_a_strip_rested_longer()
    {
        var fixtures = Enumerable.Range(1, 12)
            .Select(n => new Fixture(new DateTime(2027, 4, 1).AddDays(3 * n), TestFormats.OneDay, new StripId(13 - n), TestTeams.Opponent))
            .Append(Open)
            .ToArray();
        var game = GameAt(new GameTime(2027, 4, 1, 7), fixtures);
        AdvanceTo(game, new DateTime(2027, 6, 10));

        Assert.Equal(new StripId(12), ViewOf(game, Open).Strip);
    }

    [Fact]
    public void An_assigned_fixture_locks_with_a_notice_that_isnt_a_default()
    {
        var game = GameAt(new GameTime(2027, 6, 1, 7));
        game.Submit(new AssignStrip(Open.Id, new StripId(9)));
        AdvanceTo(game, new DateTime(2027, 6, 10));

        var notice = Assert.IsType<StripLockedNotice>(Assert.Single(game.View.Notices));
        Assert.False(notice.ByDefault);
        Assert.Equal(new StripId(9), notice.Fixture.Strip);
    }

    [Fact]
    public void A_fixture_already_inside_its_lock_at_the_start_is_locked_from_the_start()
    {
        var game = GameAt(new GameTime(2027, 6, 15, 7));

        Assert.True(ViewOf(game, Open).Locked);
        Assert.NotNull(ViewOf(game, Open).Strip);
        Assert.Single(game.View.Notices);
    }

    [Fact]
    public void A_preset_strip_starts_as_the_assignment_and_can_be_changed()
    {
        var preset = new Fixture(new DateTime(2027, 6, 20), TestFormats.OneDay, new StripId(6), TestTeams.Opponent);
        var game = GameAt(new GameTime(2027, 6, 1, 7), preset);

        Assert.Equal(new StripId(6), ViewOf(game, preset).Strip);
        Assert.True(game.Submit(new AssignStrip(preset.Id, new StripId(3))).Accepted);
    }

    [Fact]
    public void The_match_is_played_on_the_assigned_strip()
    {
        var game = GameAt(new GameTime(2027, 6, 1, 7));
        game.Submit(new AssignStrip(Open.Id, new StripId(9)));
        AdvanceTo(game, new DateTime(2027, 6, 21));

        Assert.Equal(new StripId(9), game.View.LatestMatch!.Strip);
        Assert.True(game.Inspect().Strips[8].Footholes > 0);
        Assert.Equal(0, game.Inspect().Strips[5].Footholes);
    }
}
