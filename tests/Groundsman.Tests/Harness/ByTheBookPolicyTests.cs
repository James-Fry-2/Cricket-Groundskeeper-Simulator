using Groundsman.Core;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;
using Groundsman.Harness.Policies;

namespace Groundsman.Tests.Harness;

public class ByTheBookPolicyTests
{
    private static readonly Fixture Match = new Fixture(new DateTime(2027, 6, 20), 4, new StripId(6));

    private static Game GameAt(GameTime start, ulong seed = 1) =>
        new Game(new GameSetup(TestContent.Content, start, new[] { Match }, seed));

    [Fact]
    public void Does_nothing_more_than_ten_days_out()
    {
        var game = GameAt(new GameTime(2027, 6, 5, 7));

        new ByTheBookPolicy().PlayTurn(game);

        Assert.All(game.View.Staff, s => Assert.Equal(s.HoursPerDay, s.HoursLeft));
    }

    [Fact]
    public void Reads_the_match_strip_each_morning_of_the_build_up_using_the_deputy()
    {
        var game = GameAt(new GameTime(2027, 6, 12, 7));

        new ByTheBookPolicy().PlayTurn(game);

        var reading = game.View.Strips[5].SurfaceMoisture;
        Assert.NotNull(reading);
        Assert.Equal("sam", reading!.TakenBy.Value);
    }

    [Fact]
    public void Waters_deeply_five_days_out_unless_rain_is_likely_or_the_strip_reads_wet()
    {
        var watered = 0;
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var game = GameAt(new GameTime(2027, 6, 15, 7), seed);
            new ByTheBookPolicy().PlayTurn(game);

            var view = game.View;
            var reading = view.Strips[5].SurfaceMoistureNow!.Value;
            var shouldWater = view.Forecast[0].ChanceOfRain < ByTheBookPolicy.RainLikely && (reading.Low + reading.High) / 2 <= ByTheBookPolicy.ReadsWet;
            Assert.Equal(shouldWater, view.Strips[5].WateringQueued);
            watered += shouldWater ? 1 : 0;
        }

        Assert.True(watered >= 5);
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
            });
            game.Advance();
        }
    }
}
