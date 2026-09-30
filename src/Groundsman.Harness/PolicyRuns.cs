using Groundsman.Core.Content;
using Groundsman.Harness.Policies;

namespace Groundsman.Harness;

/// <summary>Neglect, random and by the book, each played over the same seeded seasons.</summary>
public static class PolicyRuns
{
    public static readonly string[] Names = { "neglect", "random", "by the book" };

    /// <summary>For each policy in <see cref="Names"/> order, each season's results in seed order.</summary>
    public static IReadOnlyList<IReadOnlyList<MatchResult>[]> Run(GameContent content, SeasonSettings season, ScoringSettings scoring, int seasons, ulong firstSeed = 1)
    {
        var makers = new Func<ulong, IPolicy>[]
        {
            _ => new NeglectPolicy(),
            seed => new RandomPolicy(seed),
            _ => new ByTheBookPolicy(),
        };

        var runs = new List<IReadOnlyList<MatchResult>[]>();
        foreach (var make in makers)
        {
            // Seasons run in parallel; each writes only its own slot, so the order of results,
            // and everything computed from them, doesn't depend on thread timing.
            var perSeed = new IReadOnlyList<MatchResult>[seasons];
            Parallel.For(0, seasons, i =>
            {
                var seed = firstSeed + (ulong)i;
                perSeed[i] = SeasonRunner.Run(content, season, scoring, make(seed), seed);
            });
            runs.Add(perSeed);
        }
        return runs;
    }
}
