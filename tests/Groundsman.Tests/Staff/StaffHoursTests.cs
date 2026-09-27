using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Staff;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Staff;

public class StaffHoursTests
{
    private static readonly DateTime MatchDay = new DateTime(2027, 5, 20);

    private static Game NewGame(GameTime? start = null) =>
        new Game(TestContent.Setup(start ?? new GameTime(2027, 5, 10, 7), new[] { MatchDay }));

    private static double HoursLeft(Game game, StaffId id) => game.View.Staff.Single(s => s.Id == id).HoursLeft;

    [Fact]
    public void Everyone_starts_the_day_with_their_full_hours()
    {
        var staff = NewGame().View.Staff;

        Assert.Equal(new[] { TestStaff.You, TestStaff.Sam, TestStaff.Jo }, staff.Select(s => s.Id));
        Assert.All(staff, s => Assert.Equal(s.HoursPerDay, s.HoursLeft));
        Assert.Equal("Sam Test", staff[1].Name);
    }

    [Fact]
    public void Each_job_costs_its_hours_and_defaults_to_you()
    {
        var game = NewGame();

        game.Submit(new WaterStrip(new StripId(1)));
        game.Submit(new CoverStrip(new StripId(2)));
        game.Submit(new TakeReading(new StripId(3)));

        Assert.Equal(8 - 1 - 0.25 - 0.25, HoursLeft(game, TestStaff.You), 9);
        Assert.Equal(8, HoursLeft(game, TestStaff.Sam));
    }

    [Fact]
    public void A_job_can_be_given_to_someone_else()
    {
        var game = NewGame();

        game.Submit(new WaterStrip(new StripId(1), TestStaff.Sam));
        game.Submit(new TakeReading(new StripId(2), TestStaff.Jo));

        Assert.Equal(8, HoursLeft(game, TestStaff.You));
        Assert.Equal(7, HoursLeft(game, TestStaff.Sam));
        Assert.Equal(3.75, HoursLeft(game, TestStaff.Jo), 9);
    }

    [Fact]
    public void Uncovering_costs_hours()
    {
        var game = NewGame();
        game.Submit(new CoverStrip(new StripId(2)));
        game.Advance();

        game.Submit(new UncoverStrip(new StripId(2), TestStaff.Sam));

        Assert.Equal(7.75, HoursLeft(game, TestStaff.Sam), 9);
    }

    [Fact]
    public void A_job_is_refused_when_the_person_has_too_few_hours_left()
    {
        var game = NewGame();
        for (var n = 1; n <= 4; n++)
        {
            Assert.True(game.Submit(new WaterStrip(new StripId(n), TestStaff.Jo)).Accepted);
        }

        var result = game.Submit(new WaterStrip(new StripId(5), TestStaff.Jo));

        Assert.False(result.Accepted);
        Assert.Contains("Jo Test", result.Reason);
        Assert.False(game.View.Strips[4].WateringQueued);
        Assert.Equal(0, HoursLeft(game, TestStaff.Jo), 9);
    }

    [Fact]
    public void A_refused_job_costs_nothing()
    {
        var game = NewGame();

        game.Submit(new WaterStrip(new StripId(13)));
        game.Submit(new UncoverStrip(new StripId(1)));

        Assert.Equal(8, HoursLeft(game, TestStaff.You));
    }

    [Fact]
    public void Rejects_an_unknown_member_of_staff()
    {
        var result = NewGame().Submit(new WaterStrip(new StripId(1), new StaffId("bob")));

        Assert.False(result.Accepted);
        Assert.Contains("bob", result.Reason);
    }

    [Fact]
    public void Morning_and_afternoon_turns_share_the_days_hours_and_the_next_day_starts_fresh()
    {
        var game = NewGame(new GameTime(2027, 5, 18, 7));
        game.Submit(new WaterStrip(new StripId(1)));

        game.Advance();
        Assert.Equal(new GameTime(2027, 5, 18, 13), game.View.Now);
        Assert.Equal(7, HoursLeft(game, TestStaff.You));

        game.Advance();
        Assert.Equal(new GameTime(2027, 5, 19, 7), game.View.Now);
        Assert.Equal(8, HoursLeft(game, TestStaff.You));
    }

    [Fact]
    public void Readings_record_who_took_them()
    {
        var game = NewGame();

        game.Submit(new TakeReading(new StripId(3), TestStaff.Sam));

        Assert.Equal(TestStaff.Sam, game.View.Strips[2].SurfaceMoisture!.TakenBy);
    }
}
