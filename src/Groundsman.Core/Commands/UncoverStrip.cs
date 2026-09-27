using Groundsman.Core.Staff;
using Groundsman.Core.Strips;

namespace Groundsman.Core.Commands
{
    public sealed class UncoverStrip : IGameCommand
    {
        /// <param name="by">Who does the job; the player if not given.</param>
        public UncoverStrip(StripId strip, StaffId? by = null)
        {
            Strip = strip;
            By = by;
        }

        public StripId Strip { get; }
        public StaffId? By { get; }
    }
}
