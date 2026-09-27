using Groundsman.Core.Staff;
using Groundsman.Core.Strips;

namespace Groundsman.Core.Commands
{
    public sealed class WaterStrip : IGameCommand
    {
        /// <param name="by">Who does the job; the player if not given.</param>
        public WaterStrip(StripId strip, StaffId? by = null)
        {
            Strip = strip;
            By = by;
        }

        public StripId Strip { get; }
        public StaffId? By { get; }
    }
}
