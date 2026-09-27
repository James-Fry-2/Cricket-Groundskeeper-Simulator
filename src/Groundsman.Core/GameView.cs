using System;
using System.Collections.Generic;
using Groundsman.Core.Forecasting;
using Groundsman.Core.Time;

namespace Groundsman.Core
{
    /// <summary>
    /// Read-only snapshot for front ends. Built from readings only, never from true strip state.
    /// </summary>
    public sealed class GameView
    {
        public GameView(GameTime now, DayPace pace, DateTime? nextMatchDay, Fixture? nextFixture, WeatherObservation? weather, string groundName, IReadOnlyList<StripView> strips, int coversFree, int coversOwned, IReadOnlyList<StaffView> staff, IReadOnlyList<DayForecast> forecast)
        {
            Now = now;
            Pace = pace;
            NextMatchDay = nextMatchDay;
            NextFixture = nextFixture;
            Weather = weather;
            GroundName = groundName;
            Strips = strips;
            CoversFree = coversFree;
            CoversOwned = coversOwned;
            Staff = staff;
            Forecast = forecast;
        }

        public GameTime Now { get; }
        public DayPace Pace { get; }

        /// <summary>Today if a match is on today, otherwise the next match day, or null if none.</summary>
        public DateTime? NextMatchDay { get; }

        /// <summary>The fixture being played now, or the next one; null when none are left.</summary>
        public Fixture? NextFixture { get; }

        /// <summary>Null until the first hour has run.</summary>
        public WeatherObservation? Weather { get; }

        public string GroundName { get; }
        public IReadOnlyList<StripView> Strips { get; }

        /// <summary>Covers neither on a strip nor ordered onto one this turn.</summary>
        public int CoversFree { get; }

        public int CoversOwned { get; }

        /// <summary>Everyone on the ground staff, the player first.</summary>
        public IReadOnlyList<StaffView> Staff { get; }

        /// <summary>This morning's forecast, today first.</summary>
        public IReadOnlyList<DayForecast> Forecast { get; }
    }
}
