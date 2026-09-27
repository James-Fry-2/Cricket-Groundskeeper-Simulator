using System;
using System.Runtime.CompilerServices;
using Groundsman.Core.Content;
using Groundsman.Core.Randomness;

namespace Groundsman.Core.Weather
{
    /// <summary>
    /// Generates true weather one day at a time from monthly normals. Days must be requested in
    /// date order: each day carries on from the last, which is what makes spells.
    /// </summary>
    /// <remarks>
    /// Each day follows one chain of causes: wet or dry, then cloud, then sunshine (none while
    /// rain falls), then temperature, which sunshine widens and warms (or, in winter, chills).
    /// Every effect is measured from the month's average so the normals still hold.
    /// </remarks>
    public sealed class WeatherGenerator
    {
        private const int WarmestHour = 15;
        private const double SolarNoon = 12.5;

        // Calibration is a pure function of the climate, and costly enough that the harness
        // shouldn't redo it for every season it runs.
        private static readonly ConditionalWeakTable<ClimateSettings, SunshineCalibration> Calibrations =
            new ConditionalWeakTable<ClimateSettings, SunshineCalibration>();

        private readonly ClimateSettings _climate;
        private readonly RandomSource _random;
        private readonly SunshineCalibration _sunshine;
        private DateTime? _lastDate;
        private bool _lastWet;
        private double _cloudiness;
        private double _anomaly;

        public WeatherGenerator(ClimateSettings climate, RandomSource random)
        {
            _climate = climate;
            _random = random;
            _sunshine = Calibrations.GetValue(climate, c => new SunshineCalibration(c));
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

            // Draw order is fixed and part of the replay contract: wet, cloud, temperature, wind, rain.
            var wet = _random.Chance(WetChance(month, first));
            _cloudiness = Persist(_cloudiness, _climate.CloudPersistence, 1.0, first);
            var sunshineFraction = _sunshine.Fraction(month.Month, wet, _cloudiness);
            var sunDeviation = sunshineFraction - month.MeanSunshineFraction;
            _anomaly = Persist(_anomaly, _climate.TemperatureAnomalyPersistence, _climate.TemperatureAnomalySd, first)
                + month.SunWarmth * sunDeviation;
            var wind = NextWind(month, wet);
            var rain = wet ? NextRain(month) : new double[24];

            var sunshine = Sunshine(month, sunshineFraction * month.DaylightHours, rain);
            var range = Math.Max(0.2, 1 + _climate.SunshineRangeEffect * sunDeviation) * month.DailyRange;

            var hours = new HourWeather[24];
            for (var h = 0; h < 24; h++)
            {
                var temperature = month.MeanTemperature + _anomaly
                    + range / 2 * Math.Cos(2 * Math.PI * (h - WarmestHour) / 24)
                    - (rain[h] > 0 ? _climate.RainCooling : 0);
                hours[h] = new HourWeather(rain[h], temperature, wind, sunshine[h]);
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

        // Carries part of yesterday's value into today while keeping the long-run spread fixed.
        private double Persist(double previous, double persistence, double spread, bool first) =>
            first
                ? _random.NextGaussian(0, spread)
                : persistence * previous + Math.Sqrt(1 - persistence * persistence) * _random.NextGaussian(0, spread);

        // Log-normal about the wet- or dry-day mean, corrected so the month still averages its
        // normal.
        private double NextWind(MonthClimate month, bool wet)
        {
            var p = month.WetDayChance;
            var factor = _climate.WetDayWindFactor;
            var dryMean = month.MeanWindKph / (p * factor + 1 - p);
            var mean = wet ? dryMean * factor : dryMean;
            var sigma = _climate.WindVariability;
            return mean * Math.Exp(sigma * _random.NextGaussian() - sigma * sigma / 2);
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

        // Spreads the day's sunshine over daylight hours without rain. If rain takes up so much
        // daylight that the sunshine doesn't fit, the excess is lost.
        private static double[] Sunshine(MonthClimate month, double sunshineHours, double[] rain)
        {
            var sunrise = SolarNoon - month.DaylightHours / 2;
            var sunset = SolarNoon + month.DaylightHours / 2;
            var available = new double[24];
            var totalAvailable = 0.0;
            for (var h = 0; h < 24; h++)
            {
                if (rain[h] > 0)
                {
                    continue;
                }
                available[h] = Math.Max(0, Math.Min(h + 1, sunset) - Math.Max(h, sunrise));
                totalAvailable += available[h];
            }

            var share = totalAvailable > 0 ? Math.Min(1, sunshineHours / totalAvailable) : 0;
            var sunshine = new double[24];
            for (var h = 0; h < 24; h++)
            {
                sunshine[h] = available[h] * share;
            }
            return sunshine;
        }
    }
}
