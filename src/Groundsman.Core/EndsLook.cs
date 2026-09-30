using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Core
{
    /// <summary>What a look at a strip's ends showed, taken with a feel reading.</summary>
    public sealed class EndsLook
    {
        public EndsLook(EndsState state, GameTime takenAt)
        {
            State = state;
            TakenAt = takenAt;
        }

        public EndsState State { get; }
        public GameTime TakenAt { get; }
    }
}
