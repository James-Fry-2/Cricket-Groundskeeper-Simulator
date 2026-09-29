using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Commands;

public class RepairEndsTests
{
    private static readonly StripId Strip3 = new StripId(3);

    private static Game NewGame() => new Game(TestContent.Setup(new GameTime(2027, 5, 10, 7)));

    [Fact]
    public void Repairs_are_done_in_the_next_hour_and_fill_the_footholes()
    {
        var game = NewGame();
        game.Square.Get(Strip3).Footholes = 0.5;

        Assert.True(game.Submit(new RepairEnds(Strip3)).Accepted);
        Assert.Equal(0.5, game.Square.Get(Strip3).Footholes);

        game.Advance();

        Assert.True(game.Square.Get(Strip3).Footholes <= 0.5 * (1 - TestWear.Settings.RepairFills));
        Assert.True(game.Square.Get(Strip3).EndsRepaired);
    }

    [Fact]
    public void Repairs_cost_hours_and_show_as_a_record()
    {
        var game = NewGame();

        game.Submit(new RepairEnds(Strip3, TestStaff.Sam));

        Assert.Equal(8 - TestStaff.Settings.RepairEndsHours, game.View.Staff[1].HoursLeft, 9);
        Assert.True(game.View.Strips[2].RepairQueued);
        Assert.Equal(new GameTime(2027, 5, 10, 7), game.View.Strips[2].LastRepaired);
    }

    [Fact]
    public void Rejects_a_repeat_or_an_unknown_strip()
    {
        var game = NewGame();
        game.Submit(new RepairEnds(Strip3));

        Assert.False(game.Submit(new RepairEnds(Strip3)).Accepted);
        Assert.False(game.Submit(new RepairEnds(new StripId(13))).Accepted);
    }

    [Fact]
    public void Cracks_form_each_hour_on_a_dry_covered_strip()
    {
        var game = NewGame();
        game.Submit(new CoverStrip(Strip3));
        game.Square.Get(Strip3).SurfaceMoisture = TestLoams.Standard.AirDry + 1;

        game.Advance();

        Assert.True(game.Square.Get(Strip3).Cracks > 0);
        Assert.True(game.Inspect().Strips[2].Pitch.Cracking > 0);
    }
}
