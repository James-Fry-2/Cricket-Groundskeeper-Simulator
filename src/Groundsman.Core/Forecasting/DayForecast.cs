using System;
using Groundsman.Core.Readings;

namespace Groundsman.Core.Forecasting
{
    public sealed class DayForecast
    {
        public DayForecast(DateTime date, DateTime issuedOn, ValueRange rain, double chanceOfRain, ValueRange maxTemperature)
        {
            Date = date;
            IssuedOn = issuedOn;
            Rain = rain;
            ChanceOfRain = chanceOfRain;
            MaxTemperature = maxTemperature;
        }

        public DateTime Date { get; }
        public DateTime IssuedOn { get; }

        /// <summary>Rain for the whole day, mm.</summary>
        public ValueRange Rain { get; }

        /// <summary>Chance, 0 to 1, of at least the rain-day threshold falling.</summary>
        public double ChanceOfRain { get; }

        /// <summary>The day's highest temperature, °C.</summary>
        public ValueRange MaxTemperature { get; }
    }
}
