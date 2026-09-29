using Groundsman.Core.Staff;
using Groundsman.Core.Strips;

namespace Groundsman.Core.Commands
{
    /// <summary>Fill a match strip's footholes at close of play, as Law 9 allows in matches over one day.</summary>
    public sealed class FillFootholes : IGameCommand
    {
        /// <param name="by">Who does the job; the player if not given.</param>
        public FillFootholes(StripId strip, StaffId? by = null)
        {
            Strip = strip;
            By = by;
        }

        public StripId Strip { get; }
        public StaffId? By { get; }
    }
}
