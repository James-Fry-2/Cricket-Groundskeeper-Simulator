using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Content;
using Groundsman.Core.Readings;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Readings;

public class TakeReadingTests
{
    private static readonly GameTime Start = new GameTime(2027, 5, 10, 7);
    private static readonly StripId Strip3 = new StripId(3);

    private static Game NewGame(ulong seed = 1, ReadingSettings? readings = null) =>
        new Game(TestContent.Setup(Start, seed: seed, readings: readings));

    private static StripView ViewOf(Game game, StripId strip) => game.View.Strips[strip.Number - 1];

    private static Reading? LatestFor(Game game, StripId strip) => ViewOf(game, strip).SurfaceMoisture;

    [Fact]
    public void Strips_show_no_reading_before_one_is_taken()
    {
        var view = NewGame().View;

        Assert.Equal(12, view.Strips.Count);
        Assert.All(view.Strips, strip => Assert.Null(strip.SurfaceMoisture));
        Assert.All(view.Strips, strip => Assert.Null(strip.SurfaceMoistureNow));
    }

    [Fact]
    public void A_probe_reading_appears_in_the_view_straight_away()
    {
        var game = NewGame();

        var result = game.Submit(new TakeReading(Strip3));
        var reading = LatestFor(game, Strip3);

        Assert.True(result.Accepted);
        Assert.NotNull(reading);
        Assert.Equal(Strip3, reading!.Strip);
        Assert.Equal(Quantity.SurfaceMoisture, reading.Quantity);
        Assert.Equal(ReadingSource.MoistureProbe, reading.Source);
        Assert.Null(reading.Word);
        Assert.Equal(Start, reading.TakenAt);
        Assert.Equal(TestContent.Readings.MoistureProbeWidth, reading.Range.Width, 9);
        Assert.Equal(reading.Range, ViewOf(game, Strip3).SurfaceMoistureNow);
    }

    [Fact]
    public void Without_misses_the_range_always_contains_the_true_value()
    {
        for (ulong seed = 1; seed <= 200; seed++)
        {
            var game = NewGame(seed, TestContent.ExactReadings);
            foreach (var strip in game.Square.Strips)
            {
                game.Submit(new TakeReading(strip.Id));

                Assert.True(LatestFor(game, strip.Id)!.Range.Contains(strip.SurfaceMoisture), $"seed {seed}, {strip.Id}");
            }
        }
    }

    [Fact]
    public void The_true_value_sits_at_a_random_point_in_the_range()
    {
        var offsets = new HashSet<double>();
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var game = NewGame(seed, TestContent.ExactReadings);
            game.Submit(new TakeReading(Strip3));

            offsets.Add(Math.Round(game.Square.Get(Strip3).SurfaceMoisture - LatestFor(game, Strip3)!.Range.Low, 6));
        }

        Assert.True(offsets.Count > 10, "The range's position should not be fixed relative to the true value.");
    }

    [Fact]
    public void The_same_seed_gives_the_same_reading()
    {
        var a = NewGame(42);
        var b = NewGame(42);

        a.Submit(new TakeReading(Strip3));
        b.Submit(new TakeReading(Strip3));

        Assert.Equal(LatestFor(a, Strip3)!.Range, LatestFor(b, Strip3)!.Range);
    }

    [Fact]
    public void The_view_keeps_the_old_reading_after_watering_until_a_new_one_is_taken()
    {
        var game = NewGame(readings: TestContent.ExactReadings);
        game.Submit(new TakeReading(Strip3));
        var before = LatestFor(game, Strip3);

        game.Submit(new WaterStrip(Strip3));
        game.Advance();

        Assert.Same(before, LatestFor(game, Strip3));

        game.Submit(new TakeReading(Strip3));

        Assert.NotSame(before, LatestFor(game, Strip3));
        Assert.True(LatestFor(game, Strip3)!.Range.Contains(game.Square.Get(Strip3).SurfaceMoisture));
    }

    [Fact]
    public void Rejects_an_unknown_strip()
    {
        var result = NewGame().Submit(new TakeReading(new StripId(13)));

        Assert.False(result.Accepted);
        Assert.Contains("Strip 13", result.Reason);
    }

    [Fact]
    public void Rejects_a_second_reading_of_a_strip_in_one_turn_of_either_kind()
    {
        var game = NewGame();
        game.Submit(new TakeReading(Strip3));
        var first = LatestFor(game, Strip3);

        Assert.False(game.Submit(new TakeReading(Strip3)).Accepted);
        Assert.False(game.Submit(new TakeReading(Strip3, tool: ReadingSource.Feel)).Accepted);
        Assert.Same(first, LatestFor(game, Strip3));
    }

    [Fact]
    public void A_strip_can_be_read_again_next_turn()
    {
        var game = NewGame();
        game.Submit(new TakeReading(Strip3));
        game.Advance();

        Assert.True(game.Submit(new TakeReading(Strip3)).Accepted);
        Assert.Equal(game.View.Now, LatestFor(game, Strip3)!.TakenAt);
    }

    [Fact]
    public void View_shows_the_ground_name()
    {
        Assert.Equal(TestGround.Settings.Name, NewGame().View.GroundName);
    }
}
