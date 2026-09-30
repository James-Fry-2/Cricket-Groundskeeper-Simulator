using Groundsman.Core.Strips;

namespace Groundsman.Core.Commands
{
    /// <summary>Choose the strip a fixture is played on. Office work: it costs no hours.</summary>
    public sealed class AssignStrip : IGameCommand
    {
        public AssignStrip(string fixtureId, StripId strip)
        {
            FixtureId = fixtureId;
            Strip = strip;
        }

        public string FixtureId { get; }
        public StripId Strip { get; }
    }
}
