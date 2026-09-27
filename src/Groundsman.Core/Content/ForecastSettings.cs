namespace Groundsman.Core.Content
{
    public sealed class ForecastSettings
    {
        public ForecastSettings(
            int days,
            double rangeSpreads,
            double rainErrorSd,
            double rainGrowthPerDay,
            double maxTemperatureErrorSd,
            double maxTemperatureGrowthPerDay)
        {
            if (days < 1 || days > 14)
            {
                throw new ContentException($"forecast.days ({days}) must be from 1 to 14.");
            }
            if (rangeSpreads <= 0)
            {
                throw new ContentException($"forecast.rangeSpreads ({rangeSpreads}) must be above 0.");
            }
            CheckNotNegative("rain.errorSd", rainErrorSd);
            CheckNotNegative("rain.growthPerDay", rainGrowthPerDay);
            CheckNotNegative("maxTemperature.errorSd", maxTemperatureErrorSd);
            CheckNotNegative("maxTemperature.growthPerDay", maxTemperatureGrowthPerDay);

            Days = days;
            RangeSpreads = rangeSpreads;
            RainErrorSd = rainErrorSd;
            RainGrowthPerDay = rainGrowthPerDay;
            MaxTemperatureErrorSd = maxTemperatureErrorSd;
            MaxTemperatureGrowthPerDay = maxTemperatureGrowthPerDay;
        }

        /// <summary>Days covered, starting with the day it's issued.</summary>
        public int Days { get; }

        /// <summary>
        /// Half-width of each range in error spreads. 1.5 holds the truth about 87% of the time.
        /// </summary>
        public double RangeSpreads { get; }

        /// <summary>Spread of the error in a same-day rain forecast, mm.</summary>
        public double RainErrorSd { get; }

        /// <summary>Share the rain error's spread grows by for each day further ahead.</summary>
        public double RainGrowthPerDay { get; }

        /// <summary>Spread of the error in a same-day top temperature forecast, °C.</summary>
        public double MaxTemperatureErrorSd { get; }

        public double MaxTemperatureGrowthPerDay { get; }

        private static void CheckNotNegative(string field, double value)
        {
            if (value < 0)
            {
                throw new ContentException($"forecast.{field} ({value}) can't be negative.");
            }
        }
    }
}
