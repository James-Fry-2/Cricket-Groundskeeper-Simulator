using System;
using Groundsman.Core.Content;
using Groundsman.Core.Randomness;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Core.Readings
{
    internal sealed class ReadingTaker
    {
        // How far past the truth a missed probe reading can land, as a share of its width.
        private const double MaxMissGapShare = 0.25;

        private readonly ReadingSettings _settings;
        private readonly RandomSource _random;

        public ReadingTaker(ReadingSettings settings, RandomSource random)
        {
            _settings = settings;
            _random = random;
        }

        public Reading Probe(StripState strip, GameTime now, StaffMemberSettings takenBy)
        {
            var truth = strip.SurfaceMoisture;
            var width = _settings.MoistureProbeWidth * takenBy.ReadingSkill;

            // Draw order is fixed and part of the replay contract: miss, position, then side.
            var missed = _random.Chance(Math.Min(1, _settings.MoistureProbeMissRate * takenBy.ReadingSkill));
            double low;
            if (!missed)
            {
                // The true value sits at a random point in the range: a range centred on the
                // truth would give it away as the midpoint.
                low = truth - _random.NextDouble() * width;
            }
            else
            {
                var gap = _random.NextDouble() * MaxMissGapShare * width;
                low = _random.Chance(0.5) ? truth + gap : truth - gap - width;
            }

            return new Reading(strip.Id, Quantity.SurfaceMoisture, new ValueRange(low, low + width), now, ReadingSource.MoistureProbe, takenBy.Id);
        }

        public Reading Feel(StripState strip, GameTime now, StaffMemberSettings takenBy)
        {
            var judged = _random.NextGaussian(strip.SurfaceMoisture, _settings.FeelJudgementSd * takenBy.ReadingSkill);

            var bands = _settings.FeelBands;
            var band = bands[bands.Count - 1];
            for (var i = 0; i < bands.Count; i++)
            {
                if (judged < bands[i].To)
                {
                    band = bands[i];
                    break;
                }
            }

            return new Reading(strip.Id, Quantity.SurfaceMoisture, new ValueRange(band.From, band.To), now, ReadingSource.Feel, takenBy.Id, band.Word);
        }
    }
}
