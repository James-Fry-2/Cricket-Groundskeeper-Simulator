using Groundsman.Core.Readings;
using Groundsman.Core.Staff;
using Groundsman.Core.Strips;

namespace Groundsman.Core.Commands
{
    public sealed class TakeReading : IGameCommand
    {
        /// <param name="by">Who does the job; the player if not given.</param>
        public TakeReading(StripId strip, StaffId? by = null, ReadingSource tool = ReadingSource.MoistureProbe)
        {
            Strip = strip;
            By = by;
            Tool = tool;
        }

        public StripId Strip { get; }
        public StaffId? By { get; }
        public ReadingSource Tool { get; }
    }
}
