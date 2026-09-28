using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Commands;

public class MowStripTests
{
    private static readonly StripId Strip3 = new StripId(3);

    private static Game NewGame() => new Game(TestContent.Setup(new GameTime(2027, 5, 10, 7)));

    [Fact]
    public void The_cut_happens_in_the_first_hour_of_the_next_advance()
    {
        var game = NewGame();

        Assert.True(game.Submit(new MowStrip(Strip3, 10)).Accepted);
        Assert.Equal(15, game.Square.Get(Strip3).GrassHeightMm);

        game.Weather.RunHour(game.View.Now);
        game.Grass.RunHour(game.View.Now);

        Assert.InRange(game.Square.Get(Strip3).GrassHeightMm, 10, 10.1);
    }

    [Fact]
    public void The_view_shows_the_mowing_record_not_the_true_height()
    {
        var game = NewGame();
        Assert.Null(game.View.Strips[2].LastMown);

        game.Submit(new MowStrip(Strip3, 10));
        game.Advance();

        var record = game.View.Strips[2].LastMown!;
        Assert.Equal(10, record.HeightMm);
        Assert.Equal(new GameTime(2027, 5, 10, 7), record.OrderedAt);
    }

    [Fact]
    public void Mowing_costs_hours()
    {
        var game = NewGame();

        game.Submit(new MowStrip(Strip3, 10));

        Assert.Equal(8 - TestStaff.Settings.MowHours, game.View.Staff[0].HoursLeft, 9);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(51)]
    public void Rejects_a_height_the_mower_cant_set(double height)
    {
        var result = NewGame().Submit(new MowStrip(Strip3, height));

        Assert.False(result.Accepted);
        Assert.Contains("mm", result.Reason);
    }

    [Fact]
    public void Rejects_mowing_a_strip_twice_in_one_turn_or_an_unknown_strip()
    {
        var game = NewGame();
        game.Submit(new MowStrip(Strip3, 10));

        Assert.False(game.Submit(new MowStrip(Strip3, 8)).Accepted);
        Assert.False(game.Submit(new MowStrip(new StripId(13), 8)).Accepted);
    }
}
