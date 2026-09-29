using Groundsman.Core;
using Groundsman.Core.Match;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Match;

public class RatingPlayTests
{
    private static readonly StripId MatchStrip = new StripId(6);

    private static Game Played(Action<StripState> setUp, ulong seed)
    {
        var fixture = new Fixture(new DateTime(2027, 6, 10), TestFormats.FourDay, MatchStrip, TestTeams.Opponent);
        var game = new Game(new GameSetup(TestContent.Content, new GameTime(2027, 6, 10, 8), new[] { fixture }, seed));
        setUp(game.Square.Get(MatchStrip));
        while (game.View.Now.Date <= fixture.End)
        {
            game.Advance();
        }
        return game;
    }

    [Fact]
    public void Every_finished_match_is_rated_and_its_demerits_are_counted()
    {
        var game = Played(s => s.StructureDamage = 0.9, seed: 1);
        var rating = game.View.LatestMatch!.Rating!;

        Assert.NotEqual(PitchGrade.Satisfactory, rating.Grade);
        Assert.NotEmpty(rating.Reasons);
        Assert.Equal(rating.Demerits, game.View.DemeritsActive);
    }

    [Fact]
    public void A_pitch_that_falls_apart_is_abandoned_and_rated_unfit()
    {
        var game = Played(s => { s.StructureDamage = 1; s.Footholes = 1; s.Cracks = 1; s.SurfaceWear = 1; }, seed: 1);
        var match = game.View.LatestMatch!;

        Assert.Equal(ResultKind.NoResult, match.Result!.Kind);
        Assert.Contains("bandoned", match.Result.Text);
        Assert.Equal(PitchGrade.Unfit, match.Rating!.Grade);
        Assert.Equal(3, game.View.DemeritsActive);
        Assert.Contains(match.Commentary, c => c.EventId == "danger");
    }

    [Fact]
    public void A_neglected_strip_rates_worse_than_a_prepared_one()
    {
        double AverageGrade(Action<StripState> setUp) => Enumerable.Range(1, 20)
            .Average(seed => (int)Played(setUp, (ulong)seed).View.LatestMatch!.Rating!.Grade);

        var prepared = AverageGrade(s => { s.Compaction = 0.85; s.GrassHeightMm = 7; });
        var neglected = AverageGrade(_ => { });

        Assert.True(neglected > prepared, $"Neglected {neglected:0.00} vs prepared {prepared:0.00} (higher is worse)");
    }
}
