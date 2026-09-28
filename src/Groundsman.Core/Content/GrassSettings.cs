namespace Groundsman.Core.Content
{
    public sealed class GrassSettings
    {
        public GrassSettings(
            double startingCover,
            double startingHeightMm,
            double startingRootDepthMm,
            double minTemperature,
            double optimumTemperature,
            double maxTemperature,
            double heightPerDayMm,
            double coverPerDay,
            double maxCover,
            double rootsPerDayMm,
            double maxRootDepthMm,
            double droughtWaterBelow,
            double droughtCoverLossPerDay,
            double minMowHeightMm,
            double maxMowHeightMm,
            double scalpShare,
            double scalpCoverLossPerMm)
        {
            if (maxCover <= 0 || maxCover > 100)
            {
                throw new ContentException($"grass.growth.maxCover ({maxCover}) must be above 0 and at most 100.");
            }
            if (startingCover < 0 || startingCover > maxCover)
            {
                throw new ContentException($"grass.starting.cover ({startingCover}) must be from 0 to maxCover ({maxCover}).");
            }
            if (maxRootDepthMm <= 0)
            {
                throw new ContentException($"grass.growth.maxRootDepthMm ({maxRootDepthMm}) must be above 0.");
            }
            if (startingHeightMm < 0 || startingRootDepthMm < 0 || startingRootDepthMm > maxRootDepthMm)
            {
                throw new ContentException("grass.starting: heightMm can't be negative and rootDepthMm must be from 0 to maxRootDepthMm.");
            }
            if (!(minTemperature < optimumTemperature && optimumTemperature < maxTemperature))
            {
                throw new ContentException($"grass.temperature must run min ({minTemperature}) < optimum ({optimumTemperature}) < max ({maxTemperature}).");
            }
            CheckNotNegative("growth.heightPerDayMm", heightPerDayMm);
            CheckNotNegative("growth.coverPerDay", coverPerDay);
            CheckNotNegative("growth.rootsPerDayMm", rootsPerDayMm);
            if (droughtWaterBelow < 0 || droughtWaterBelow > 1)
            {
                throw new ContentException($"grass.drought.waterBelow ({droughtWaterBelow}) must be from 0 to 1.");
            }
            CheckNotNegative("drought.coverLossPerDay", droughtCoverLossPerDay);
            if (minMowHeightMm <= 0 || maxMowHeightMm <= minMowHeightMm)
            {
                throw new ContentException($"grass.mowing.minHeightMm ({minMowHeightMm}) must be above 0 and below maxHeightMm ({maxMowHeightMm}).");
            }
            if (scalpShare <= 0 || scalpShare >= 1)
            {
                throw new ContentException($"grass.mowing.scalpShare ({scalpShare}) must be between 0 and 1.");
            }
            CheckNotNegative("mowing.scalpCoverLossPerMm", scalpCoverLossPerMm);

            StartingCover = startingCover;
            StartingHeightMm = startingHeightMm;
            StartingRootDepthMm = startingRootDepthMm;
            MinTemperature = minTemperature;
            OptimumTemperature = optimumTemperature;
            MaxTemperature = maxTemperature;
            HeightPerDayMm = heightPerDayMm;
            CoverPerDay = coverPerDay;
            MaxCover = maxCover;
            RootsPerDayMm = rootsPerDayMm;
            MaxRootDepthMm = maxRootDepthMm;
            DroughtWaterBelow = droughtWaterBelow;
            DroughtCoverLossPerDay = droughtCoverLossPerDay;
            MinMowHeightMm = minMowHeightMm;
            MaxMowHeightMm = maxMowHeightMm;
            ScalpShare = scalpShare;
            ScalpCoverLossPerMm = scalpCoverLossPerMm;
        }

        public double StartingCover { get; }
        public double StartingHeightMm { get; }
        public double StartingRootDepthMm { get; }

        /// <summary>Below this, °C, grass doesn't grow.</summary>
        public double MinTemperature { get; }

        public double OptimumTemperature { get; }

        /// <summary>Above this, °C, grass stops growing in the heat.</summary>
        public double MaxTemperature { get; }

        /// <summary>Height added per day at the best temperature with water freely available.</summary>
        public double HeightPerDayMm { get; }

        /// <summary>Cover, in percentage points, recovered per day in ideal conditions.</summary>
        public double CoverPerDay { get; }

        public double MaxCover { get; }
        public double RootsPerDayMm { get; }
        public double MaxRootDepthMm { get; }

        /// <summary>Water availability at depth, 0 to 1, below which grass stops growing and cover thins.</summary>
        public double DroughtWaterBelow { get; }

        public double DroughtCoverLossPerDay { get; }
        public double MinMowHeightMm { get; }
        public double MaxMowHeightMm { get; }

        /// <summary>Largest share of the height one cut can take without scalping.</summary>
        public double ScalpShare { get; }

        /// <summary>Cover lost, in percentage points, per mm cut beyond the scalping limit.</summary>
        public double ScalpCoverLossPerMm { get; }

        private static void CheckNotNegative(string field, double value)
        {
            if (value < 0)
            {
                throw new ContentException($"grass.{field} ({value}) can't be negative.");
            }
        }
    }
}
