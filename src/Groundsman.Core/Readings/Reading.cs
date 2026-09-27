using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Core.Readings
{
    public sealed class Reading
    {
        public Reading(StripId strip, Quantity quantity, ValueRange range, GameTime takenAt, ReadingSource source)
        {
            Strip = strip;
            Quantity = quantity;
            Range = range;
            TakenAt = takenAt;
            Source = source;
        }

        public StripId Strip { get; }
        public Quantity Quantity { get; }
        public ValueRange Range { get; }
        public GameTime TakenAt { get; }
        public ReadingSource Source { get; }
    }
}
