using System.Text;
using Groundsman.Core.Content;
using Groundsman.Core.Match;
using Groundsman.Harness.Policies;

namespace Groundsman.Harness;

/// <summary>
/// Gate B: does strip rotation matter? Greedy play (the best strip now, yes to everything)
/// should keep up with planned play early in the season and fall behind it late, once the
/// strips it burned are tired. Scored as the satisfaction points each fixture earns across the
/// captain, the board and the referee, by the month it's played.
/// </summary>
public sealed class GateBReport
{
    public static readonly int[] Months = { 4, 5, 6, 7, 8, 9 };
    public static readonly int[] Early = { 4, 5 };
    public static readonly int[] Late = { 8, 9 };

    private GateBReport(IReadOnlyList<PolicyMonths> policies, double requiredLead, double earlyTolerance, string csv)
    {
        Policies = policies;
        RequiredLead = requiredLead;
        EarlyTolerance = earlyTolerance;
        Csv = csv;
    }

    public IReadOnlyList<PolicyMonths> Policies { get; }

    /// <summary>Points a season planned play must lead greedy by in the late months.</summary>
    public double RequiredLead { get; }

    /// <summary>Points a season greedy may trail planned by in the early months and still count as level.</summary>
    public double EarlyTolerance { get; }

    public string Csv { get; }

    public PolicyMonths Greedy => Policies.Single(p => p.Name == "greedy");
    public PolicyMonths Planned => Policies.Single(p => p.Name == "planned");

    public double EarlyGap => Planned.Points(Early) - Greedy.Points(Early);
    public double LateLead => Planned.Points(Late) - Greedy.Points(Late);
    public bool Passed => EarlyGap <= EarlyTolerance && LateLead >= RequiredLead;

    public static GateBReport Run(GameContent content, SeasonSettings season, ScoringSettings scoring, int seasons, double requiredLead, double earlyTolerance, ulong firstSeed = 1)
    {
        var makers = new (string Name, Func<ulong, IPolicy> Make)[]
        {
            ("neglect", _ => new NeglectPolicy()),
            ("by the book", _ => new ByTheBookPolicy(SeasonPlans.Original)),
            ("greedy", _ => new GreedyPolicy()),
            ("planned", _ => new PlannedPolicy()),
        };
        var runs = PolicyRuns.Run(content, season, scoring, seasons, firstSeed, makers.Select(m => m.Make).ToArray());

        var csv = new StringBuilder("seed,policy,fixture,start,strip,grade,points\n");
        var policies = new List<PolicyMonths>();
        for (var p = 0; p < makers.Length; p++)
        {
            var all = runs[p].SelectMany(r => r).ToList();
            foreach (var r in all)
            {
                csv.Append($"{r.Seed},{makers[p].Name},{r.FixtureIndex},{r.Start:yyyy-MM-dd},{r.Strip.Number},{r.Grade},{Harness.Csv.Number(r.Points)}\n");
            }
            policies.Add(new PolicyMonths(
                makers[p].Name,
                Months.ToDictionary(m => m, m => all.Where(r => r.Start.Month == m).Sum(r => r.Points) / seasons),
                Months.ToDictionary(m => m, m => Share(all.Where(r => r.Start.Month == m && r.Grade != null).ToList(), r => r.Grade <= PitchGrade.Satisfactory)),
                Months.ToDictionary(m => m, m => Share(all.Where(r => r.Start.Month == m && r.Grade != null).ToList(), r => r.Grade == PitchGrade.VeryGood))));
        }
        return new GateBReport(policies, requiredLead, earlyTolerance, csv.ToString());
    }

    public string SummaryTable()
    {
        var text = new StringBuilder("Satisfaction points a season, by month played (captain + board + referee):\n");
        text.Append("Policy        " + string.Join("", Months.Select(m => $"{new DateTime(2027, m, 1).ToString("MMM", System.Globalization.CultureInfo.InvariantCulture),7}")) + "   Season\n");
        foreach (var p in Policies)
        {
            text.Append($"{p.Name,-12}  " + string.Join("", Months.Select(m => $"{p.PointsByMonth[m],7:0.0}")) + $"  {p.Points(Months),7:0.0}\n");
        }
        text.Append("\nSatisfactory or better / very good, by month:\n");
        foreach (var p in Policies)
        {
            text.Append($"{p.Name,-12}  " + string.Join("", Months.Select(m => $" {p.SatisfactoryByMonth[m],4:P0}/{p.VeryGoodByMonth[m],-4:P0}")) + "\n");
        }
        text.Append($"\nApril and May: greedy {Greedy.Points(Early):0.0}, planned {Planned.Points(Early):0.0} (greedy may trail by {EarlyTolerance:0}). ");
        text.Append($"August and September: planned leads by {LateLead:0.0} (needs {RequiredLead:0}): Gate B {(Passed ? "PASSED" : "NOT PASSED")}.\n");
        return text.ToString();
    }

    private static double Share(List<MatchResult> results, Func<MatchResult, bool> test) =>
        results.Count == 0 ? 0 : results.Count(test) / (double)results.Count;

    public sealed record PolicyMonths(
        string Name,
        IReadOnlyDictionary<int, double> PointsByMonth,
        IReadOnlyDictionary<int, double> SatisfactoryByMonth,
        IReadOnlyDictionary<int, double> VeryGoodByMonth)
    {
        public double Points(IEnumerable<int> months) => months.Sum(m => PointsByMonth[m]);
    }
}
