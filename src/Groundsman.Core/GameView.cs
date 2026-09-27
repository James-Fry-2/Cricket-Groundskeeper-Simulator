using System.Collections.Generic;
using Groundsman.Core.Time;

namespace Groundsman.Core
{
    /// <summary>
    /// Read-only snapshot for front ends. Built from readings only, never from true strip state.
    /// </summary>
    public sealed class GameView
    {
        public GameView(GameTime now, string groundName, IReadOnlyList<StripView> strips)
        {
            Now = now;
            GroundName = groundName;
            Strips = strips;
        }

        public GameTime Now { get; }
        public string GroundName { get; }
        public IReadOnlyList<StripView> Strips { get; }
    }
}
