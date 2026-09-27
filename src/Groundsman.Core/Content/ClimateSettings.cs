using System;
using System.Collections.Generic;

namespace Groundsman.Core.Content
{
    public sealed class ClimateSettings
    {
        public ClimateSettings(
            double rainDayThresholdMm,
            double wetDayPersistence,
            int rainSpellMinHours,
            int rainSpellMaxHours,
            double temperatureAnomalySd,
            double temperatureAnomalyPersistence,
            double windVariability,
            double wetDaySunshineFactor,
            double cloudVariability,
            double cloudPersistence,
            double sunshineRangeEffect,
            double rainCooling,
            double wetDayWindFactor,
            IReadOnlyList<MonthClimate> months)
        {
            if (rainDayThresholdMm <= 0)
            {
                throw new ContentException($"climate.rainDayThresholdMm ({rainDayThresholdMm}) must be above 0.");
            }
            CheckFraction("wetDayPersistence", wetDayPersistence);
            if (rainSpellMinHours < 1 || rainSpellMaxHours > 24 || rainSpellMinHours > rainSpellMaxHours)
            {
                throw new ContentException($"climate.rainSpellHours ({rainSpellMinHours} to {rainSpellMaxHours}) must run from at least 1 to at most 24, min no more than max.");
            }
            if (temperatureAnomalySd < 0)
            {
                throw new ContentException($"climate.temperatureAnomalySd ({temperatureAnomalySd}) can't be negative.");
            }
            CheckFraction("temperatureAnomalyPersistence", temperatureAnomalyPersistence);
            if (windVariability < 0)
            {
                throw new ContentException($"climate.windVariability ({windVariability}) can't be negative.");
            }
            if (wetDaySunshineFactor < 0 || wetDaySunshineFactor > 1)
            {
                throw new ContentException($"climate.wetDaySunshineFactor ({wetDaySunshineFactor}) must be from 0 to 1.");
            }
            if (cloudVariability < 0)
            {
                throw new ContentException($"climate.cloudVariability ({cloudVariability}) can't be negative.");
            }
            CheckFraction("cloudPersistence", cloudPersistence);
            if (sunshineRangeEffect < 0)
            {
                throw new ContentException($"climate.sunshineRangeEffect ({sunshineRangeEffect}) can't be negative.");
            }
            if (rainCooling < 0)
            {
                throw new ContentException($"climate.rainCooling ({rainCooling}) can't be negative.");
            }
            if (wetDayWindFactor <= 0)
            {
                throw new ContentException($"climate.wetDayWindFactor ({wetDayWindFactor}) must be above 0.");
            }
            if (months.Count != 12)
            {
                throw new ContentException($"climate.months needs all 12 months, got {months.Count}.");
            }
            for (var i = 0; i < months.Count; i++)
            {
                CheckMonth(i, months[i], rainDayThresholdMm, wetDaySunshineFactor);
            }

            RainDayThresholdMm = rainDayThresholdMm;
            WetDayPersistence = wetDayPersistence;
            RainSpellMinHours = rainSpellMinHours;
            RainSpellMaxHours = rainSpellMaxHours;
            TemperatureAnomalySd = temperatureAnomalySd;
            TemperatureAnomalyPersistence = temperatureAnomalyPersistence;
            WindVariability = windVariability;
            WetDaySunshineFactor = wetDaySunshineFactor;
            CloudVariability = cloudVariability;
            CloudPersistence = cloudPersistence;
            SunshineRangeEffect = sunshineRangeEffect;
            RainCooling = rainCooling;
            WetDayWindFactor = wetDayWindFactor;
            Months = new List<MonthClimate>(months).AsReadOnly();
        }

        /// <summary>Least rain that makes a day count as a rain day, as in published normals.</summary>
        public double RainDayThresholdMm { get; }

        /// <summary>0 for independent days, towards 1 for long wet and dry spells.</summary>
        public double WetDayPersistence { get; }

        public int RainSpellMinHours { get; }
        public int RainSpellMaxHours { get; }

        /// <summary>Spread of the daily mean temperature about the monthly mean, °C.</summary>
        public double TemperatureAnomalySd { get; }

        /// <summary>How much of today's temperature anomaly carries into tomorrow, 0 to under 1.</summary>
        public double TemperatureAnomalyPersistence { get; }

        /// <summary>Log-scale spread of daily wind about the monthly mean.</summary>
        public double WindVariability { get; }

        /// <summary>Sunshine on a wet day as a fraction of a dry day's.</summary>
        public double WetDaySunshineFactor { get; }

        /// <summary>Day-to-day spread of cloud cover, on a log-odds scale.</summary>
        public double CloudVariability { get; }

        /// <summary>How much of today's cloudiness carries into tomorrow, 0 to under 1.</summary>
        public double CloudPersistence { get; }

        /// <summary>
        /// Daily temperature range grows by this share per unit of sunshine fraction above the
        /// month's average: clear skies give warmer afternoons and colder nights.
        /// </summary>
        public double SunshineRangeEffect { get; }

        /// <summary>°C taken off each hour with rain falling.</summary>
        public double RainCooling { get; }

        /// <summary>Mean wind on wet days as a multiple of dry days'.</summary>
        public double WetDayWindFactor { get; }

        public IReadOnlyList<MonthClimate> Months { get; }

        public MonthClimate ForMonth(int month) => Months[month - 1];

        private static void CheckFraction(string field, double value)
        {
            if (value < 0 || value >= 1)
            {
                throw new ContentException($"climate.{field} ({value}) must be from 0 to under 1.");
            }
        }

        private static void CheckMonth(int index, MonthClimate month, double threshold, double wetSunFactor)
        {
            var path = $"climate.months[{index}]";
            if (month.Month != index + 1)
            {
                throw new ContentException($"{path}.month ({month.Month}) must be {index + 1}: months run January to December in order.");
            }
            if (month.RainDays <= 0 || month.RainDays > month.Days)
            {
                throw new ContentException($"{path}.rainDays ({month.RainDays}) must be above 0 and at most {month.Days}.");
            }
            if (month.RainTotalMm < month.RainDays * threshold)
            {
                throw new ContentException($"{path}.rainTotalMm ({month.RainTotalMm}) is less than rainDays × rainDayThresholdMm.");
            }
            if (month.DailyRange < 0 || month.MeanWindKph < 0)
            {
                throw new ContentException($"{path}: dailyRange and meanWindKph can't be negative.");
            }
            if (month.DaylightHours <= 0 || month.DaylightHours > 24)
            {
                throw new ContentException($"{path}.daylightHours ({month.DaylightHours}) must be above 0 and at most 24.");
            }

            // Dry days get more sun than wet ones. The dry-day average has to stay below all-day
            // sun, or no spread of sunny and cloudy days could produce it.
            if (month.SunshineHours <= 0 || month.DryDaySunshineFraction(wetSunFactor) >= 1)
            {
                throw new ContentException($"{path}.sunshineHours ({month.SunshineHours}) doesn't fit in the month's daylight.");
            }
        }
    }
}
