using Groundsman.Core.Content;
using Groundsman.Core.Randomness;
using Groundsman.Core.Staff;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Core.Readings
{
    internal sealed class ReadingTaker
    {
        private readonly ReadingSettings _settings;
        private readonly RandomSource _random;

        public ReadingTaker(ReadingSettings settings, RandomSource random)
        {
            _settings = settings;
            _random = random;
        }

        public Reading ProbeSurfaceMoisture(StripState strip, GameTime now, StaffId takenBy)
        {
            // Place the true value at a random point in the range: a range centred on the truth
            // would give it away as the midpoint.
            var width = _settings.MoistureProbeWidth;
            var low = strip.SurfaceMoisture - _random.NextDouble() * width;
            var range = new ValueRange(low, low + width);

            return new Reading(strip.Id, Quantity.SurfaceMoisture, range, now, ReadingSource.MoistureProbe, takenBy);
        }
    }
}
