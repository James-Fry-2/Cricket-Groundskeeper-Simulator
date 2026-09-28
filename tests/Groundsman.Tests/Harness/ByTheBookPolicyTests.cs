using Groundsman.Core;
using Groundsman.Core.Readings;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;
using Groundsman.Harness.Policies;

namespace Groundsman.Tests.Harness;

public class ByTheBookPolicyTests
{
    private static readonly Fixture Match = new Fixture(new DateTime(2027, 6, 20), TestFormats.FourDay, new StripId(6), TestTeams.Opponent);

    private static Game GameAt(GameTime start, ulong seed = 1) =>
        new Game(new GameSetup(TestContent.Content, start, new[] { Match }, seed));

    private static double Midpoint(ValueRange range) => (range.Low + range.High) / 2;

    [Fact]
    public void Does_nothing_more_than_ten_days_out()
    {
        var game = GameAt(new GameTime(2027, 6, 5, 7));

        new ByTheBookPolicy().PlayTurn(game);

        Assert.All(game.View.Staff, s => Assert.Equal(s.HoursPerDay, s.HoursLeft));
    }

    [Fact]
    public void The_deputy_cores_the_match_strip_each_morning_from_a_week_out()
    {
        var game = GameAt(new GameTime(2027, 6, 13, 7));

        new ByTheBookPolicy().PlayTurn(game);

        var core = game.View.Strips[5].SubsurfaceMoisture;
        Assert.NotNull(core);
        Assert.Equal(ReadingSource.SoilCore, core!.Source);
        Assert.Equal("sam", core.TakenBy.Value);
        Assert.Null(game.View.Strips[5].SurfaceMoisture);
    }

    [Fact]
    public void Also_probes_the_surface_in_the_final_days()
    {
        var game = GameAt(new GameTime(2027, 6, 17, 7));

        new ByTheBookPolicy().PlayTurn(game);

        Assert.NotNull(game.View.Strips[5].SurfaceMoisture);
        Assert.NotNull(game.View.Strips[5].SubsurfaceMoisture);
    }

    [Fact]
    public void Waters_when_the_core_reads_dry_and_rain_is_unlikely_and_not_otherwise()
    {
        var watered = 0;
        var held = 0;
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var game = GameAt(new GameTime(2027, 6, 15, 7), seed);
            if (seed % 2 == 0)
            {
                game.Square.Get(Match.Strip).SubsurfaceMoisture = 18;
            }
            new ByTheBookPolicy().PlayTurn(game);

            var view = game.View;
            var depth = Midpoint(view.Strips[5].SubsurfaceMoistureNow!.Value);
            var shouldWater = depth < ByTheBookPolicy.WaterBelowDepth && view.Forecast[0].ChanceOfRain < ByTheBookPolicy.RainLikely;
            Assert.Equal(shouldWater, view.Strips[5].WateringQueued);
            watered += shouldWater ? 1 : 0;
            held += shouldWater ? 0 : 1;
        }

        Assert.True(watered >= 5 && held >= 5, $"watered {watered}, held {held}");
    }

    [Fact]
    public void Covers_in_the_final_days_when_rain_threatens_and_uncovers_when_it_doesnt()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var game = GameAt(new GameTime(2027, 6, 18, 7), seed);
            new ByTheBookPolicy().PlayTurn(game);

            var view = game.View;
            var threat = view.Forecast[0].ChanceOfRain >= ByTheBookPolicy.CoverAtChance;
            Assert.Equal(threat, view.Strips[5].CoverOrder == CoverOrder.Cover);
        }
    }

    [Fact]
    public void Leaves_a_dry_strip_open_to_rain_early_in_the_build_up()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var game = GameAt(new GameTime(2027, 6, 14, 7), seed);
            game.Square.Get(Match.Strip).SubsurfaceMoisture = 16;

            new ByTheBookPolicy().PlayTurn(game);

            var view = game.View;
            var heavy = view.Forecast[0].ChanceOfRain >= ByTheBookPolicy.RainLikely && view.Forecast[0].Rain.High >= ByTheBookPolicy.HeavyRainMm;
            Assert.Equal(heavy, view.Strips[5].CoverOrder == CoverOrder.Cover);
        }
    }

    [Fact]
    public void Only_ever_works_on_the_match_strip()
    {
        var game = GameAt(new GameTime(2027, 6, 10, 7));
        var policy = new ByTheBookPolicy();

        while (game.View.Now.Date < Match.Start)
        {
            policy.PlayTurn(game);
            Assert.All(game.View.Strips.Where(s => s.Id != Match.Strip), s =>
            {
                Assert.False(s.WateringQueued);
                Assert.Equal(CoverOrder.None, s.CoverOrder);
                Assert.Null(s.SubsurfaceMoisture);
            });
            game.Advance();
        }
    }
}
