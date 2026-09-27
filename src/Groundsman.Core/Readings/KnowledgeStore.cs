using System;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Core.Readings
{
    /// <summary>
    /// Everything the player knows about the strips. The view is built from here, never from
    /// true state.
    /// </summary>
    internal sealed class KnowledgeStore
    {
        private readonly ReadingSettings _settings;
        private readonly Known _surface;
        private readonly Known _subsurface;

        public KnowledgeStore(ReadingSettings settings, int stripCount)
        {
            _settings = settings;
            _surface = new Known(stripCount);
            _subsurface = new Known(stripCount);
        }

        public Reading? LatestSurfaceMoisture(StripId strip) => _surface.Latest[strip.Number - 1];

        public Reading? LatestSubsurfaceMoisture(StripId strip) => _subsurface.Latest[strip.Number - 1];

        public void Record(Reading reading)
        {
            var known = reading.Quantity == Quantity.SubsurfaceMoisture ? _subsurface : _surface;
            known.Latest[reading.Strip.Number - 1] = reading;
            known.WaterSinceMm[reading.Strip.Number - 1] = 0;
        }

        /// <summary>
        /// Notes water known to have reached a strip, from the rain gauge while it was open or
        /// from watering the player ordered.
        /// </summary>
        public void AddWater(StripId strip, double mm)
        {
            _surface.WaterSinceMm[strip.Number - 1] += mm;
            _subsurface.WaterSinceMm[strip.Number - 1] += mm;
        }

        public ValueRange? CurrentSurfaceMoisture(StripId strip, GameTime now) => Current(_surface, strip, now);

        public ValueRange? CurrentSubsurfaceMoisture(StripId strip, GameTime now) => Current(_subsurface, strip, now);

        /// <summary>
        /// The latest reading's range widened for its age and the water the strip has taken
        /// since, split evenly either side; null if the strip hasn't been read.
        /// </summary>
        private ValueRange? Current(Known known, StripId strip, GameTime now)
        {
            var reading = known.Latest[strip.Number - 1];
            if (reading == null)
            {
                return null;
            }

            var days = reading.TakenAt.HoursUntil(now) / 24.0;
            var widening = _settings.WidenPerDay * days + _settings.WidenPerMmWater * known.WaterSinceMm[strip.Number - 1];
            var range = reading.Range;
            return new ValueRange(Math.Max(0, range.Low - widening / 2), Math.Max(0, range.High + widening / 2));
        }

        private sealed class Known
        {
            public Known(int stripCount)
            {
                Latest = new Reading?[stripCount];
                WaterSinceMm = new double[stripCount];
            }

            public Reading?[] Latest { get; }
            public double[] WaterSinceMm { get; }
        }
    }
}
