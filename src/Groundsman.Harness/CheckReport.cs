using System.Text;
using Groundsman.Core.Content;
using Groundsman.Core.Match;

namespace Groundsman.Harness;

/// <summary>
/// Check 3, the gate before phase 4: over many seeded seasons, does play by the book satisfy the
/// match referee, and does neglect earn demerits? The phase 2 stand-in score rides alongside.
/// </summary>
public sealed class CheckReport
{
    private CheckReport(IReadOnlyList<PolicyCheck> policies, double requiredSatisfactory, double requiredNeglectDemerits, string csv)
    {
        Policies = policies;
        RequiredSatisfactory = requiredSatisfactory;
        RequiredNeglectDemerits = requiredNeglectDemerits;
        Csv = csv;
    }

    public IReadOnlyList<PolicyCheck> Policies { get; }

    /// <summary>Share of by the book's rated matches that must be satisfactory or better.</summary>
    public double RequiredSatisfactory { get; }

    /// <summary>Share of neglect's matches that must earn demerits.</summary>
    public double RequiredNeglectDemerits { get; }

    public string Csv { get; }

    public bool Passed =>
        Policies[2].SatisfactoryOrBetter >= RequiredSatisfactory && Policies[0].WithDemerits >= RequiredNeglectDemerits;

    public static CheckReport From(IReadOnlyList<IReadOnlyList<MatchResult>[]> runs, RatingSettings rating, double requiredSatisfactory, double requiredNeglectDemerits)
    {
        var csv = new StringBuilder("seed,policy,fixture,strip,result,grade,demerits,reasons,stand_in_on_target\n");
        var checks = new List<PolicyCheck>();
        for (var p = 0; p < runs.Count; p++)
        {
            var name = PolicyRuns.Names[p];
            var seasons = runs[p];
            var all = seasons.SelectMany(s => s).ToList();
            var rated = all.Where(r => r.Grade != null).ToList();

            foreach (var r in all)
            {
                csv.Append($"{r.Seed},{name},{r.FixtureIndex},{r.Strip.Number},{r.Result},{r.Grade},{r.Demerits},{string.Join(' ', r.ReasonIds)},{r.OnTarget}\n");
            }

            var faults = all
                .Where(r => r.Demerits > 0)
                .SelectMany(r => r.ReasonIds.Distinct())
                .GroupBy(id => id)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count() / (double)all.Count);

            checks.Add(new PolicyCheck(
                name,
                all.Count,
                rated.Count,
                Share(rated, r => r.Grade == PitchGrade.VeryGood),
                Share(rated, r => r.Grade == PitchGrade.Satisfactory),
                Share(rated, r => r.Grade == PitchGrade.Unsatisfactory),
                Share(rated, r => r.Grade == PitchGrade.Unfit),
                Share(all, r => r.Demerits > 0),
                seasons.Length == 0 ? 0 : seasons.Average(s => (double)s.Sum(r => r.Demerits)),
                seasons.Length == 0 ? 0 : seasons.Count(s => s.Sum(r => r.Demerits) >= rating.BanAt) / (double)seasons.Length,
                Share(all, r => r.Result == ResultKind.Draw),
                Share(all, r => r.Result == ResultKind.NoResult),
                Share(all, r => r.OnTarget),
                faults));
        }
        return new CheckReport(checks, requiredSatisfactory, requiredNeglectDemerits, csv.ToString());
    }

    public string SummaryTable()
    {
        var text = new StringBuilder("Policy        Matches  Very good  Satisfactory  Unsatisfactory  Unfit  Sat. or better  Demerits  Per season  Banned  Draws  No result  Stand-in\n");
        foreach (var p in Policies)
        {
            text.Append($"{p.Name,-12}  {p.Matches,7}  {p.VeryGood,9:P0}  {p.Satisfactory,12:P0}  {p.Unsatisfactory,14:P0}  {p.Unfit,5:P0}  {p.SatisfactoryOrBetter,14:P0}  {p.WithDemerits,8:P0}  {p.DemeritsPerSeason,10:0.0}  {p.BannedSeasons,6:P0}  {p.Draws,5:P0}  {p.NoResults,9:P0}  {p.OnTarget,8:P0}\n");
        }

        text.Append("\nFaults, as a share of all matches:\n");
        foreach (var p in Policies)
        {
            var faults = p.Faults.Count == 0 ? "none" : string.Join(", ", p.Faults.OrderByDescending(f => f.Value).Select(f => $"{f.Key} {f.Value:P0}"));
            text.Append($"  {p.Name,-12}  {faults}\n");
        }

        text.Append($"\nBy the book satisfactory or better on {Policies[2].SatisfactoryOrBetter:P0} of rated matches (needs {RequiredSatisfactory:P0}); ");
        text.Append($"neglect earned demerits on {Policies[0].WithDemerits:P0} of matches (needs {RequiredNeglectDemerits:P0}): Check 3 {(Passed ? "PASSED" : "NOT PASSED")}.\n");
        return text.ToString();
    }

    private static double Share(List<MatchResult> results, Func<MatchResult, bool> test) =>
        results.Count == 0 ? 0 : results.Count(test) / (double)results.Count;

    /// <summary>Grade shares are of rated matches; the rest are of all matches.</summary>
    public sealed record PolicyCheck(
        string Name,
        int Matches,
        int Rated,
        double VeryGood,
        double Satisfactory,
        double Unsatisfactory,
        double Unfit,
        double WithDemerits,
        double DemeritsPerSeason,
        double BannedSeasons,
        double Draws,
        double NoResults,
        double OnTarget,
        IReadOnlyDictionary<string, double> Faults)
    {
        public double SatisfactoryOrBetter => VeryGood + Satisfactory;
    }
}
