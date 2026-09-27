using Groundsman.Core.Staff;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Core.Readings
{
    public sealed class Reading
    {
        public Reading(StripId strip, Quantity quantity, ValueRange range, GameTime takenAt, ReadingSource source, StaffId takenBy, string? word = null)
        {
            Word = word;
            TakenBy = takenBy;
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
        public StaffId TakenBy { get; }

        /// <summary>For a feel reading, the word it gave; the range is that word's band.</summary>
        public string? Word { get; }
    }
}
