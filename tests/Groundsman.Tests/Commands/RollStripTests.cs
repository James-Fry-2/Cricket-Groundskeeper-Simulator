using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Commands;

public class RollStripTests
{
    private static readonly StripId Strip3 = new StripId(3);

    private static Game NewGame() => new Game(TestContent.Setup(new GameTime(2027, 5, 10, 7)));

    [Fact]
    public void Rolling_happens_in_the_first_hour_of_the_next_advance()
    {
        var game = NewGame();
        var before = game.Square.Get(Strip3).Compaction;

        Assert.True(game.Submit(new RollStrip(Strip3, "medium", 30)).Accepted);
        Assert.Equal(before, game.Square.Get(Strip3).Compaction);

        game.Advance();

        Assert.NotEqual(before, game.Square.Get(Strip3).Compaction);
    }

    [Fact]
    public void Rolling_costs_the_minutes_and_shows_as_a_record()
    {
        var game = NewGame();

        game.Submit(new RollStrip(Strip3, "heavy", 30, TestStaff.Sam));

        Assert.Equal(7.5, game.View.Staff[1].HoursLeft, 9);
        var record = game.View.Strips[2].LastRolled!;
        Assert.Equal("heavy", record.RollerId);
        Assert.Equal(30, record.Minutes);
        Assert.Equal(new GameTime(2027, 5, 10, 7), record.OrderedAt);
        Assert.True(game.View.Strips[2].RollingQueued);
    }

    [Fact]
    public void Rejects_an_unknown_roller_a_bad_time_a_repeat_or_an_unknown_strip()
    {
        var game = NewGame();

        Assert.False(game.Submit(new RollStrip(Strip3, "steamroller", 30)).Accepted);
        Assert.False(game.Submit(new RollStrip(Strip3, "light", 2)).Accepted);
        Assert.False(game.Submit(new RollStrip(Strip3, "light", 121)).Accepted);
        Assert.False(game.Submit(new RollStrip(new StripId(13), "light", 30)).Accepted);
        Assert.True(game.Submit(new RollStrip(Strip3, "light", 30)).Accepted);
        Assert.False(game.Submit(new RollStrip(Strip3, "heavy", 30)).Accepted);
    }
}
