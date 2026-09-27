using Groundsman.Core;
using Groundsman.Core.Content;
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
        new Fixture(new DateTime(2027, 4, 20), 4, new StripId(6)),
        new Fixture(new DateTime(2027, 5, 5), 1, new StripId(8)),
    });

    [Fact]
    public void Scores_each_fixture_once_on_its_first_morning_from_the_truth()
    {
        var results = SeasonRunner.Run(TestContent.Content, Season, Scoring, new NeglectPolicy(), seed: 3);

        Assert.Equal(2, results.Count);
        Assert.Equal(new GameTime(2027, 4, 20, TestCalendar.Settings.MatchDayDecisionHours[0]), results[0].CheckedAt);
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
}
