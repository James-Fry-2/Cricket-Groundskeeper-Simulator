using Groundsman.Core.Strips;

namespace Groundsman.Core.Commands
{
    public sealed class UncoverStrip : IGameCommand
    {
        public UncoverStrip(StripId strip)
        {
            Strip = strip;
        }

        public StripId Strip { get; }
    }
}
