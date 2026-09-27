using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Moisture;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Commands;

public class WaterStripTests
{
    private static readonly StripId Strip3 = new StripId(3);

    private static Game NewGame(ulong seed = 1) => new Game(TestContent.Setup(new GameTime(2027, 5, 10, 7), seed: seed));

    private static double WaterMm(Game game, StripId id)
    {
        var strip = game.Square.Get(id);
        return strip.SurfaceMoisture / 100 * TestMoisture.Settings.SurfaceDepthMm
            + strip.SubsurfaceMoisture / 100 * TestMoisture.Settings.SubsurfaceDepthMm;
    }

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
    public void Watering_goes_on_in_the_first_hour_of_the_advance()
    {
        var watered = NewGame();
        var dry = NewGame();
        var model = new MoistureModel(TestMoisture.Settings);
        var expected = new StripState(Strip3, TestLoams.Standard, dry.Square.Get(Strip3).SurfaceMoisture, dry.Square.Get(Strip3).SubsurfaceMoisture);

        watered.Submit(new WaterStrip(Strip3));
        watered.Weather.RunHour(watered.View.Now);
        model.RunHour(expected, watered.Weather.LastHour!.Value, covered: false, wateringMm: TestContent.Tasks.WaterMm);
        watered.Moisture.RunHour(watered.View.Now);

        Assert.Equal(expected.SurfaceMoisture, watered.Square.Get(Strip3).SurfaceMoisture, 9);
        Assert.Equal(expected.SubsurfaceMoisture, watered.Square.Get(Strip3).SubsurfaceMoisture, 9);
    }

    [Fact]
    public void A_watered_strip_holds_more_water_than_the_same_strip_left_alone_unless_rain_filled_it()
    {
        var dryDays = 0;
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var watered = NewGame(seed);
            var dry = NewGame(seed);

            watered.Submit(new WaterStrip(Strip3));
            watered.Advance();
            dry.Advance();

            // Heavy rain can saturate both layers, and then the watering just runs off.
            Assert.True(WaterMm(watered, Strip3) >= WaterMm(dry, Strip3) - 1e-9);
            if (watered.View.Weather!.RainLast24HoursMm < 1)
            {
                dryDays++;
                Assert.True(WaterMm(watered, Strip3) > WaterMm(dry, Strip3), $"seed {seed}");
            }
            Assert.Equal(WaterMm(dry, new StripId(4)), WaterMm(watered, new StripId(4)), 9);
        }

        Assert.True(dryDays >= 5, "Too few dry days to test the ordinary case");
    }

    [Fact]
    public void Watering_applies_once_per_order()
    {
        var game = NewGame();

        game.Submit(new WaterStrip(Strip3));
        game.Advance();

        Assert.False(game.View.Strips[2].WateringQueued);
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

        game.Submit(new WaterStrip(Strip3));
        var second = game.Submit(new WaterStrip(Strip3));

        Assert.False(second.Accepted);
        Assert.Contains("already", second.Reason);
    }
}
