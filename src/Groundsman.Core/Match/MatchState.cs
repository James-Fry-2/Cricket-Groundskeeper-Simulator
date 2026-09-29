using System;
using System.Collections.Generic;
using System.Linq;
using Groundsman.Core.Content;
using Groundsman.Core.Pitch;
using Groundsman.Core.Time;

namespace Groundsman.Core.Match
{
    internal sealed class InningsState
    {
        public InningsState(TeamSettings batting, TeamSettings bowling)
        {
            Batting = batting;
            Bowling = bowling;
        }

        public TeamSettings Batting { get; }
        public TeamSettings Bowling { get; }
        public double Runs { get; set; }
        public int Wickets { get; set; }
        public double Overs { get; set; }
        public bool Declared { get; set; }
        public bool Closed { get; set; }

        /// <summary>Closed by rain running out its session before the overs or wickets did.</summary>
        public bool CutShort { get; set; }

        public int WicketsInHand => 10 - Wickets;
    }

    /// <summary>One hour of a match, kept for the referee's rating: truth, not for the player.</summary>
    internal sealed class MatchHour
    {
        public MatchHour(GameTime hour, bool rained, double overs, double runs, int wickets, PitchCharacteristics pitch, PitchDrivers drivers, double grassMm)
        {
            Drivers = drivers;
            GrassMm = grassMm;
            Hour = hour;
            Rained = rained;
            Overs = overs;
            Runs = runs;
            Wickets = wickets;
            Pitch = pitch;
        }

        public GameTime Hour { get; }
        public bool Rained { get; }
        public double Overs { get; }
        public double Runs { get; }
        public int Wickets { get; }
        public PitchCharacteristics Pitch { get; }
        public PitchDrivers Drivers { get; }
        public double GrassMm { get; }
    }

    internal sealed class MatchState
    {
        public MatchState(Fixture fixture, TeamSettings home)
        {
            Fixture = fixture;
            Home = home;
        }

        public Fixture Fixture { get; }
        public TeamSettings Home { get; }
        public TeamSettings Away => Fixture.Opponent;
        public List<InningsState> Innings { get; } = new List<InningsState>();
        public List<MatchHour> Hours { get; } = new List<MatchHour>();
        public List<CommentaryLine> Commentary { get; } = new List<CommentaryLine>();
        public HashSet<string> Fired { get; } = new HashSet<string>();
        public GameTime? LastRainHour { get; set; }
        public bool RestartDelay { get; set; }

        /// <summary>A limited-overs chase's overs, cut to what the first innings faced if rain shortened it.</summary>
        public double? ReducedOvers { get; set; }
        public bool Finished { get; set; }
        public ResultView? Result { get; set; }

        /// <summary>The referee stopped play because the pitch was unsafe.</summary>
        public bool Abandoned { get; set; }

        public RatingView? Rating { get; set; }

        public InningsState? Current => Innings.Count == 0 ? null : Innings[Innings.Count - 1];

        public double TotalFor(TeamSettings team) => Innings.Where(i => i.Batting == team).Sum(i => i.Runs);

        public MatchView ToView() => new MatchView(
            Fixture,
            Innings.Select(i => new InningsView(i.Batting.Name, (int)Math.Round(i.Runs), i.Wickets, Math.Round(i.Overs, 1), i.Declared)).ToArray(),
            Finished,
            Result,
            Commentary.ToArray(),
            Rating);
    }
}
