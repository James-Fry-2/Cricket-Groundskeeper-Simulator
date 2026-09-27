using System.Collections.Generic;
using Groundsman.Core.Time;
using Groundsman.Core.Weather;

namespace Groundsman.Core.Inspection
{
    /// <summary>
    /// True state for the harness, replay tests and the debug view. It must never feed into
    /// <see cref="GameView"/> or anything a player sees in normal play.
    /// </summary>
    public sealed class TruthSnapshot
    {
        public TruthSnapshot(GameTime now, HourWeather? lastHourWeather, IReadOnlyList<StripTruth> strips)
        {
            Now = now;
            LastHourWeather = lastHourWeather;
            Strips = strips;
        }

        public GameTime Now { get; }

        /// <summary>Null before the first hour has run.</summary>
        public HourWeather? LastHourWeather { get; }

        public IReadOnlyList<StripTruth> Strips { get; }
    }
}
