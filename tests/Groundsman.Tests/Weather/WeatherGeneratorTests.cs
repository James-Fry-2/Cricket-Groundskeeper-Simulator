using Groundsman.Core.Content;
using Groundsman.Core.Randomness;
using Groundsman.Core.Weather;

namespace Groundsman.Tests.Weather;

public class WeatherGeneratorTests
{
    private const int Years = 200;

    private static readonly ClimateSettings Climate =
        ContentParser.ParseClimate(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "climate.json")));

    // Generated once and shared: the statistical tests all read the same 200 years.
    private static readonly Lazy<List<DayWeather>> LongRun = new(() => Generate(seed: 1, new DateTime(2001, 1, 1), 365 * Years));

    private static List<DayWeather> Generate(ulong seed, DateTime start, int days)
    {
        var generator = new WeatherGenerator(Climate, new RandomStreams(seed).Get(RandomStream.Weather));
        return Enumerable.Range(0, days).Select(i => generator.Next(start.AddDays(i))).ToList();
    }

    [Fact]
    public void Monthly_rain_totals_match_the_normals()
    {
        foreach (var month in Climate.Months)
        {
            var mean = LongRun.Value.Where(d => d.Date.Month == month.Month).Sum(d => d.RainMm) / Years;

            Assert.InRange(mean, month.RainTotalMm * 0.9, month.RainTotalMm * 1.1);
        }
    }

    [Fact]
    public void Monthly_rain_days_match_the_normals()
    {
        foreach (var month in Climate.Months)
        {
            var mean = LongRun.Value.Count(d => d.Date.Month == month.Month && d.RainMm >= Climate.RainDayThresholdMm) / (double)Years;

            Assert.InRange(mean, month.RainDays - 0.75, month.RainDays + 0.75);
        }
    }

    [Fact]
    public void Monthly_temperature_sunshine_and_wind_match_the_normals()
    {
        foreach (var month in Climate.Months)
        {
            var days = LongRun.Value.Where(d => d.Date.Month == month.Month).ToList();

            Assert.InRange(days.Average(d => d.MeanTemperature), month.MeanTemperature - 0.3, month.MeanTemperature + 0.3);
            Assert.InRange(days.Sum(d => d.SunshineHours) / Years, month.SunshineHours * 0.95, month.SunshineHours * 1.05);
            Assert.InRange(days.Average(d => d.WindKph), month.MeanWindKph * 0.95, month.MeanWindKph * 1.05);
        }
    }

    [Fact]
    public void Wet_days_come_in_spells()
    {
        var days = LongRun.Value;
        var wetRate = days.Count(d => d.IsWet) / (double)days.Count;
        var afterWet = days.Skip(1).Where((_, i) => days[i].IsWet).ToList();
        var wetAfterWet = afterWet.Count(d => d.IsWet) / (double)afterWet.Count;

        Assert.True(wetAfterWet > wetRate + 0.1, $"Wet after wet {wetAfterWet:0.00} vs overall {wetRate:0.00}");
    }

    [Fact]
    public void Dry_days_have_no_rain_and_wet_days_at_least_the_threshold()
    {
        foreach (var day in LongRun.Value.Take(3650))
        {
            if (day.IsWet)
            {
                Assert.True(day.RainMm >= Climate.RainDayThresholdMm);
            }
            else
            {
                Assert.Equal(0, day.RainMm);
            }
        }
    }

    [Fact]
    public void Rain_falls_in_one_unbroken_spell_of_allowed_length()
    {
        foreach (var day in LongRun.Value.Take(3650).Where(d => d.IsWet))
        {
            var rainyHours = Enumerable.Range(0, 24).Where(h => day.Hours[h].RainMm > 0).ToList();

            Assert.InRange(rainyHours.Count, Climate.RainSpellMinHours, Climate.RainSpellMaxHours);
            Assert.Equal(rainyHours.Count - 1, rainyHours[^1] - rainyHours[0]);
            Assert.Equal(day.RainMm, day.Hours.Sum(h => h.RainMm), 9);
        }
    }

    [Fact]
    public void Days_are_coolest_before_dawn_and_warmest_mid_afternoon()
    {
        var day = LongRun.Value[180];
        var hours = day.Hours.Select(h => h.Temperature).ToList();

        Assert.InRange(hours.IndexOf(hours.Min()), 2, 3);
        Assert.InRange(hours.IndexOf(hours.Max()), 14, 15);
        Assert.Equal(Climate.ForMonth(day.Date.Month).DailyRange, day.MaxTemperature - day.MinTemperature, 1);
    }

    [Fact]
    public void Sunshine_only_falls_in_daylight()
    {
        foreach (var day in LongRun.Value.Take(365))
        {
            Assert.Equal(0, day.Hours[0].Sunshine);
            Assert.Equal(0, day.Hours[23].Sunshine);
            Assert.All(day.Hours, h => Assert.InRange(h.Sunshine, 0.0, 1.0));
            Assert.Equal(day.SunshineHours, day.Hours.Sum(h => h.Sunshine), 9);
        }
    }

    [Fact]
    public void The_same_seed_gives_the_same_weather()
    {
        var a = Generate(seed: 5, new DateTime(2027, 4, 1), 60);
        var b = Generate(seed: 5, new DateTime(2027, 4, 1), 60);

        Assert.Equal(a.Select(d => d.RainMm), b.Select(d => d.RainMm));
        Assert.Equal(a.Select(d => d.MeanTemperature), b.Select(d => d.MeanTemperature));
    }

    [Fact]
    public void Days_must_be_generated_in_order()
    {
        var generator = new WeatherGenerator(Climate, new RandomStreams(1).Get(RandomStream.Weather));
        generator.Next(new DateTime(2027, 4, 1));

        Assert.Throws<ArgumentException>(() => generator.Next(new DateTime(2027, 4, 3)));
    }
}
