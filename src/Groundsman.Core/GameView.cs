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
        public GameView(GameTime now, DayPace pace, DateTime? nextMatchDay, WeatherObservation? weather, string groundName, IReadOnlyList<StripView> strips, int coversFree)
        {
            Now = now;
            Pace = pace;
            NextMatchDay = nextMatchDay;
            Weather = weather;
            GroundName = groundName;
            Strips = strips;
            CoversFree = coversFree;
        }

        public GameTime Now { get; }
        public DayPace Pace { get; }

        /// <summary>Today if a match is on today, otherwise the next match day, or null if none.</summary>
        public DateTime? NextMatchDay { get; }

        /// <summary>Null until the first hour has run.</summary>
        public WeatherObservation? Weather { get; }

        public string GroundName { get; }
        public IReadOnlyList<StripView> Strips { get; }

        /// <summary>Covers neither on a strip nor ordered onto one this turn.</summary>
        public int CoversFree { get; }
    }
}
