using Groundsman.Core.Strips;

namespace Groundsman.Core.Commands
{
    public sealed class WaterStrip : IGameCommand
    {
        public WaterStrip(StripId strip)
        {
            Strip = strip;
        }

        public StripId Strip { get; }
    }
}
