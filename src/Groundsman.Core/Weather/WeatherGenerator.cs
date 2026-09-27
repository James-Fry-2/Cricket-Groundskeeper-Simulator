using System;
using Groundsman.Core.Content;
using Groundsman.Core.Randomness;

namespace Groundsman.Core.Weather
{
    /// <summary>
    /// Generates true weather one day at a time from monthly normals. Days must be requested in
    /// date order: each day carries on from the last, which is what makes spells.
    /// </summary>
    public sealed class WeatherGenerator
    {
        private const int WarmestHour = 15;
        private const double SolarNoon = 12.5;

        private readonly ClimateSettings _climate;
        private readonly RandomSource _random;
        private DateTime? _lastDate;
        private bool _lastWet;
        private double _anomaly;

        public WeatherGenerator(ClimateSettings climate, RandomSource random)
        {
            _climate = climate;
            _random = random;
        }

        public DayWeather Next(DateTime date)
        {
            date = date.Date;
            if (_lastDate.HasValue && date != _lastDate.Value.AddDays(1))
            {
                throw new ArgumentException($"Expected {_lastDate.Value.AddDays(1):yyyy-MM-dd}, got {date:yyyy-MM-dd}.", nameof(date));
            }

            var month = _climate.ForMonth(date.Month);
            var first = !_lastDate.HasValue;

            // Draw order is fixed and part of the replay contract: wet, temperature, wind, rain.
            var wet = _random.Chance(WetChance(month, first));
            _anomaly = first
                ? _random.NextGaussian(0, _climate.TemperatureAnomalySd)
                : NextAnomaly();
            var wind = NextWind(month);
            var hours = new HourWeather[24];
            var rain = wet ? NextRain(month) : new double[24];
            var sunshine = Sunshine(month, wet);

            for (var h = 0; h < 24; h++)
            {
                hours[h] = new HourWeather(rain[h], Temperature(month, h), wind, sunshine[h]);
            }

            _lastDate = date;
            _lastWet = wet;
            return new DayWeather(date, wet, wind, hours);
        }

        // A two-state chain whose long-run wet rate is the month's rain-day rate, with
        // persistence pulling tomorrow towards today.
        private double WetChance(MonthClimate month, bool first)
        {
            var p = month.WetDayChance;
            if (first)
            {
                return p;
            }

            var r = _climate.WetDayPersistence;
            return _lastWet ? p + r * (1 - p) : p * (1 - r);
        }

        private double NextAnomaly()
        {
            var phi = _climate.TemperatureAnomalyPersistence;
            return phi * _anomaly + Math.Sqrt(1 - phi * phi) * _random.NextGaussian(0, _climate.TemperatureAnomalySd);
        }

        // Log-normal about the monthly mean, corrected so the average stays at the mean.
        private double NextWind(MonthClimate month)
        {
            var sigma = _climate.WindVariability;
            return month.MeanWindKph * Math.Exp(sigma * _random.NextGaussian() - sigma * sigma / 2);
        }

        private double[] NextRain(MonthClimate month)
        {
            var threshold = _climate.RainDayThresholdMm;
            var excessMean = month.MeanWetDayRainMm - threshold;
            var total = threshold + (excessMean > 0 ? _random.NextExponential(excessMean) : 0);

            var length = _random.NextInt(_climate.RainSpellMinHours, _climate.RainSpellMaxHours + 1);
            var start = _random.NextInt(0, 24 - length + 1);

            var rain = new double[24];
            for (var h = start; h < start + length; h++)
            {
                rain[h] = total / length;
            }
            return rain;
        }

        private double Temperature(MonthClimate month, int hour) =>
            month.MeanTemperature + _anomaly + month.DailyRange / 2 * Math.Cos(2 * Math.PI * (hour - WarmestHour) / 24);

        private double[] Sunshine(MonthClimate month, bool wet)
        {
            var p = month.WetDayChance;
            var factor = _climate.WetDaySunshineFactor;
            var dryDay = month.SunshineHours / month.Days / (p * factor + 1 - p);
            var daySunshine = wet ? dryDay * factor : dryDay;

            var sunrise = SolarNoon - month.DaylightHours / 2;
            var sunset = SolarNoon + month.DaylightHours / 2;
            var sunshine = new double[24];
            for (var h = 0; h < 24; h++)
            {
                var daylight = Math.Max(0, Math.Min(h + 1, sunset) - Math.Max(h, sunrise));
                sunshine[h] = daylight * daySunshine / month.DaylightHours;
            }
            return sunshine;
        }
    }
}
