using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;
using Groundsman.Harness;

namespace Groundsman.Tests.Harness;

public class GateReportTests
{
    private static readonly SeasonSettings Season = new SeasonSettings(new DateTime(2027, 4, 1), new[]
    {
        new Fixture(new DateTime(2027, 5, 20), TestFormats.FourDay, new StripId(6), TestTeams.Opponent),
        new Fixture(new DateTime(2027, 6, 10), TestFormats.OneDay, new StripId(8), TestTeams.Opponent),
        new Fixture(new DateTime(2027, 7, 1), TestFormats.FourDay, new StripId(5), TestTeams.Opponent),
    });

    [Fact]
    public void Summarises_each_policy_and_is_repeatable()
    {
        var a = GateReport.Run(TestContent.Content, Season, new ScoringSettings(24, 30, 22), seasons: 10, requiredLead: 0.25);
        var b = GateReport.Run(TestContent.Content, Season, new ScoringSettings(24, 30, 22), seasons: 10, requiredLead: 0.25);

        Assert.Equal(new[] { "neglect", "random", "by the book" }, a.Policies.Select(p => p.Name));
        Assert.All(a.Policies, p => Assert.Equal(30, p.Matches));
        Assert.Equal(a.Policies.Select(p => p.OnTargetRate), b.Policies.Select(p => p.OnTargetRate));
        Assert.Equal(a.Csv, b.Csv);
    }

    [Fact]
    public void Passes_only_when_by_the_book_leads_neglect_by_the_required_margin()
    {
        var report = GateReport.Run(TestContent.Content, Season, new ScoringSettings(24, 30, 22), seasons: 10, requiredLead: 0.25);
        var lead = report.Policies[2].OnTargetRate - report.Policies[0].OnTargetRate;

        Assert.Equal(lead >= 0.25, report.Passed);
    }
}
