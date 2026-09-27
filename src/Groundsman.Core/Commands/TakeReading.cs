using Groundsman.Core.Strips;

namespace Groundsman.Core.Commands
{
    public sealed class TakeReading : IGameCommand
    {
        public TakeReading(StripId strip)
        {
            Strip = strip;
        }

        public StripId Strip { get; }
    }
}
