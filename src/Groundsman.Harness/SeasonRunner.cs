using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Match;
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

    public ResultKind? Result { get; init; }

    /// <summary>Null when rain allowed no play to rate.</summary>
    public PitchGrade? Grade { get; init; }

    public int Demerits { get; init; }
    public IReadOnlyList<string> ReasonIds { get; init; } = Array.Empty<string>();

    /// <summary>The true pitch averaged over the match's turns, for tuning; never seen by a policy.</summary>
    public double Carry { get; init; }

    public double Consistency { get; init; }
    public double Pace { get; init; }
    public double Bounce { get; init; }

    /// <summary>Satisfaction points this fixture earned across the captain, the board and the referee.</summary>
    public double Points { get; init; }

    public DateTime Start { get; init; }
}

/// <summary>
/// Plays one season with a policy. Each fixture's strip is scored from the truth on its first
/// morning, before the policy acts that turn (the phase 2 stand-in), and the season plays on
/// until the last fixture is over so every match has its result and rating.
/// </summary>
public static class SeasonRunner
{
    public static IReadOnlyList<MatchResult> Run(GameContent content, SeasonSettings season, ScoringSettings scoring, IPolicy policy, ulong seed)
    {
        var start = GameTime.OnDate(season.Start, content.Calendar.MorningHour);
        var game = new Game(new GameSetup(content, start, season.Fixtures, seed));
        var results = new List<MatchResult>();

        var samples = season.Fixtures.Select(_ => new List<Groundsman.Core.Pitch.PitchCharacteristics>()).ToArray();
        void Sample()
        {
            var date = game.View.Now.Date;
            for (var i = 0; i < season.Fixtures.Count; i++)
            {
                var f = season.Fixtures[i];
                if (f.Start <= date && date <= f.End && game.View.Fixtures[i].Strip is { } strip)
                {
                    samples[i].Add(game.Inspect().Strips[strip.Number - 1].Pitch);
                }
            }
        }

        var next = 0;
        while (next < season.Fixtures.Count)
        {
            Sample();
            var fixture = season.Fixtures[next];
            var check = GameTime.OnDate(fixture.Start, fixture.Format.DecisionHours[0]);
            var now = game.View.Now;

            if (now == check)
            {
                var strip = game.View.Fixtures[next].Strip!.Value;
                var truth = game.Inspect().Strips[strip.Number - 1];
                results.Add(new MatchResult(seed, next, strip, now, truth.SurfaceMoisture, truth.SubsurfaceMoisture, scoring.Judge(truth.SurfaceMoisture, truth.SubsurfaceMoisture)));
                next++;
            }
            else if (now > check)
            {
                throw new InvalidOperationException($"The game skipped the first morning of fixture {next} ({check}).");
            }

            policy.PlayTurn(game);
            game.Advance();
        }

        var last = season.Fixtures.Count == 0 ? (DateTime?)null : season.Fixtures[season.Fixtures.Count - 1].End;
        while (last != null && game.View.Now.Date <= last)
        {
            Sample();
            policy.PlayTurn(game);
            game.Advance();
        }

        var matches = game.View.Matches;
        var changes = game.View.Stakeholders.SelectMany(s => s.Changes).ToList();
        for (var i = 0; i < results.Count; i++)
        {
            var match = matches.FirstOrDefault(m => m.Fixture == season.Fixtures[i]);
            results[i] = results[i] with
            {
                Result = match?.Result?.Kind,
                Grade = match?.Rating?.Grade,
                Demerits = match?.Rating?.Demerits ?? 0,
                ReasonIds = match?.Rating?.ReasonIds ?? Array.Empty<string>(),
                Carry = Mean(samples[i], p => p.Carry),
                Consistency = Mean(samples[i], p => p.Consistency),
                Pace = Mean(samples[i], p => p.Pace),
                Bounce = Mean(samples[i], p => p.Bounce),
                Points = changes.Where(c => c.Fixture == season.Fixtures[i]).Sum(c => c.Delta),
                Start = season.Fixtures[i].Start,
            };
        }
        return results;
    }

    private static double Mean(List<Groundsman.Core.Pitch.PitchCharacteristics> samples, Func<Groundsman.Core.Pitch.PitchCharacteristics, double> value) =>
        samples.Count == 0 ? 0 : samples.Average(value);
}
