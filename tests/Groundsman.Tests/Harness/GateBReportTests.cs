using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;
using Groundsman.Harness;

namespace Groundsman.Tests.Harness;

public class GateBReportTests
{
    private static readonly SeasonSettings Season = new SeasonSettings(new DateTime(2027, 4, 1), new[]
    {
        new Fixture(new DateTime(2027, 4, 20), TestFormats.OneDay, null, TestTeams.Opponent, televised: true),
        new Fixture(new DateTime(2027, 5, 20), TestFormats.FourDay, null, TestTeams.Opponent),
        new Fixture(new DateTime(2027, 8, 20), TestFormats.OneDay, null, TestTeams.Opponent, televised: true),
    });

    private static GateBReport Run(double lead = 10, double tolerance = 3) =>
        GateBReport.Run(TestContent.Content, Season, new ScoringSettings(24, 30, 22), seasons: 4, lead, tolerance);

    [Fact]
    public void Reports_points_by_month_for_each_policy_repeatably()
    {
        var a = Run();
        var b = Run();

        Assert.Equal(new[] { "neglect", "by the book", "greedy", "planned" }, a.Policies.Select(p => p.Name));
        Assert.Equal(a.Csv, b.Csv);
        Assert.All(a.Policies, p => Assert.Equal(0, p.PointsByMonth[6]));
        Assert.Contains("Gate B", a.SummaryTable());
    }

    [Fact]
    public void Passes_only_when_greedy_keeps_up_early_and_planned_leads_late()
    {
        var report = Run();

        Assert.Equal(report.EarlyGap <= 3 && report.LateLead >= 10, report.Passed);
        Assert.False(Run(lead: report.LateLead + 1).Passed);
    }
}
