using Groundsman.Core.Time;

namespace Groundsman.Core
{
    /// <summary>
    /// Read-only snapshot for front ends. Built from readings only, never from true strip state.
    /// </summary>
    public sealed class GameView
    {
        public GameView(GameTime now)
        {
            Now = now;
        }

        public GameTime Now { get; }
    }
}
