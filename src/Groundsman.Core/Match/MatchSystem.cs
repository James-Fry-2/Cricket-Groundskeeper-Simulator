using System;
using System.Collections.Generic;
using System.Linq;
using Groundsman.Core.Content;
using Groundsman.Core.Pitch;
using Groundsman.Core.Randomness;
using Groundsman.Core.Simulation;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;
using Groundsman.Core.Wear;
using Groundsman.Core.Weather;

namespace Groundsman.Core.Match
{
    /// <summary>
    /// Plays the fixtures hour by hour in their session hours: toss, innings, rain stopping
    /// play, declarations and results. Each hour of play reads the pitch as it stands and wears
    /// it with the bowling side's attack.
    /// </summary>
    internal sealed class MatchSystem : IHourlySystem
    {
        private readonly IReadOnlyList<Fixture> _fixtures;
        private readonly TeamSettings _home;
        private readonly MatchSettings _settings;
        private readonly Square _square;
        private readonly WeatherSystem _weather;
        private readonly PitchModel _pitch;
        private readonly WearModel _wear;
        private readonly MatchModel _model;
        private readonly RandomSource _random;
        private readonly Commentator _commentator;

        public MatchSystem(
            IReadOnlyList<Fixture> fixtures,
            TeamSettings home,
            MatchSettings settings,
            Square square,
            WeatherSystem weather,
            PitchModel pitch,
            WearModel wear,
            RandomSource random,
            Commentator commentator)
        {
            _commentator = commentator;
            _fixtures = fixtures;
            _home = home;
            _settings = settings;
            _square = square;
            _weather = weather;
            _pitch = pitch;
            _wear = wear;
            _random = random;
            _model = new MatchModel(settings, random);
        }

        public TickStep Step => TickStep.Match;

        /// <summary>The match in progress, or the last one played.</summary>
        public MatchState? Latest { get; private set; }

        public Fixture? FixtureOn(DateTime date) => _fixtures.FirstOrDefault(f => f.Start <= date.Date && date.Date <= f.End);

        /// <summary>
        /// Whether a strip must be covered this hour under the playing conditions: the match
        /// strip is covered through a fixture except while dry play is on. Null when the
        /// player's own cover orders apply.
        /// </summary>
        public bool? CoverOverride(StripId strip, GameTime hour)
        {
            var fixture = FixtureOn(hour.Date);
            if (fixture == null || fixture.Strip != strip)
            {
                return null;
            }

            var raining = _weather.LastHour is { } weather && weather.RainMm > 0;
            return !(IsPlayHour(fixture, hour) && !raining);
        }

        public void RunHour(GameTime hour)
        {
            var fixture = FixtureOn(hour.Date);
            if (fixture == null)
            {
                return;
            }
            if (Latest == null || Latest.Fixture != fixture)
            {
                Latest = new MatchState(fixture, _home);
            }

            var match = Latest;
            if (!match.Finished && IsPlayHour(fixture, hour))
            {
                PlayHour(match, hour);
                var session = SessionIndex(fixture.Format, hour);
                if (hour.Hour + 1 == fixture.Format.Sessions[session].End)
                {
                    _commentator.Break(match, hour, fixture.Format.BreakNames[session]);
                }
            }

            var lastEnd = fixture.Format.Sessions[fixture.Format.Sessions.Count - 1].End;
            if (!match.Finished && hour.Date == fixture.End && hour.Hour + 1 >= lastEnd)
            {
                Conclude(match);
            }
        }

        private void PlayHour(MatchState match, GameTime hour)
        {
            var format = match.Fixture.Format;
            var strip = _square.Get(match.Fixture.Strip);

            if (match.Current == null)
            {
                Toss(match, strip);
            }

            var session = SessionIndex(format, hour);

            // In limited-overs cricket each innings has its own session. One that rain kept
            // from finishing in its session closes when the next begins.
            if (format.OversPerInnings != null)
            {
                if (match.Innings.Count - 1 < session && !match.Current!.Closed)
                {
                    CutShort(match);
                    if (match.Finished)
                    {
                        return;
                    }
                }
                if (match.Innings.Count - 1 != session)
                {
                    return;
                }
            }

            var innings = match.Current!;

            var pitch = _pitch.Characterise(strip);
            if (_weather.LastHour!.Value.RainMm > 0)
            {
                match.RestartDelay = true;
                match.Hours.Add(new MatchHour(hour, true, 0, 0, 0, pitch));
                _commentator.Rain(match, hour);
                return;
            }

            var overs = OversPerPlayHour(format) * (match.RestartDelay ? 1 - _settings.RainRestartLoss : 1);
            match.RestartDelay = false;
            if (OversLimit(match) is { } limit)
            {
                overs = Math.Min(overs, limit - innings.Overs);
            }

            var block = _model.PlayBlock(pitch, innings.Batting, innings.Bowling, format, overs, innings.WicketsInHand);
            var runs = block.Runs;
            var wickets = block.Wickets;
            var oversUsed = block.Overs;

            var needed = RunsNeeded(match);
            var chaseWon = false;
            if (needed != null && runs >= needed.Value)
            {
                // The chase finishes as soon as the target is passed.
                oversUsed = runs > 0 ? oversUsed * needed.Value / runs : oversUsed;
                runs = needed.Value;
                wickets = Math.Min(wickets, innings.WicketsInHand - 1);
                chaseWon = true;
            }

            innings.Runs += runs;
            innings.Wickets += wickets;
            innings.Overs += oversUsed;
            _wear.ApplyOvers(strip, oversUsed, innings.Bowling.Attack);
            match.Hours.Add(new MatchHour(hour, false, oversUsed, runs, wickets, pitch));
            _commentator.Play(match, hour, strip, pitch, _pitch.Explain(strip), innings, wickets);

            var day = (hour.Date - match.Fixture.Start).Days + 1;
            var allOut = innings.Wickets >= 10;
            var oversUp = OversLimit(match) is { } maxOvers && innings.Overs >= maxOvers - 1e-9;
            if (!allOut && !oversUp && !chaseWon && ShouldDeclare(match, innings, day))
            {
                innings.Declared = true;
            }

            if (allOut || oversUp || chaseWon || innings.Declared)
            {
                CloseInnings(match);
            }
        }

        /// <summary>
        /// A limited-overs innings rain kept from finishing in its session. Under half its overs
        /// and the match is abandoned; otherwise the chase gets the overs the first innings faced.
        /// </summary>
        private void CutShort(MatchState match)
        {
            var innings = match.Current!;
            innings.CutShort = true;
            var limit = match.Fixture.Format.OversPerInnings!.Value;
            if (innings.Overs < limit / 2.0)
            {
                match.Finished = true;
                match.Result = new ResultView(ResultKind.NoResult, null, "No result");
                return;
            }
            if (match.Innings.Count == 1)
            {
                match.ReducedOvers = innings.Overs;
            }
            CloseInnings(match);
        }

        private static double? OversLimit(MatchState match)
        {
            var limit = match.Fixture.Format.OversPerInnings;
            if (limit == null)
            {
                return null;
            }
            return match.Innings.Count == 2 && match.ReducedOvers is { } reduced ? reduced : limit;
        }

        private void Toss(MatchState match, StripState strip)
        {
            var homeWon = _random.Chance(0.5);
            var bowlFirst = _pitch.Characterise(strip).Seam > _settings.TossBowlFirstSeamAbove;
            var winner = homeWon ? match.Home : match.Away;
            var loser = homeWon ? match.Away : match.Home;
            var battingFirst = bowlFirst ? loser : winner;
            var bowlingFirst = bowlFirst ? winner : loser;
            match.Innings.Add(new InningsState(battingFirst, bowlingFirst));
        }

        private void CloseInnings(MatchState match)
        {
            var closed = match.Current!;
            closed.Closed = true;
            var format = match.Fixture.Format;
            var totalInnings = 2 * format.InningsPerSide;

            if (RunsNeeded(match) != null && match.Innings.Count == totalInnings)
            {
                Finish(match);
                return;
            }

            // An innings victory: after three innings, the side batting twice is still behind.
            if (totalInnings == 4 && match.Innings.Count == 3 && !closed.Declared
                && match.TotalFor(closed.Batting) < match.TotalFor(closed.Bowling))
            {
                Finish(match);
                return;
            }

            if (match.Innings.Count < totalInnings)
            {
                match.Innings.Add(new InningsState(closed.Bowling, closed.Batting));
            }
            else
            {
                Finish(match);
            }
        }

        private void Finish(MatchState match)
        {
            match.Finished = true;
            var a = match.Innings[0].Batting;
            var b = match.Innings[0].Bowling;
            var aRuns = match.TotalFor(a);
            var bRuns = match.TotalFor(b);
            var last = match.Current!;

            if (Math.Round(aRuns) == Math.Round(bRuns) && last.Closed && !last.Declared)
            {
                match.Result = new ResultView(ResultKind.Tie, null, "Match tied");
                return;
            }

            var winner = aRuns > bRuns ? a : b;
            var loser = winner == a ? b : a;
            var margin = Math.Round(Math.Abs(aRuns - bRuns));
            string text;
            if (last.Batting == winner && last.WicketsInHand > 0 && !(match.Innings.Count == 3 && match.Fixture.Format.InningsPerSide == 2))
            {
                text = $"{winner.Name} won by {Plural(last.WicketsInHand, "wicket")}";
            }
            else if (match.Innings.Count == 3 && match.Fixture.Format.InningsPerSide == 2)
            {
                text = $"{winner.Name} won by an innings and {Plural((int)margin, "run")}";
            }
            else
            {
                text = $"{winner.Name} won by {Plural((int)margin, "run")}";
            }
            match.Result = new ResultView(ResultKind.Win, winner.Name, text);
        }

        /// <summary>Time is up: whatever hasn't finished is drawn, or has no result.</summary>
        private void Conclude(MatchState match)
        {
            match.Finished = true;
            var format = match.Fixture.Format;
            if (format.OversPerInnings == null)
            {
                match.Result = new ResultView(ResultKind.Draw, null, "Match drawn");
                return;
            }

            // A chase rain kept from reaching half its overs is abandoned too.
            var chaseOvers = OversLimit(match) ?? 0;
            if (match.Innings.Count < 2 || match.Innings[1].Overs < chaseOvers / 2)
            {
                match.Result = new ResultView(ResultKind.NoResult, null, "No result");
                return;
            }

            match.Current!.Closed = true;
            Finish(match);
        }

        private bool ShouldDeclare(MatchState match, InningsState innings, int day)
        {
            var format = match.Fixture.Format;
            if (format.OversPerInnings != null || match.Innings.Count != 3)
            {
                return false;
            }

            var lead = match.TotalFor(innings.Batting) - match.TotalFor(innings.Bowling);
            return day >= _settings.DeclarationFromDay && lead >= _settings.DeclarationLead;
        }

        /// <summary>Runs the side batting last still needs to win, or null if no side is chasing.</summary>
        private static double? RunsNeeded(MatchState match)
        {
            var format = match.Fixture.Format;
            if (match.Innings.Count != 2 * format.InningsPerSide)
            {
                return null;
            }

            var chasing = match.Current!;
            return match.TotalFor(chasing.Bowling) - match.TotalFor(chasing.Batting) + 1;
        }

        private static bool IsPlayHour(Fixture fixture, GameTime hour) => SessionIndex(fixture.Format, hour) >= 0;

        private static int SessionIndex(FormatSettings format, GameTime hour)
        {
            for (var i = 0; i < format.Sessions.Count; i++)
            {
                if (hour.Hour >= format.Sessions[i].Start && hour.Hour < format.Sessions[i].End)
                {
                    return i;
                }
            }
            return -1;
        }

        private static double OversPerPlayHour(FormatSettings format)
        {
            if (format.OversPerInnings is { } perInnings)
            {
                var first = format.Sessions[0];
                return (double)perInnings / (first.End - first.Start);
            }

            var hours = format.Sessions.Sum(s => s.End - s.Start);
            return (double)format.OversPerDay!.Value / hours;
        }

        private static string Plural(int count, string word) => count == 1 ? $"1 {word}" : $"{count} {word}s";
    }
}
