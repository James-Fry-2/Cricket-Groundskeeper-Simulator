using System.Text;
using Groundsman.Core.Content;
using Groundsman.Core.Randomness;
using Groundsman.Core.Weather;

namespace Groundsman.Harness;

/// <summary>
/// Runs the weather generator on its own for many years, to check it against the climate
/// normals by eye.
/// </summary>
public sealed class WeatherReport
{
    private const int FirstYear = 2027;

    private WeatherReport(string dailyCsv, IReadOnlyList<MonthSummary> months)
    {
        DailyCsv = dailyCsv;
        Months = months;
    }

    public string DailyCsv { get; }
    public IReadOnlyList<MonthSummary> Months { get; }

    public static WeatherReport Run(ClimateSettings climate, ulong seed, int years)
    {
        var generator = new WeatherGenerator(climate, new RandomStreams(seed).Get(RandomStream.Weather));
        var start = new DateTime(FirstYear, 1, 1);
        var end = start.AddYears(years);
        var days = new List<DayWeather>();
        for (var date = start; date < end; date = date.AddDays(1))
        {
            days.Add(generator.Next(date));
        }

        var csv = new StringBuilder("date,wet,rain_mm,min_temp,max_temp,mean_temp,wind_kph,sunshine_h\n");
        foreach (var day in days)
        {
            csv.Append($"{day.Date:yyyy-MM-dd},{(day.IsWet ? 1 : 0)},{Csv.Number(day.RainMm)},{Csv.Number(day.MinTemperature)},{Csv.Number(day.MaxTemperature)},{Csv.Number(day.MeanTemperature)},{Csv.Number(day.WindKph)},{Csv.Number(day.SunshineHours)}\n");
        }

        var months = climate.Months.Select(normal =>
        {
            var inMonth = days.Where(d => d.Date.Month == normal.Month).ToList();
            return new MonthSummary(
                normal,
                inMonth.Sum(d => d.RainMm) / years,
                inMonth.Count(d => d.RainMm >= climate.RainDayThresholdMm) / (double)years,
                inMonth.Average(d => d.MeanTemperature),
                inMonth.Sum(d => d.SunshineHours) / years,
                inMonth.Average(d => d.WindKph));
        }).ToList();

        return new WeatherReport(csv.ToString(), months);
    }

    public string SummaryTable()
    {
        var text = new StringBuilder("Month  Rain mm (normal)  Rain days (normal)  Mean °C (normal)  Sun h (normal)  Wind km/h (normal)\n");
        foreach (var m in Months)
        {
            text.Append($"{m.Normal.Month,5}  {m.RainTotalMm,7:0.0} ({m.Normal.RainTotalMm,5:0.0})  {m.RainDays,9:0.0} ({m.Normal.RainDays,6:0.0})  {m.MeanTemperature,7:0.0} ({m.Normal.MeanTemperature,6:0.0})  {m.SunshineHours,5:0} ({m.Normal.SunshineHours,6:0})  {m.WindKph,9:0.0} ({m.Normal.MeanWindKph,6:0.0})\n");
        }
        return text.ToString();
    }

    public sealed record MonthSummary(
        MonthClimate Normal,
        double RainTotalMm,
        double RainDays,
        double MeanTemperature,
        double SunshineHours,
        double WindKph);
}
