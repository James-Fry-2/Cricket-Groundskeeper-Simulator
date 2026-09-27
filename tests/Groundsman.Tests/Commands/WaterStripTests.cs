using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Commands;

public class WaterStripTests
{
    private static readonly StripId Strip3 = new StripId(3);

    private static Game NewGame() => new Game(TestContent.Setup(new GameTime(2027, 5, 10, 7)));

    [Fact]
    public void Watering_waits_for_the_next_advance()
    {
        var game = NewGame();
        var before = game.Square.Get(Strip3).SurfaceMoisture;

        var result = game.Submit(new WaterStrip(Strip3));

        Assert.True(result.Accepted);
        Assert.Equal(before, game.Square.Get(Strip3).SurfaceMoisture);
    }

    [Fact]
    public void Watering_raises_surface_moisture_by_the_content_amount()
    {
        var game = NewGame();
        var before = game.Square.Get(Strip3).SurfaceMoisture;

        game.Submit(new WaterStrip(Strip3));
        game.Advance();

        Assert.Equal(before + TestContent.Tasks.WaterSurfaceGain, game.Square.Get(Strip3).SurfaceMoisture);
    }

    [Fact]
    public void Watering_only_touches_the_chosen_strip_surface()
    {
        var game = NewGame();
        var otherBefore = game.Square.Get(new StripId(4)).SurfaceMoisture;
        var subsurfaceBefore = game.Square.Get(Strip3).SubsurfaceMoisture;

        game.Submit(new WaterStrip(Strip3));
        game.Advance();

        Assert.Equal(otherBefore, game.Square.Get(new StripId(4)).SurfaceMoisture);
        Assert.Equal(subsurfaceBefore, game.Square.Get(Strip3).SubsurfaceMoisture);
    }

    [Fact]
    public void Watering_never_takes_moisture_past_saturation()
    {
        var game = NewGame();
        game.Square.Get(Strip3).SurfaceMoisture = game.Square.Saturation - 1;

        game.Submit(new WaterStrip(Strip3));
        game.Advance();

        Assert.Equal(game.Square.Saturation, game.Square.Get(Strip3).SurfaceMoisture);
    }

    [Fact]
    public void Watering_applies_once_per_order()
    {
        var game = NewGame();
        var before = game.Square.Get(Strip3).SurfaceMoisture;

        game.Submit(new WaterStrip(Strip3));
        game.Advance();
        game.Advance();

        Assert.Equal(before + TestContent.Tasks.WaterSurfaceGain, game.Square.Get(Strip3).SurfaceMoisture);
    }

    [Fact]
    public void Rejects_an_unknown_strip()
    {
        var game = NewGame();

        var result = game.Submit(new WaterStrip(new StripId(13)));

        Assert.False(result.Accepted);
        Assert.Contains("Strip 13", result.Reason);
    }

    [Fact]
    public void Rejects_watering_a_strip_twice_in_one_turn()
    {
        var game = NewGame();
        var before = game.Square.Get(Strip3).SurfaceMoisture;

        game.Submit(new WaterStrip(Strip3));
        var second = game.Submit(new WaterStrip(Strip3));
        game.Advance();

        Assert.False(second.Accepted);
        Assert.Equal(before + TestContent.Tasks.WaterSurfaceGain, game.Square.Get(Strip3).SurfaceMoisture);
    }
}
