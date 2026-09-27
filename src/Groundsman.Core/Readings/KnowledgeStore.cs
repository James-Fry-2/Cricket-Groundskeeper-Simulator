using Groundsman.Core.Strips;

namespace Groundsman.Core.Readings
{
    /// <summary>
    /// Everything the player knows about the strips. The view is built from here, never from
    /// true state.
    /// </summary>
    internal sealed class KnowledgeStore
    {
        private readonly Reading?[] _latestSurfaceMoisture;

        public KnowledgeStore(int stripCount)
        {
            _latestSurfaceMoisture = new Reading?[stripCount];
        }

        public Reading? LatestSurfaceMoisture(StripId strip) => _latestSurfaceMoisture[strip.Number - 1];

        public void Record(Reading reading) => _latestSurfaceMoisture[reading.Strip.Number - 1] = reading;
    }
}
