using Groundsman.Harness;

namespace Groundsman.Tests.Harness;

public class WeatherReportTests
{
    [Fact]
    public void Writes_one_row_per_day_with_a_header()
    {
        var report = WeatherReport.Run(TestClimate.Settings, seed: 3, years: 2);
        var lines = report.DailyCsv.TrimEnd().Split('\n');

        Assert.Equal("date,wet,rain_mm,min_temp,max_temp,mean_temp,wind_kph,sunshine_h", lines[0]);
        Assert.Equal(365 + 366 + 1, lines.Length);
        Assert.StartsWith("2027-01-01,", lines[1]);
    }

    [Fact]
    public void Summarises_each_month_against_the_normals()
    {
        var report = WeatherReport.Run(TestClimate.Settings, seed: 3, years: 50);

        Assert.Equal(12, report.Months.Count);
        foreach (var month in report.Months)
        {
            Assert.InRange(month.RainTotalMm, month.Normal.RainTotalMm * 0.8, month.Normal.RainTotalMm * 1.2);
        }
    }
}
