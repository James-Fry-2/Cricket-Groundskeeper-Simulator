using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Readings;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Readings;

public class SoilCoreTests
{
    private static readonly GameTime Start = new GameTime(2027, 5, 10, 7);
    private static readonly StripId Strip3 = new StripId(3);

    private static Game NewGame(ulong seed = 1, bool exact = false) =>
        new Game(TestContent.Setup(Start, seed: seed, readings: exact ? TestContent.ExactReadings : null));

    private static TakeReading Core(StripId strip, Groundsman.Core.Staff.StaffId? by = null) => new TakeReading(strip, by, ReadingSource.SoilCore);

    [Fact]
    public void A_soil_core_reads_moisture_at_depth()
    {
        for (ulong seed = 1; seed <= 100; seed++)
        {
            var game = NewGame(seed, exact: true);

            Assert.True(game.Submit(Core(Strip3)).Accepted);

            var reading = game.View.Strips[2].SubsurfaceMoisture!;
            Assert.Equal(Quantity.SubsurfaceMoisture, reading.Quantity);
            Assert.Equal(ReadingSource.SoilCore, reading.Source);
            Assert.Equal(TestContent.ExactReadings.SoilCoreWidth, reading.Range.Width, 9);
            Assert.True(reading.Range.Contains(game.Square.Get(Strip3).SubsurfaceMoisture));
        }
    }

    [Fact]
    public void A_soil_core_leaves_the_surface_reading_alone()
    {
        var game = NewGame();
        game.Submit(new TakeReading(Strip3));
        var surface = game.View.Strips[2].SurfaceMoisture;

        game.Submit(Core(Strip3));

        Assert.Same(surface, game.View.Strips[2].SurfaceMoisture);
        Assert.NotNull(game.View.Strips[2].SubsurfaceMoisture);
    }

    [Fact]
    public void One_surface_reading_and_one_core_are_allowed_per_strip_per_turn()
    {
        var game = NewGame();

        Assert.True(game.Submit(new TakeReading(Strip3)).Accepted);
        Assert.True(game.Submit(Core(Strip3)).Accepted);
        Assert.False(game.Submit(Core(Strip3)).Accepted);
    }

    [Fact]
    public void A_soil_core_takes_its_hours_and_skill_scales_its_width()
    {
        var game = NewGame();

        game.Submit(Core(Strip3, TestStaff.Sam));

        Assert.Equal(8 - TestStaff.Settings.SoilCoreHours, game.View.Staff[1].HoursLeft, 9);
        Assert.Equal(TestContent.Readings.SoilCoreWidth * 0.8, game.View.Strips[2].SubsurfaceMoisture!.Range.Width, 9);
    }

    [Fact]
    public void Soil_cores_miss_about_as_often_as_content_says()
    {
        var misses = 0;
        var total = 0;
        for (ulong seed = 1; seed <= 400; seed++)
        {
            var game = NewGame(seed);
            for (var n = 1; n <= 8; n++)
            {
                var id = new StripId(n);
                game.Submit(Core(id, n % 2 == 0 ? TestStaff.You : TestStaff.Sam));
                if (n % 2 == 0)
                {
                    total++;
                    misses += game.View.Strips[n - 1].SubsurfaceMoisture!.Range.Contains(game.Square.Get(id).SubsurfaceMoisture) ? 0 : 1;
                }
            }
        }

        Assert.InRange(misses / (double)total, TestContent.Readings.SoilCoreMissRate - 0.025, TestContent.Readings.SoilCoreMissRate + 0.025);
    }

    [Fact]
    public void A_core_ages_like_any_other_reading()
    {
        var game = NewGame(exact: true);
        game.Submit(new CoverStrip(Strip3));
        game.Submit(Core(Strip3));
        var taken = game.View.Strips[2].SubsurfaceMoisture!.Range;

        game.Advance();
        game.Advance();

        Assert.Equal(taken.Width + 2 * TestContent.Readings.WidenPerDay, game.View.Strips[2].SubsurfaceMoistureNow!.Value.Width, 9);
    }
}
