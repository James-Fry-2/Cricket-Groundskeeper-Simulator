using Groundsman.Core.Staff;
using Groundsman.Core.Strips;

namespace Groundsman.Core.Commands
{
    public sealed class RollStrip : IGameCommand
    {
        /// <param name="by">Who does the job; the player if not given.</param>
        public RollStrip(StripId strip, string rollerId, double minutes, StaffId? by = null)
        {
            Strip = strip;
            RollerId = rollerId;
            Minutes = minutes;
            By = by;
        }

        public StripId Strip { get; }
        public string RollerId { get; }
        public double Minutes { get; }
        public StaffId? By { get; }
    }
}
