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
        private readonly ForecastSettings _settings;
        private readonly WeatherSystem _weather;
        private readonly RandomSource _random;
        private DateTime? _issuedOn;

        public Forecaster(ForecastSettings settings, WeatherSystem weather, RandomSource random)
        {
            _settings = settings;
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
                    new ValueRange(temperature - temperatureHalfWidth, temperature + temperatureHalfWidth));
            }

            Current = days;
            _issuedOn = today;
        }
    }
}
