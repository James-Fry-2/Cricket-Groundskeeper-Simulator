using Groundsman.Core.Staff;
using Groundsman.Core.Strips;

namespace Groundsman.Core.Commands
{
    public sealed class MowStrip : IGameCommand
    {
        /// <param name="by">Who does the job; the player if not given.</param>
        public MowStrip(StripId strip, double heightMm, StaffId? by = null)
        {
            Strip = strip;
            HeightMm = heightMm;
            By = by;
        }

        public StripId Strip { get; }
        public double HeightMm { get; }
        public StaffId? By { get; }
    }
}
