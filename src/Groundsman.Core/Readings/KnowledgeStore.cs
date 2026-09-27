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
        private readonly Reading?[] _latestSurfaceMoisture;
        private readonly double[] _waterSinceReadingMm;

        public KnowledgeStore(ReadingSettings settings, int stripCount)
        {
            _settings = settings;
            _latestSurfaceMoisture = new Reading?[stripCount];
            _waterSinceReadingMm = new double[stripCount];
        }

        public Reading? LatestSurfaceMoisture(StripId strip) => _latestSurfaceMoisture[strip.Number - 1];

        public void Record(Reading reading)
        {
            _latestSurfaceMoisture[reading.Strip.Number - 1] = reading;
            _waterSinceReadingMm[reading.Strip.Number - 1] = 0;
        }

        /// <summary>
        /// Notes water known to have reached a strip, from the rain gauge while it was open or
        /// from watering the player ordered.
        /// </summary>
        public void AddWater(StripId strip, double mm) => _waterSinceReadingMm[strip.Number - 1] += mm;

        /// <summary>
        /// The latest reading's range widened for its age and the water the strip has taken
        /// since, split evenly either side; null if the strip hasn't been read.
        /// </summary>
        public ValueRange? CurrentSurfaceMoisture(StripId strip, GameTime now)
        {
            var reading = LatestSurfaceMoisture(strip);
            if (reading == null)
            {
                return null;
            }

            var days = reading.TakenAt.HoursUntil(now) / 24.0;
            var widening = _settings.WidenPerDay * days + _settings.WidenPerMmWater * _waterSinceReadingMm[strip.Number - 1];
            var range = reading.Range;
            return new ValueRange(Math.Max(0, range.Low - widening / 2), Math.Max(0, range.High + widening / 2));
        }
    }
}
