using Groundsman.Core.Strips;

namespace Groundsman.Core.Commands
{
    public sealed class CoverStrip : IGameCommand
    {
        public CoverStrip(StripId strip)
        {
            Strip = strip;
        }

        public StripId Strip { get; }
    }
}
