using Groundsman.Core.Readings;
using Groundsman.Core.Strips;

namespace Groundsman.Core
{
    public sealed class StripView
    {
        public StripView(StripId id, Reading? surfaceMoisture, bool wateringQueued, bool covered, CoverOrder coverOrder)
        {
            Id = id;
            SurfaceMoisture = surfaceMoisture;
            WateringQueued = wateringQueued;
            Covered = covered;
            CoverOrder = coverOrder;
        }

        public StripId Id { get; }

        /// <summary>The latest reading, or null if the strip hasn't been read.</summary>
        public Reading? SurfaceMoisture { get; }

        /// <summary>Watering ordered this turn, carried out when time next advances.</summary>
        public bool WateringQueued { get; }

        /// <summary>Under a cover now.</summary>
        public bool Covered { get; }

        /// <summary>A cover going on or coming off when time next advances.</summary>
        public CoverOrder CoverOrder { get; }
    }
}
