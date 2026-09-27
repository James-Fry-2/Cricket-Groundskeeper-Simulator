using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;
using Groundsman.Harness.Policies;

namespace Groundsman.Harness;

public sealed record MatchResult(
    ulong Seed,
    int FixtureIndex,
    StripId Strip,
    GameTime CheckedAt,
    double Surface,
    double Subsurface,
    MatchMorningMiss Miss)
{
    public bool OnTarget => Miss == MatchMorningMiss.None;
}

/// <summary>
/// Plays one season with a policy and scores each fixture's strip from the truth on its first
/// morning, before the policy acts that turn.
/// </summary>
public static class SeasonRunner
{
    public static IReadOnlyList<MatchResult> Run(GameContent content, SeasonSettings season, ScoringSettings scoring, IPolicy policy, ulong seed)
    {
        var start = GameTime.OnDate(season.Start, content.Calendar.MorningHour);
        var game = new Game(new GameSetup(content, start, season.Fixtures, seed));
        var firstHour = content.Calendar.MatchDayDecisionHours[0];
        var results = new List<MatchResult>();

        var next = 0;
        while (next < season.Fixtures.Count)
        {
            var fixture = season.Fixtures[next];
            var check = GameTime.OnDate(fixture.Start, firstHour);
            var now = game.View.Now;

            if (now == check)
            {
                var truth = game.Inspect().Strips[fixture.Strip.Number - 1];
                results.Add(new MatchResult(seed, next, fixture.Strip, now, truth.SurfaceMoisture, truth.SubsurfaceMoisture, scoring.Judge(truth.SurfaceMoisture, truth.SubsurfaceMoisture)));
                next++;
            }
            else if (now > check)
            {
                throw new InvalidOperationException($"The game skipped the first morning of fixture {next} ({check}).");
            }

            policy.PlayTurn(game);
            game.Advance();
        }

        return results;
    }
}
