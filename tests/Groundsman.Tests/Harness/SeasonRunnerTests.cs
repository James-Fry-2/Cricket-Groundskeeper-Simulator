using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Match;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;
using Groundsman.Harness;
using Groundsman.Harness.Policies;

namespace Groundsman.Tests.Harness;

public class SeasonRunnerTests
{
    private static readonly ScoringSettings Scoring = new ScoringSettings(24, 30, 22);

    private static readonly SeasonSettings Season = new SeasonSettings(new DateTime(2027, 4, 1), new[]
    {
        new Fixture(new DateTime(2027, 4, 20), TestFormats.FourDay, new StripId(6), TestTeams.Opponent),
        new Fixture(new DateTime(2027, 5, 5), TestFormats.OneDay, new StripId(8), TestTeams.Opponent),
    });

    [Fact]
    public void Scores_each_fixture_once_on_its_first_morning_from_the_truth()
    {
        var results = SeasonRunner.Run(TestContent.Content, Season, Scoring, new NeglectPolicy(), seed: 3);

        Assert.Equal(2, results.Count);
        Assert.Equal(new GameTime(2027, 4, 20, TestFormats.FourDay.DecisionHours[0]), results[0].CheckedAt);
        Assert.Equal(new StripId(6), results[0].Strip);
        Assert.Equal(new StripId(8), results[1].Strip);
        Assert.All(results, r => Assert.Equal(Scoring.Judge(r.Surface, r.Subsurface), r.Miss));
    }

    [Fact]
    public void The_same_seed_gives_the_same_results()
    {
        var a = SeasonRunner.Run(TestContent.Content, Season, Scoring, new ByTheBookPolicy(), seed: 3);
        var b = SeasonRunner.Run(TestContent.Content, Season, Scoring, new ByTheBookPolicy(), seed: 3);

        Assert.Equal(a.Select(r => r.Subsurface), b.Select(r => r.Subsurface));
    }

    [Fact]
    public void Neglect_leaves_the_same_ground_as_an_untouched_game()
    {
        var results = SeasonRunner.Run(TestContent.Content, Season, Scoring, new NeglectPolicy(), seed: 3);

        var game = new Game(new GameSetup(TestContent.Content, new GameTime(2027, 4, 1, 7), Season.Fixtures, 3));
        while (game.View.Now < results[0].CheckedAt)
        {
            game.Advance();
        }

        Assert.Equal(game.Inspect().Strips[5].SubsurfaceMoisture, results[0].Subsurface);
    }

    [Fact]
    public void Random_play_is_repeatable_for_a_seed()
    {
        var a = SeasonRunner.Run(TestContent.Content, Season, Scoring, new RandomPolicy(9), seed: 3);
        var b = SeasonRunner.Run(TestContent.Content, Season, Scoring, new RandomPolicy(9), seed: 3);

        Assert.Equal(a.Select(r => r.Surface), b.Select(r => r.Surface));
    }

    [Fact]
    public void Each_fixture_carries_its_result_and_the_referees_rating()
    {
        var results = SeasonRunner.Run(TestContent.Content, Season, Scoring, new NeglectPolicy(), seed: 3);

        var game = new Game(new GameSetup(TestContent.Content, new GameTime(2027, 4, 1, 7), Season.Fixtures, 3));
        while (game.View.Now.Date <= Season.Fixtures[^1].End)
        {
            game.Advance();
        }

        Assert.Equal(game.View.Matches.Count, results.Count);
        for (var i = 0; i < results.Count; i++)
        {
            var match = game.View.Matches[i];
            Assert.Equal(match.Result!.Kind, results[i].Result);
            Assert.Equal(match.Rating?.Grade, results[i].Grade);
            Assert.Equal(match.Rating?.Demerits ?? 0, results[i].Demerits);
            Assert.Equal(match.Rating?.ReasonIds ?? Array.Empty<string>(), results[i].ReasonIds);
        }
    }

    [Fact]
    public void Each_fixture_records_its_true_pitch_averaged_over_the_match()
    {
        var results = SeasonRunner.Run(TestContent.Content, Season, Scoring, new NeglectPolicy(), seed: 3);

        Assert.All(results, r =>
        {
            Assert.InRange(r.Carry, 0.01, 10);
            Assert.InRange(r.Consistency, 0.01, 10);
            Assert.InRange(r.Pace, 0.01, 10);
            Assert.InRange(r.Bounce, 0.01, 10);
        });
    }
}
