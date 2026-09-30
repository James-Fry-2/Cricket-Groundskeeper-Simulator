using Groundsman.Harness;

namespace Groundsman.Tests.Harness;

public class ReuseReportTests
{
    [Fact]
    public void Compares_each_reuse_gap_and_a_third_use_with_a_fresh_strip_repeatably()
    {
        var a = ReuseReport.Run(TestContent.Content, new DateTime(2027, 4, 1), new DateTime(2027, 6, 1), seasons: 2);
        var b = ReuseReport.Run(TestContent.Content, new DateTime(2027, 4, 1), new DateTime(2027, 6, 1), seasons: 2);

        Assert.Equal(ReuseReport.GapDays.Length + 1, a.Rows.Count);
        Assert.All(a.Rows, r => Assert.InRange(r.Consistency, 0.01, 10));
        Assert.Equal(a.Rows, b.Rows);
        Assert.Contains("Third use", a.SummaryTable());
    }
}
