using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Readings;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Readings;

public class ReadingQualityTests
{
    private static readonly GameTime Start = new GameTime(2027, 5, 10, 7);

    private static Game NewGame(ulong seed) => new Game(TestContent.Setup(Start, seed: seed));

    [Fact]
    public void Skilled_staff_read_tighter_and_casual_staff_looser()
    {
        var game = NewGame(1);

        game.Submit(new TakeReading(new StripId(1), TestStaff.You));
        game.Submit(new TakeReading(new StripId(2), TestStaff.Sam));
        game.Submit(new TakeReading(new StripId(3), TestStaff.Jo));

        var width = TestContent.Readings.MoistureProbeWidth;
        Assert.Equal(width * 1.0, game.View.Strips[0].SurfaceMoisture!.Range.Width, 9);
        Assert.Equal(width * 0.8, game.View.Strips[1].SurfaceMoisture!.Range.Width, 9);
        Assert.Equal(width * 1.4, game.View.Strips[2].SurfaceMoisture!.Range.Width, 9);
    }

    [Fact]
    public void Probe_readings_miss_about_as_often_as_content_says_scaled_by_skill()
    {
        double MissRate(Groundsman.Core.Staff.StaffId by)
        {
            var misses = 0;
            var total = 0;
            for (ulong seed = 1; seed <= 400; seed++)
            {
                var game = NewGame(seed);
                for (var n = 1; n <= 12; n++)
                {
                    var id = new StripId(n);
                    if (!game.Submit(new TakeReading(id, by)).Accepted)
                    {
                        break;
                    }
                    total++;
                    misses += game.View.Strips[n - 1].SurfaceMoisture!.Range.Contains(game.Square.Get(id).SurfaceMoisture) ? 0 : 1;
                }
            }
            return misses / (double)total;
        }

        Assert.InRange(MissRate(TestStaff.You), 0.06, 0.10);
        Assert.InRange(MissRate(TestStaff.Sam), 0.045, 0.085);
        Assert.InRange(MissRate(TestStaff.Jo), 0.09, 0.135);
    }

    [Fact]
    public void A_missed_reading_lands_just_off_the_truth()
    {
        var misses = 0;
        for (ulong seed = 1; seed <= 300; seed++)
        {
            var game = NewGame(seed);
            for (var n = 1; n <= 12; n++)
            {
                var id = new StripId(n);
                game.Submit(new TakeReading(id));
                var range = game.View.Strips[n - 1].SurfaceMoisture!.Range;
                var truth = game.Square.Get(id).SurfaceMoisture;
                if (range.Contains(truth))
                {
                    continue;
                }

                misses++;
                var gap = truth < range.Low ? range.Low - truth : truth - range.High;
                Assert.InRange(gap, 0, range.Width * 0.25 + 1e-9);
            }
        }

        Assert.True(misses > 100);
    }

    [Fact]
    public void A_feel_reading_gives_a_word_and_its_band()
    {
        var game = new Game(TestContent.Setup(Start, readings: TestContent.ExactReadings));

        game.Submit(new TakeReading(new StripId(3), tool: ReadingSource.Feel));
        var reading = game.View.Strips[2].SurfaceMoisture!;
        var truth = game.Square.Get(new StripId(3)).SurfaceMoisture;

        var band = TestContent.Readings.FeelBands.Single(b => truth >= b.From && truth < b.To);
        Assert.Equal(ReadingSource.Feel, reading.Source);
        Assert.Equal(band.Word, reading.Word);
        Assert.Equal(new ValueRange(band.From, band.To), reading.Range);
    }

    [Fact]
    public void Feel_readings_are_sometimes_wrong_near_a_boundary_and_more_so_for_casual_staff()
    {
        double WrongRate(Groundsman.Core.Staff.StaffId by)
        {
            var wrong = 0;
            var total = 0;
            for (ulong seed = 1; seed <= 300; seed++)
            {
                var game = NewGame(seed);
                for (var n = 1; n <= 12; n++)
                {
                    var id = new StripId(n);
                    game.Submit(new TakeReading(id, by, ReadingSource.Feel));
                    total++;
                    wrong += game.View.Strips[n - 1].SurfaceMoisture!.Range.Contains(game.Square.Get(id).SurfaceMoisture) ? 0 : 1;
                }
            }
            return wrong / (double)total;
        }

        var you = WrongRate(TestStaff.You);
        var jo = WrongRate(TestStaff.Jo);

        Assert.True(you > 0.02, $"You were wrong {you:P1} of the time");
        Assert.True(jo > you, $"Jo {jo:P1} vs you {you:P1}");
    }

    [Fact]
    public void A_feel_reading_costs_less_time_than_a_probe()
    {
        var game = NewGame(1);

        game.Submit(new TakeReading(new StripId(1), tool: ReadingSource.Feel));

        Assert.Equal(8 - TestStaff.Settings.FeelReadingHours, game.View.Staff[0].HoursLeft, 9);
    }
}
