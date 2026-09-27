using System;
using System.Collections.Generic;
using System.Linq;

namespace Groundsman.Core.Weather
{
    public sealed class DayWeather
    {
        public DayWeather(DateTime date, bool isWet, double windKph, IReadOnlyList<HourWeather> hours)
        {
            Date = date;
            IsWet = isWet;
            WindKph = windKph;
            Hours = hours;
            RainMm = hours.Sum(h => h.RainMm);
            SunshineHours = hours.Sum(h => h.Sunshine);
            MeanTemperature = hours.Average(h => h.Temperature);
            MinTemperature = hours.Min(h => h.Temperature);
            MaxTemperature = hours.Max(h => h.Temperature);
        }

        public DateTime Date { get; }
        public bool IsWet { get; }
        public double WindKph { get; }

        /// <summary>Hours 0 to 23 of the day.</summary>
        public IReadOnlyList<HourWeather> Hours { get; }

        public double RainMm { get; }
        public double SunshineHours { get; }
        public double MeanTemperature { get; }
        public double MinTemperature { get; }
        public double MaxTemperature { get; }
    }
}
