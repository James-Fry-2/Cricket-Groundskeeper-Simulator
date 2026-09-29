using Groundsman.Core.Staff;
using Groundsman.Core.Strips;

namespace Groundsman.Core.Commands
{
    /// <summary>Clean and dry a match strip's footholes at a break, as Law 9 allows in every format.</summary>
    public sealed class CleanFootholes : IGameCommand
    {
        /// <param name="by">Who does the job; the player if not given.</param>
        public CleanFootholes(StripId strip, StaffId? by = null)
        {
            Strip = strip;
            By = by;
        }

        public StripId Strip { get; }
        public StaffId? By { get; }
    }
}
