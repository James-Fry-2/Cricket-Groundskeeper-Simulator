using Groundsman.Core.Readings;
using Groundsman.Core.Strips;

namespace Groundsman.Core
{
    public sealed class StripView
    {
        public StripView(StripId id, Reading? surfaceMoisture)
        {
            Id = id;
            SurfaceMoisture = surfaceMoisture;
        }

        public StripId Id { get; }

        /// <summary>The latest reading, or null if the strip hasn't been read.</summary>
        public Reading? SurfaceMoisture { get; }
    }
}
