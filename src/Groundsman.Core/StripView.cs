using Groundsman.Core.Readings;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Core
{
    public sealed class StripView
    {
        public StripView(StripId id, Reading? surfaceMoisture, ValueRange? surfaceMoistureNow, Reading? subsurfaceMoisture, ValueRange? subsurfaceMoistureNow, bool wateringQueued, bool covered, CoverOrder coverOrder, MowRecord? lastMown, bool mowingQueued, RollRecord? lastRolled, bool rollingQueued, GameTime? lastRepaired, bool repairQueued)
        {
            LastRepaired = lastRepaired;
            RepairQueued = repairQueued;
            LastRolled = lastRolled;
            RollingQueued = rollingQueued;
            LastMown = lastMown;
            MowingQueued = mowingQueued;
            SubsurfaceMoisture = subsurfaceMoisture;
            SubsurfaceMoistureNow = subsurfaceMoistureNow;
            SurfaceMoistureNow = surfaceMoistureNow;
            Id = id;
            SurfaceMoisture = surfaceMoisture;
            WateringQueued = wateringQueued;
            Covered = covered;
            CoverOrder = coverOrder;
        }

        public StripId Id { get; }

        /// <summary>The latest reading, or null if the strip hasn't been read.</summary>
        public Reading? SurfaceMoisture { get; }

        /// <summary>
        /// The latest reading's range widened for how long ago it was taken and the water the
        /// strip has had since: what the reading can still tell you now.
        /// </summary>
        public ValueRange? SurfaceMoistureNow { get; }

        /// <summary>The latest soil core's reading of moisture at depth, or null if none.</summary>
        public Reading? SubsurfaceMoisture { get; }

        /// <summary>The latest core's range widened as for surface readings.</summary>
        public ValueRange? SubsurfaceMoistureNow { get; }

        /// <summary>Watering ordered this turn, carried out when time next advances.</summary>
        public bool WateringQueued { get; }

        /// <summary>Under a cover now.</summary>
        public bool Covered { get; }

        /// <summary>A cover going on or coming off when time next advances.</summary>
        public CoverOrder CoverOrder { get; }

        /// <summary>The last cut ordered, or null if the strip hasn't been mown this game.</summary>
        public MowRecord? LastMown { get; }

        public bool MowingQueued { get; }

        /// <summary>The last rolling ordered, or null if none this game.</summary>
        public RollRecord? LastRolled { get; }

        public bool RollingQueued { get; }

        /// <summary>When you last ordered the ends repaired, or null if never this game.</summary>
        public GameTime? LastRepaired { get; }

        public bool RepairQueued { get; }
    }
}
