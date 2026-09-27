using System;

namespace Groundsman.Core.Content
{
    /// <summary>Long-run averages for one calendar month.</summary>
    public sealed class MonthClimate
    {
        public MonthClimate(
            int month,
            double meanTemperature,
            double dailyRange,
            double rainDays,
            double rainTotalMm,
            double sunshineHours,
            double meanWindKph,
            double daylightHours)
        {
            Month = month;
            MeanTemperature = meanTemperature;
            DailyRange = dailyRange;
            RainDays = rainDays;
            RainTotalMm = rainTotalMm;
            SunshineHours = sunshineHours;
            MeanWindKph = meanWindKph;
            DaylightHours = daylightHours;
        }

        public int Month { get; }

        /// <summary>Mean of the daily mean temperature, °C.</summary>
        public double MeanTemperature { get; }

        /// <summary>Typical gap between the day's minimum and maximum, °C.</summary>
        public double DailyRange { get; }

        /// <summary>Mean number of days in the month with at least the rain-day threshold.</summary>
        public double RainDays { get; }

        public double RainTotalMm { get; }

        /// <summary>Mean total bright sunshine for the month.</summary>
        public double SunshineHours { get; }

        public double MeanWindKph { get; }

        /// <summary>Sunrise to sunset, mid-month.</summary>
        public double DaylightHours { get; }

        /// <summary>Days in the month, taking February as 28 so normals don't shift in leap years.</summary>
        public int Days => DateTime.DaysInMonth(2001, Month);

        public double WetDayChance => RainDays / Days;

        public double MeanWetDayRainMm => RainTotalMm / RainDays;
    }
}
