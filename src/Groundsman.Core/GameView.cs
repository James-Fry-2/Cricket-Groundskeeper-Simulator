using System;
using System.Collections.Generic;
using Groundsman.Core.Time;

namespace Groundsman.Core
{
    /// <summary>
    /// Read-only snapshot for front ends. Built from readings only, never from true strip state.
    /// </summary>
    public sealed class GameView
    {
        public GameView(GameTime now, DayPace pace, DateTime? nextMatchDay, string groundName, IReadOnlyList<StripView> strips)
        {
            Now = now;
            Pace = pace;
            NextMatchDay = nextMatchDay;
            GroundName = groundName;
            Strips = strips;
        }

        public GameTime Now { get; }
        public DayPace Pace { get; }

        /// <summary>Today if a match is on today, otherwise the next match day, or null if none.</summary>
        public DateTime? NextMatchDay { get; }

        public string GroundName { get; }
        public IReadOnlyList<StripView> Strips { get; }
    }
}
