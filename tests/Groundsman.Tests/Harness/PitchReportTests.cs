using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Harness;

namespace Groundsman.Tests.Harness;

public class PitchReportTests
{
    private static readonly SeasonSettings Season = new SeasonSettings(new DateTime(2027, 4, 1), Array.Empty<Fixture>());

    [Fact]
    public void Records_each_morning_for_every_seed_and_averages_by_month()
    {
        var report = PitchReport.Run(TestContent.Content, Season, seasons: 3, end: new DateTime(2027, 6, 30));
        var lines = report.Csv.TrimEnd().Split('\n');

        Assert.Equal("seed,date,pace,bounce,consistency,carry,seam,spin,cracking", lines[0]);
        Assert.Equal(3 * 91 + 1, lines.Length);
        Assert.Equal(new[] { 4, 5, 6 }, report.Months.Select(m => m.Month));
        Assert.All(report.Months, m => Assert.InRange(m.Pace, 0, 10));
    }

    [Fact]
    public void Is_repeatable()
    {
        Assert.Equal(
            PitchReport.Run(TestContent.Content, Season, seasons: 2, end: new DateTime(2027, 5, 1)).Csv,
            PitchReport.Run(TestContent.Content, Season, seasons: 2, end: new DateTime(2027, 5, 1)).Csv);
    }
}
