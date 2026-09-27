using Groundsman.Core.Time;

namespace Groundsman.Core
{
    public sealed class AdvanceResult
    {
        public AdvanceResult(GameTime from, GameTime to)
        {
            From = from;
            To = to;
        }

        public GameTime From { get; }
        public GameTime To { get; }
        public int HoursRun => From.HoursUntil(To);
    }
}
