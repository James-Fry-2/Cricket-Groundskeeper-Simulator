using Groundsman.Core.Time;

namespace Groundsman.Core
{
    /// <summary>The last rolling you ordered for a strip.</summary>
    public sealed class RollRecord
    {
        public RollRecord(string rollerId, double minutes, GameTime orderedAt)
        {
            RollerId = rollerId;
            Minutes = minutes;
            OrderedAt = orderedAt;
        }

        public string RollerId { get; }
        public double Minutes { get; }
        public GameTime OrderedAt { get; }
    }
}
