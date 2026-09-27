using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Commands;

public class CoverStripTests
{
    private static readonly StripId Strip3 = new StripId(3);

    private static Game NewGame(ulong seed = 1) => new Game(TestContent.Setup(new GameTime(2027, 5, 10, 7), seed: seed));

    private static StripView View(Game game, StripId id) => game.View.Strips[id.Number - 1];

    private static double WaterMm(Game game, StripId id)
    {
        var strip = game.Square.Get(id);
        return strip.SurfaceMoisture / 100 * TestMoisture.Settings.SurfaceDepthMm
            + strip.SubsurfaceMoisture / 100 * TestMoisture.Settings.SubsurfaceDepthMm;
    }

    [Fact]
    public void Strips_start_uncovered_with_every_cover_free()
    {
        var view = NewGame().View;

        Assert.All(view.Strips, s => Assert.False(s.Covered));
        Assert.Equal(TestContent.Covers.Count, view.CoversFree);
    }

    [Fact]
    public void A_cover_goes_on_when_time_next_advances()
    {
        var game = NewGame();

        Assert.True(game.Submit(new CoverStrip(Strip3)).Accepted);

        Assert.False(View(game, Strip3).Covered);
        Assert.Equal(CoverOrder.Cover, View(game, Strip3).CoverOrder);
        Assert.Equal(TestContent.Covers.Count - 1, game.View.CoversFree);

        game.Advance();

        Assert.True(View(game, Strip3).Covered);
        Assert.Equal(CoverOrder.None, View(game, Strip3).CoverOrder);
    }

    [Fact]
    public void A_cover_comes_off_when_time_next_advances()
    {
        var game = NewGame();
        game.Submit(new CoverStrip(Strip3));
        game.Advance();

        Assert.True(game.Submit(new UncoverStrip(Strip3)).Accepted);
        Assert.True(View(game, Strip3).Covered);
        Assert.Equal(CoverOrder.Uncover, View(game, Strip3).CoverOrder);

        game.Advance();

        Assert.False(View(game, Strip3).Covered);
        Assert.Equal(TestContent.Covers.Count, game.View.CoversFree);
    }

    [Fact]
    public void A_covered_strip_keeps_rain_out()
    {
        var rainyDays = 0;
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var covered = NewGame(seed);
            var open = NewGame(seed);
            covered.Submit(new CoverStrip(Strip3));

            covered.Advance();
            open.Advance();

            if (open.View.Weather!.RainLast24HoursMm > 2)
            {
                rainyDays++;
                Assert.True(WaterMm(covered, Strip3) < WaterMm(open, Strip3), $"seed {seed}");
            }
        }

        Assert.True(rainyDays >= 3, "Too few rainy days to test covers");
    }

    [Fact]
    public void Only_as_many_strips_as_there_are_covers_can_be_covered()
    {
        var game = NewGame();
        for (var n = 1; n <= TestContent.Covers.Count; n++)
        {
            Assert.True(game.Submit(new CoverStrip(new StripId(n))).Accepted);
        }

        var extra = game.Submit(new CoverStrip(new StripId(12)));

        Assert.False(extra.Accepted);
        Assert.Contains("cover", extra.Reason);
        Assert.Equal(0, game.View.CoversFree);
    }

    [Fact]
    public void An_uncover_order_frees_its_cover_for_another_strip_in_the_same_turn()
    {
        var game = NewGame();
        for (var n = 1; n <= TestContent.Covers.Count; n++)
        {
            game.Submit(new CoverStrip(new StripId(n)));
        }
        game.Advance();

        game.Submit(new UncoverStrip(new StripId(1)));

        Assert.True(game.Submit(new CoverStrip(new StripId(12))).Accepted);
        game.Advance();
        Assert.False(View(game, new StripId(1)).Covered);
        Assert.True(View(game, new StripId(12)).Covered);
    }

    [Fact]
    public void Rejects_covering_a_covered_strip_or_uncovering_an_open_one()
    {
        var game = NewGame();

        Assert.False(game.Submit(new UncoverStrip(Strip3)).Accepted);

        game.Submit(new CoverStrip(Strip3));
        game.Advance();

        Assert.False(game.Submit(new CoverStrip(Strip3)).Accepted);
    }

    [Fact]
    public void Rejects_a_second_cover_order_for_a_strip_in_one_turn()
    {
        var game = NewGame();
        game.Submit(new CoverStrip(Strip3));

        var second = game.Submit(new CoverStrip(Strip3));

        Assert.False(second.Accepted);
        Assert.Contains("already", second.Reason);
    }

    [Fact]
    public void Rejects_an_unknown_strip()
    {
        var game = NewGame();

        Assert.False(game.Submit(new CoverStrip(new StripId(13))).Accepted);
        Assert.False(game.Submit(new UncoverStrip(new StripId(13))).Accepted);
    }
}
