using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests;

public class GameViewTests
{
    private static readonly DateTime MatchDay = new DateTime(2027, 5, 20);

    private static Game NewGame(GameTime start) => new Game(TestContent.Setup(start, new[] { MatchDay }));

    [Theory]
    [InlineData(2027, 3, 1, DayPace.OffSeason)]
    [InlineData(2027, 5, 10, DayPace.InSeason)]
    [InlineData(2027, 5, 18, DayPace.FinalPrep)]
    [InlineData(2027, 5, 20, DayPace.MatchDay)]
    public void Shows_the_pace_of_the_current_day(int year, int month, int day, DayPace expected)
    {
        Assert.Equal(expected, NewGame(new GameTime(year, month, day, 7)).View.Pace);
    }

    [Fact]
    public void Shows_the_next_match_day_including_today()
    {
        Assert.Equal(MatchDay, NewGame(new GameTime(2027, 5, 10, 7)).View.NextMatchDay);
        Assert.Equal(MatchDay, NewGame(new GameTime(2027, 5, 20, 13)).View.NextMatchDay);
        Assert.Null(NewGame(new GameTime(2027, 5, 21, 7)).View.NextMatchDay);
    }

    [Fact]
    public void Shows_which_strips_are_down_for_watering_until_it_is_done()
    {
        var game = NewGame(new GameTime(2027, 5, 10, 7));

        game.Submit(new WaterStrip(new StripId(3)));

        Assert.True(game.View.Strips[2].WateringQueued);
        Assert.False(game.View.Strips[3].WateringQueued);

        game.Advance();

        Assert.False(game.View.Strips[2].WateringQueued);
    }
}
