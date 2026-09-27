using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests;

public class InspectTests
{
    private static Game NewGame() => new Game(TestContent.Setup(new GameTime(2027, 5, 10, 7)));

    [Fact]
    public void Shows_true_strip_moisture()
    {
        var game = NewGame();
        game.Submit(new WaterStrip(new StripId(3)));
        game.Advance();

        var truth = game.Inspect();

        Assert.Equal(12, truth.Strips.Count);
        Assert.Equal(game.Square.Get(new StripId(3)).SurfaceMoisture, truth.Strips[2].SurfaceMoisture);
        Assert.Equal(game.Square.Get(new StripId(3)).SubsurfaceMoisture, truth.Strips[2].SubsurfaceMoisture);
        Assert.Equal(new StripId(3), truth.Strips[2].Id);
    }

    [Fact]
    public void Shows_the_time_and_the_hour_of_weather_just_gone()
    {
        var game = NewGame();
        Assert.Null(game.Inspect().LastHourWeather);

        game.Advance();
        var truth = game.Inspect();

        Assert.Equal(game.View.Now, truth.Now);
        Assert.Equal(game.Weather.LastHour, truth.LastHourWeather);
    }

    [Fact]
    public void Is_a_snapshot_that_later_play_does_not_change()
    {
        var game = NewGame();
        var before = game.Inspect();
        var moisture = before.Strips[2].SurfaceMoisture;

        game.Submit(new WaterStrip(new StripId(3)));
        game.Advance();

        Assert.Equal(moisture, before.Strips[2].SurfaceMoisture);
    }
}
