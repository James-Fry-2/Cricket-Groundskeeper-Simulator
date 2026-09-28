using Groundsman.Core.Time;

namespace Groundsman.Core
{
    /// <summary>The last cut you ordered for a strip: known to you, unlike the grass's true height.</summary>
    public sealed class MowRecord
    {
        public MowRecord(double heightMm, GameTime orderedAt)
        {
            HeightMm = heightMm;
            OrderedAt = orderedAt;
        }

        public double HeightMm { get; }
        public GameTime OrderedAt { get; }
    }
}
