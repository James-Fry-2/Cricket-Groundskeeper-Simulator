using System.Collections.Generic;

namespace Groundsman.Core.Content
{
    public sealed class ReadingSettings
    {
        public ReadingSettings(
            double moistureProbeWidth,
            double moistureProbeMissRate,
            double feelJudgementSd,
            IReadOnlyList<FeelBand> feelBands,
            double widenPerDay,
            double widenPerMmWater)
        {
            if (moistureProbeWidth <= 0)
            {
                throw new ContentException($"readings.moistureProbe.width ({moistureProbeWidth}) must be above 0.");
            }
            if (moistureProbeMissRate < 0 || moistureProbeMissRate > 1)
            {
                throw new ContentException($"readings.moistureProbe.missRate ({moistureProbeMissRate}) must be from 0 to 1.");
            }
            if (feelJudgementSd < 0)
            {
                throw new ContentException($"readings.feel.judgementSd ({feelJudgementSd}) can't be negative.");
            }
            CheckBands(feelBands);
            if (widenPerDay < 0)
            {
                throw new ContentException($"readings.ageing.widenPerDay ({widenPerDay}) can't be negative.");
            }
            if (widenPerMmWater < 0)
            {
                throw new ContentException($"readings.ageing.widenPerMmWater ({widenPerMmWater}) can't be negative.");
            }

            MoistureProbeWidth = moistureProbeWidth;
            MoistureProbeMissRate = moistureProbeMissRate;
            FeelJudgementSd = feelJudgementSd;
            FeelBands = new List<FeelBand>(feelBands).AsReadOnly();
            WidenPerDay = widenPerDay;
            WidenPerMmWater = widenPerMmWater;
        }

        /// <summary>Width in percentage points of a probe reading by someone of skill 1.</summary>
        public double MoistureProbeWidth { get; }

        /// <summary>Chance a probe reading by someone of skill 1 misses the true value.</summary>
        public double MoistureProbeMissRate { get; }

        /// <summary>Spread, in percentage points, of a feel judgement about the true value at skill 1.</summary>
        public double FeelJudgementSd { get; }

        /// <summary>Words a feel reading can give, driest first, covering moisture from 0 upwards without gaps.</summary>
        public IReadOnlyList<FeelBand> FeelBands { get; }

        /// <summary>Percentage points a reading's range widens for each day since it was taken.</summary>
        public double WidenPerDay { get; }

        /// <summary>Percentage points a reading's range widens for each mm of rain or watering the strip has taken since.</summary>
        public double WidenPerMmWater { get; }

        private static void CheckBands(IReadOnlyList<FeelBand> bands)
        {
            if (bands.Count == 0)
            {
                throw new ContentException("readings.feel.bands needs at least one band.");
            }
            for (var i = 0; i < bands.Count; i++)
            {
                var band = bands[i];
                if (string.IsNullOrWhiteSpace(band.Word))
                {
                    throw new ContentException($"readings.feel.bands[{i}].word can't be blank.");
                }
                var expectedFrom = i == 0 ? 0 : bands[i - 1].To;
                if (band.From != expectedFrom || band.To <= band.From)
                {
                    throw new ContentException($"readings.feel.bands[{i}] must run from {expectedFrom} to something higher: bands start at 0 and follow on without gaps.");
                }
            }
        }
    }
}
