using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Readings;

public class ReadingAgeingTests
{
    private static readonly StripId Strip3 = new StripId(3);

    private static Game NewGame(ulong seed = 1) =>
        new Game(TestContent.Setup(new GameTime(2027, 5, 10, 7), seed: seed, readings: TestContent.ExactReadings));

    private static StripView ViewOf(Game game) => game.View.Strips[Strip3.Number - 1];

    [Fact]
    public void A_reading_widens_each_day_by_the_content_rate_when_no_water_reaches_the_strip()
    {
        var game = NewGame();
        game.Submit(new CoverStrip(Strip3));
        game.Submit(new TakeReading(Strip3));
        var taken = ViewOf(game).SurfaceMoisture!.Range;

        game.Advance();
        game.Advance();
        game.Advance();

        var now = ViewOf(game).SurfaceMoistureNow!.Value;
        Assert.Equal(taken.Width + 3 * TestContent.Readings.WidenPerDay, now.Width, 9);
        Assert.Equal(taken.Low - 1.5 * TestContent.Readings.WidenPerDay, now.Low, 9);
    }

    [Fact]
    public void Watering_widens_a_reading_by_the_water_it_put_on()
    {
        var game = NewGame();
        game.Submit(new CoverStrip(Strip3));
        game.Submit(new TakeReading(Strip3));
        var taken = ViewOf(game).SurfaceMoisture!.Range;

        game.Submit(new WaterStrip(Strip3));
        game.Advance();

        var expected = taken.Width + TestContent.Readings.WidenPerDay + TestContent.Tasks.WaterMm * TestContent.Readings.WidenPerMmWater;
        Assert.Equal(expected, ViewOf(game).SurfaceMoistureNow!.Value.Width, 9);
    }

    [Fact]
    public void Rain_on_an_open_strip_widens_its_reading_but_not_a_covered_ones()
    {
        var wetDays = 0;
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var game = NewGame(seed);
            game.Submit(new CoverStrip(new StripId(4)));
            game.Submit(new TakeReading(Strip3));
            game.Submit(new TakeReading(new StripId(4)));

            game.Advance();
            var rain = game.View.Weather!.RainLast24HoursMm;
            if (rain < 1)
            {
                continue;
            }

            wetDays++;
            var open = game.View.Strips[2];
            var covered = game.View.Strips[3];
            Assert.Equal(open.SurfaceMoisture!.Range.Width + TestContent.Readings.WidenPerDay + rain * TestContent.Readings.WidenPerMmWater, open.SurfaceMoistureNow!.Value.Width, 6);
            Assert.Equal(covered.SurfaceMoisture!.Range.Width + TestContent.Readings.WidenPerDay, covered.SurfaceMoistureNow!.Value.Width, 6);
        }

        Assert.True(wetDays >= 3);
    }

    [Fact]
    public void A_widened_range_never_goes_below_zero()
    {
        var game = NewGame();
        game.Square.Get(Strip3).SurfaceMoisture = 1;
        game.Submit(new TakeReading(Strip3));

        for (var i = 0; i < 10; i++)
        {
            game.Advance();
        }

        Assert.True(ViewOf(game).SurfaceMoistureNow!.Value.Low >= 0);
    }

    [Fact]
    public void A_new_reading_starts_fresh()
    {
        var game = NewGame();
        game.Submit(new TakeReading(Strip3));
        game.Advance();
        game.Advance();

        game.Submit(new TakeReading(Strip3));

        Assert.Equal(ViewOf(game).SurfaceMoisture!.Range, ViewOf(game).SurfaceMoistureNow);
    }
}
