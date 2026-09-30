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

    [Fact]
    public void Strips_say_whether_they_are_centre_strips()
    {
        var view = new Game(TestContent.Setup(new GameTime(2027, 5, 10, 7))).View;

        Assert.Equal(new[] { 5, 6, 7, 8 }, view.Strips.Where(s => s.Centre).Select(s => s.Id.Number));
    }

    [Fact]
    public void A_feel_reading_also_looks_at_the_ends()
    {
        var game = NewGame(new GameTime(2027, 5, 10, 7));
        Assert.Null(game.View.Strips[2].Ends);

        game.Submit(new TakeReading(new StripId(3), null, Groundsman.Core.Readings.ReadingSource.Feel));

        var look = game.View.Strips[2].Ends!;
        Assert.Equal(EndsState.Established, look.State);
        Assert.Equal(game.View.Now, look.TakenAt);
    }

    [Fact]
    public void A_probe_reading_doesnt_look_at_the_ends()
    {
        var game = NewGame(new GameTime(2027, 5, 10, 7));

        game.Submit(new TakeReading(new StripId(3)));

        Assert.Null(game.View.Strips[2].Ends);
    }

    [Fact]
    public void Strips_say_when_they_were_last_played_on()
    {
        var game = NewGame(new GameTime(2027, 5, 10, 7));
        while (game.View.Now.Date <= MatchDay)
        {
            game.Advance();
        }

        var played = game.View.Strips[0].LastPlayed!;
        Assert.Equal(MatchDay, played.Start);
        Assert.Null(game.View.Strips[1].LastPlayed);
    }

    [Fact]
    public void After_a_match_the_ends_look_worn_and_once_repaired_are_growing_back()
    {
        var game = NewGame(new GameTime(2027, 5, 10, 7));
        while (game.View.Now.Date <= MatchDay)
        {
            game.Advance();
        }
        var strip = new StripId(1);

        game.Submit(new TakeReading(strip, null, Groundsman.Core.Readings.ReadingSource.Feel));
        var before = game.View.Strips[0].Ends!.State;
        game.Submit(new RepairEnds(strip));
        game.Advance();
        game.Submit(new TakeReading(strip, null, Groundsman.Core.Readings.ReadingSource.Feel));

        Assert.Contains(before, new[] { EndsState.Bare, EndsState.Thin });
        Assert.Contains(game.View.Strips[0].Ends!.State, new[] { EndsState.Seeded, EndsState.Thin });
    }
}
