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
            // Wider on the low side: rain spells can fill most of a short winter day, and
            // sunshine that doesn't fit in the dry daylight hours is lost.
            Assert.InRange(days.Sum(d => d.SunshineHours) / Years, month.SunshineHours * 0.93, month.SunshineHours * 1.05);
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
    public void Dry_days_are_coolest_before_dawn_and_warmest_mid_afternoon()
    {
        foreach (var day in LongRun.Value.Take(365).Where(d => !d.IsWet))
        {
            var hours = day.Hours.Select(h => h.Temperature).ToList();

            Assert.Equal(3, hours.IndexOf(hours.Min()));
            Assert.Equal(15, hours.IndexOf(hours.Max()));
        }
    }

    [Fact]
    public void Sunshine_only_falls_in_dry_daylight_hours()
    {
        foreach (var day in LongRun.Value.Take(3650))
        {
            Assert.Equal(0, day.Hours[0].Sunshine);
            Assert.Equal(0, day.Hours[23].Sunshine);
            Assert.All(day.Hours, h => Assert.InRange(h.Sunshine, 0.0, 1.0));
            Assert.All(day.Hours.Where(h => h.RainMm > 0), h => Assert.Equal(0, h.Sunshine));
            Assert.Equal(day.SunshineHours, day.Hours.Sum(h => h.Sunshine), 9);
        }
    }

    [Fact]
    public void Sunshine_varies_from_day_to_day()
    {
        var julyDryDays = LongRun.Value.Where(d => d.Date.Month == 7 && !d.IsWet).Select(d => d.SunshineHours).ToList();

        Assert.True(julyDryDays.Min() < 3, $"Dullest dry July day had {julyDryDays.Min():0.0} h of sun");
        Assert.True(julyDryDays.Max() > 13, $"Sunniest dry July day had {julyDryDays.Max():0.0} h of sun");
    }

    [Fact]
    public void Wet_days_are_duller_and_windier_than_dry_days()
    {
        var days = LongRun.Value.Where(d => d.Date.Month == 6).ToList();

        Assert.True(days.Where(d => d.IsWet).Average(d => d.SunshineHours) < days.Where(d => !d.IsWet).Average(d => d.SunshineHours));
        Assert.True(days.Where(d => d.IsWet).Average(d => d.WindKph) > days.Where(d => !d.IsWet).Average(d => d.WindKph));
    }

    [Fact]
    public void Sunny_days_have_a_wider_temperature_range()
    {
        var dryJuly = LongRun.Value.Where(d => d.Date.Month == 7 && !d.IsWet).OrderBy(d => d.SunshineHours).ToList();
        var quarter = dryJuly.Count / 4;

        var dullRange = dryJuly.Take(quarter).Average(d => d.MaxTemperature - d.MinTemperature);
        var sunnyRange = dryJuly.Skip(dryJuly.Count - quarter).Average(d => d.MaxTemperature - d.MinTemperature);

        Assert.True(sunnyRange > dullRange + 3, $"Sunny range {sunnyRange:0.0} vs dull {dullRange:0.0}");
    }

    [Fact]
    public void Sunny_summer_days_are_warmer_and_sunny_winter_nights_colder()
    {
        static (double Dull, double Sunny) Quartiles(IEnumerable<DayWeather> days, Func<DayWeather, double> value)
        {
            var sorted = days.Where(d => !d.IsWet).OrderBy(d => d.SunshineHours).ToList();
            var quarter = sorted.Count / 4;
            return (sorted.Take(quarter).Average(value), sorted.Skip(sorted.Count - quarter).Average(value));
        }

        var july = Quartiles(LongRun.Value.Where(d => d.Date.Month == 7), d => d.MaxTemperature);
        var january = Quartiles(LongRun.Value.Where(d => d.Date.Month == 1), d => d.MinTemperature);

        Assert.True(july.Sunny > july.Dull + 3, $"July max: sunny {july.Sunny:0.0} vs dull {july.Dull:0.0}");
        Assert.True(january.Sunny < january.Dull - 1, $"January min: sunny {january.Sunny:0.0} vs dull {january.Dull:0.0}");
    }

    [Fact]
    public void Hot_days_happen_in_summer_and_cluster()
    {
        var days = LongRun.Value;
        var julyHotDaysPerYear = days.Count(d => d.Date.Month == 7 && d.MaxTemperature >= 25) / (double)Years;

        var summer = days.Where(d => d.Date.Month >= 6 && d.Date.Month <= 8).ToList();
        var hotRate = summer.Count(d => d.MaxTemperature >= 25) / (double)summer.Count;
        var afterHot = summer.Skip(1).Where((_, i) => summer[i].MaxTemperature >= 25 && summer[i + 1].Date == summer[i].Date.AddDays(1)).ToList();
        var hotAfterHot = afterHot.Count(d => d.MaxTemperature >= 25) / (double)afterHot.Count;

        Assert.InRange(julyHotDaysPerYear, 3, 8);
        Assert.True(hotAfterHot > hotRate * 3, $"Hot after hot {hotAfterHot:0.00} vs summer rate {hotRate:0.00}");
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
