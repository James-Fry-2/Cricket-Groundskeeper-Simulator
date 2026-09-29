using Groundsman.Core.Staff;
using Groundsman.Core.Strips;

namespace Groundsman.Core.Commands
{
    /// <summary>Fill and seed a strip's footholes and rough after use.</summary>
    public sealed class RepairEnds : IGameCommand
    {
        /// <param name="by">Who does the job; the player if not given.</param>
        public RepairEnds(StripId strip, StaffId? by = null)
        {
            Strip = strip;
            By = by;
        }

        public StripId Strip { get; }
        public StaffId? By { get; }
    }
}
