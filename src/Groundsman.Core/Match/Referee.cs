using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Groundsman.Core.Content;
using Groundsman.Core.Pitch;

namespace Groundsman.Core.Match
{
    /// <summary>
    /// Rates a finished match's pitch on the ICC's scale. Bounce consistency and danger weigh
    /// most, then carry, then the balance between bat and ball, with the failure modes from the
    /// research: too much for the bowlers (a four-day match over in two days) and lifeless (a
    /// high-scoring draw with nothing for the bowlers). Reasons carry named causes.
    /// </summary>
    internal sealed class Referee
    {
        private readonly RatingSettings _settings;
        private readonly CommentarySettings _commentary;

        public Referee(RatingSettings settings, CommentarySettings commentary)
        {
            _settings = settings;
            _commentary = commentary;
        }

        /// <summary>Null when no dry play was possible, so there's nothing to rate.</summary>
        public RatingView? Rate(MatchState match)
        {
            var played = match.Hours.Where(h => !h.Rained).ToList();
            if (played.Count == 0)
            {
                return null;
            }

            var ratedOn = match.Fixture.End;
            var worst = played.OrderBy(h => h.Pitch.Consistency).First();
            var averageConsistency = played.Average(h => h.Pitch.Consistency);
            var averageCarry = played.Average(h => h.Pitch.Carry);

            if (match.Abandoned)
            {
                return Unfit(ratedOn, Reason("abandoned", worst.Pitch.Consistency, UnevenCause(match, worst)));
            }
            if (worst.Pitch.Consistency < _settings.UnfitBelow)
            {
                return Unfit(ratedOn, Reason("dangerous", worst.Pitch.Consistency, UnevenCause(match, worst)));
            }

            var faults = new List<(string Id, string Text)>();
            if (averageConsistency < _settings.UnsatisfactoryConsistencyBelow)
            {
                faults.Add(Reason("uneven", averageConsistency, UnevenCause(match, worst)));
            }
            if (averageCarry < _settings.UnsatisfactoryCarryBelow)
            {
                var slowest = played.OrderBy(h => h.Pitch.Carry).First();
                faults.Add(Reason("dead", averageCarry, DeadCause(match, slowest)));
            }

            var format = match.Fixture.Format;
            var runs = match.Innings.Sum(i => i.Runs);
            var wickets = match.Innings.Sum(i => i.Wickets);
            var runsPerWicket = wickets == 0 ? double.PositiveInfinity : runs / wickets;
            if (format.OversPerDay is { } perDay)
            {
                var share = match.Innings.Sum(i => i.Overs) / (perDay * format.Days);
                if (match.Result?.Kind == ResultKind.Win && share < _settings.BowlersOversShare && runsPerWicket < _settings.BowlersRunsPerWicket)
                {
                    faults.Add(Reason("bowlers", share * 100, null, "0"));
                }
                var movement = played.Max(h => Math.Max(h.Pitch.Seam, h.Pitch.Spin));
                if (match.Result?.Kind == ResultKind.Draw && runsPerWicket > _settings.LifelessRunsPerWicket && movement < _settings.LifelessMovementBelow)
                {
                    faults.Add(Reason("lifeless", 0, null));
                }
            }
            else if (format.OversPerInnings is { } perInnings)
            {
                var par = format.RunsPerOver * perInnings * _settings.LimitedParShare;
                if (match.Innings.Count == 2 && match.Innings.All(i => i.Runs < par))
                {
                    faults.Add(Reason("limitedLow", 0, null));
                }
            }

            if (faults.Count > 0)
            {
                return Rating(PitchGrade.Unsatisfactory, _settings.UnsatisfactoryDemerits, faults, ratedOn);
            }

            var praise = new List<(string Id, string Text)>
            {
                Reason("consistent", averageConsistency, null),
                Reason("carry", averageCarry, null),
            };
            var lively = format.OversPerDay == null || HasMovement(match, played);
            praise.Add(Reason(lively ? "movement" : "flat", 0, null));

            var veryGood = averageConsistency >= _settings.VeryGoodConsistencyAtLeast
                && averageCarry >= _settings.VeryGoodCarryAtLeast
                && lively;
            return Rating(veryGood ? PitchGrade.VeryGood : PitchGrade.Satisfactory, 0, praise, ratedOn);
        }

        private RatingView Unfit(DateTime ratedOn, (string Id, string Text) reason) =>
            Rating(PitchGrade.Unfit, _settings.UnfitDemerits, new[] { reason }, ratedOn);

        private static RatingView Rating(PitchGrade grade, int demerits, IReadOnlyList<(string Id, string Text)> reasons, DateTime ratedOn) =>
            new RatingView(grade, demerits, reasons.Select(r => r.Text).ToArray(), reasons.Select(r => r.Id).ToArray(), ratedOn);

        // A multi-day pitch should offer seam early or turn later, as the research's ideal Test pitch does.
        private bool HasMovement(MatchState match, List<MatchHour> played)
        {
            var firstDay = match.Fixture.Start;
            return played.Any(h =>
                (h.Hour.Date == firstDay && h.Pitch.Seam >= _settings.VeryGoodMovementAtLeast)
                || (h.Hour.Date > firstDay && h.Pitch.Spin >= _settings.VeryGoodMovementAtLeast));
        }

        private string UnevenCause(MatchState match, MatchHour worst) =>
            CommentaryCause(match, "danger", "uneven", "keepingLow")
            ?? CauseText(Biggest(
                ("structureDamage", worst.Drivers.StructureDamage),
                ("loose", worst.Drivers.Loose),
                ("cracks", worst.Drivers.Cracks),
                ("footholes", worst.Drivers.Footholes),
                ("surfaceWear", worst.Drivers.SurfaceWear),
                ("lastingWear", worst.Drivers.LastingWear),
                ("thinEnds", worst.Drivers.ThinEnds)), worst);

        private string DeadCause(MatchState match, MatchHour slowest) =>
            CommentaryCause(match, "dead")
            ?? CauseText(Biggest(("loose", slowest.Drivers.Loose * 2), ("longGrass", slowest.Drivers.Cushion), ("wet", slowest.Drivers.Wetness * 0.5)), slowest);

        private static string? CommentaryCause(MatchState match, params string[] eventIds) =>
            match.Commentary.FirstOrDefault(c => c.EventId != null && eventIds.Contains(c.EventId) && c.CauseText != null)?.CauseText;

        private static string Biggest(params (string Id, double Weight)[] causes) => causes.OrderByDescending(c => c.Weight).First().Id;

        private string CauseText(string id, MatchHour hour) =>
            _commentary.Cause(id).Replace("{grassMm}", Math.Round(hour.GrassMm).ToString(CultureInfo.InvariantCulture));

        private (string Id, string Text) Reason(string id, double value, string? cause, string format = "0.#") =>
            (id, _settings.Reason(id)
                .Replace("{value}", value.ToString(format, CultureInfo.InvariantCulture))
                .Replace("{cause}", cause ?? ""));
    }
}
