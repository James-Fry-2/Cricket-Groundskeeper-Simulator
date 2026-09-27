using System.Text;
using Groundsman.Core.Content;
using Groundsman.Harness.Policies;

namespace Groundsman.Harness;

/// <summary>
/// Gate A: over many seeded seasons, does play by the book land match strips on target
/// clearly more often than neglect?
/// </summary>
public sealed class GateReport
{
    private GateReport(IReadOnlyList<PolicySummary> policies, bool passed, double requiredLead, string csv)
    {
        Policies = policies;
        Passed = passed;
        RequiredLead = requiredLead;
        Csv = csv;
    }

    public IReadOnlyList<PolicySummary> Policies { get; }
    public bool Passed { get; }
    public double RequiredLead { get; }
    public string Csv { get; }

    public static GateReport Run(GameContent content, SeasonSettings season, ScoringSettings scoring, int seasons, double requiredLead)
    {
        var makers = new (string Name, Func<ulong, IPolicy> Make)[]
        {
            ("neglect", _ => new NeglectPolicy()),
            ("random", seed => new RandomPolicy(seed)),
            ("by the book", _ => new ByTheBookPolicy()),
        };

        var csv = new StringBuilder("seed,policy,fixture,strip,checked_at,surface,subsurface,result\n");
        var summaries = new List<PolicySummary>();
        foreach (var (name, make) in makers)
        {
            // Seasons run in parallel; each writes only its own slot, so the order of results,
            // and everything computed from them, doesn't depend on thread timing.
            var perSeed = new IReadOnlyList<MatchResult>[seasons];
            Parallel.For(0, seasons, i =>
            {
                var seed = (ulong)(i + 1);
                perSeed[i] = SeasonRunner.Run(content, season, scoring, make(seed), seed);
            });

            var results = perSeed.SelectMany(r => r).ToList();
            foreach (var r in results)
            {
                csv.Append($"{r.Seed},{name},{r.FixtureIndex},{r.Strip.Number},{r.CheckedAt},{Harness.Csv.Number(r.Surface)},{Harness.Csv.Number(r.Subsurface)},{r.Miss}\n");
            }
            summaries.Add(new PolicySummary(
                name,
                results.Count,
                Rate(results, r => r.OnTarget),
                Rate(results, r => r.Miss == MatchMorningMiss.SubsurfaceDry),
                Rate(results, r => r.Miss == MatchMorningMiss.SubsurfaceWet),
                Rate(results, r => r.Miss == MatchMorningMiss.SurfaceWet)));
        }

        var lead = summaries[2].OnTargetRate - summaries[0].OnTargetRate;
        return new GateReport(summaries, lead >= requiredLead, requiredLead, csv.ToString());
    }

    public string SummaryTable()
    {
        var text = new StringBuilder("Policy        Matches  On target  Too dry below  Too wet below  Wet on top\n");
        foreach (var p in Policies)
        {
            text.Append($"{p.Name,-12}  {p.Matches,7}  {p.OnTargetRate,9:P0}  {p.SubsurfaceDryRate,13:P0}  {p.SubsurfaceWetRate,13:P0}  {p.SurfaceWetRate,10:P0}\n");
        }
        var lead = Policies[2].OnTargetRate - Policies[0].OnTargetRate;
        text.Append($"\nBy the book leads neglect by {lead * 100:0} points (needs {RequiredLead * 100:0}): Gate A {(Passed ? "PASSED" : "NOT PASSED")}.\n");
        return text.ToString();
    }

    private static double Rate(List<MatchResult> results, Func<MatchResult, bool> test) =>
        results.Count == 0 ? 0 : results.Count(test) / (double)results.Count;

    public sealed record PolicySummary(
        string Name,
        int Matches,
        double OnTargetRate,
        double SubsurfaceDryRate,
        double SubsurfaceWetRate,
        double SurfaceWetRate);
}
