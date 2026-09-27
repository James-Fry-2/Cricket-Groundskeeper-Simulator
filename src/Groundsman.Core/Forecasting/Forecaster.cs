using System;
using System.Collections.Generic;
using Groundsman.Core.Content;
using Groundsman.Core.Randomness;
using Groundsman.Core.Readings;
using Groundsman.Core.Weather;

namespace Groundsman.Core.Forecasting
{
    /// <summary>
    /// Issues a forecast once per day: the true weather plus an error drawn at issue, with
    /// spread growing the further ahead the day is. Range widths depend only on how far ahead,
    /// never on the truth, so a narrow range can't give the answer away.
    /// </summary>
    internal sealed class Forecaster
    {
        private const double IntegrationStepMm = 0.25;

        private readonly ForecastSettings _settings;
        private readonly ClimateSettings _climate;
        private readonly WeatherSystem _weather;
        private readonly RandomSource _random;
        private DateTime? _issuedOn;

        public Forecaster(ForecastSettings settings, ClimateSettings climate, WeatherSystem weather, RandomSource random)
        {
            _settings = settings;
            _climate = climate;
            _weather = weather;
            _random = random;
        }

        public IReadOnlyList<DayForecast> Current { get; private set; } = Array.Empty<DayForecast>();

        public void IssueIfNewDay(DateTime today)
        {
            today = today.Date;
            if (_issuedOn == today)
            {
                return;
            }

            var days = new DayForecast[_settings.Days];
            for (var lead = 0; lead < days.Length; lead++)
            {
                var date = today.AddDays(lead);
                var truth = _weather.Day(date);

                // Draw order is fixed and part of the replay contract: rain, then temperature.
                var rainSpread = _settings.RainErrorSd * (1 + _settings.RainGrowthPerDay * lead);
                var rain = truth.RainMm + _random.NextGaussian(0, rainSpread);
                var temperatureSpread = _settings.MaxTemperatureErrorSd * (1 + _settings.MaxTemperatureGrowthPerDay * lead);
                var temperature = truth.MaxTemperature + _random.NextGaussian(0, temperatureSpread);

                var rainHalfWidth = _settings.RangeSpreads * rainSpread;
                var temperatureHalfWidth = _settings.RangeSpreads * temperatureSpread;
                days[lead] = new DayForecast(
                    date,
                    today,
                    new ValueRange(Math.Max(0, rain - rainHalfWidth), Math.Max(0, rain + rainHalfWidth)),
                    ChanceOfRain(rain, rainSpread, _climate.ForMonth(date.Month)),
                    new ValueRange(temperature - temperatureHalfWidth, temperature + temperatureHalfWidth));
            }

            Current = days;
            _issuedOn = today;
        }

        // How likely a rain day is, given the forecast value and the month's climate. Weighing by
        // how often it rains matters: on its own a forecast value a little above the threshold
        // is more often a dry day with a high error than a wet day. Uses only the forecast value
        // and published normals, never the truth.
        private double ChanceOfRain(double forecastMm, double spread, MonthClimate month)
        {
            var threshold = _climate.RainDayThresholdMm;
            if (spread <= 0)
            {
                return forecastMm >= threshold ? 1 : 0;
            }

            var excessMean = month.MeanWetDayRainMm - threshold;
            double wetLikelihood;
            if (excessMean <= 0)
            {
                wetLikelihood = Likelihood(forecastMm, threshold, spread);
            }
            else
            {
                wetLikelihood = 0;
                for (var amount = threshold; amount < threshold + 12 * excessMean; amount += IntegrationStepMm)
                {
                    var density = Math.Exp(-(amount - threshold) / excessMean) / excessMean;
                    wetLikelihood += density * Likelihood(forecastMm, amount, spread) * IntegrationStepMm;
                }
            }

            var wet = month.WetDayChance * wetLikelihood;
            var dry = (1 - month.WetDayChance) * Likelihood(forecastMm, 0, spread);
            return wet + dry > 0 ? wet / (wet + dry) : month.WetDayChance;
        }

        // Normal density up to a constant factor, which cancels in the ratio above.
        private static double Likelihood(double forecast, double truth, double spread)
        {
            var z = (forecast - truth) / spread;
            return Math.Exp(-z * z / 2);
        }
    }
}
