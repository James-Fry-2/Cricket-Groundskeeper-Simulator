using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Groundsman.Core.Content;
using Groundsman.Core.Pitch;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Core.Match
{
    /// <summary>
    /// Turns what the pitch is doing into commentary, each event tied to its biggest cause so
    /// a verdict never feels random. Events are raised once a match, when a characteristic
    /// crosses its threshold; nothing here draws random numbers.
    /// </summary>
    internal sealed class Commentator
    {
        private readonly CommentarySettings _settings;
        private readonly IReadOnlyList<string> _ends;

        public Commentator(CommentarySettings settings, IReadOnlyList<string> ends)
        {
            _settings = settings;
            _ends = ends;
        }

        public void Rain(MatchState match, GameTime hour)
        {
            if (match.LastRainHour != hour.AddHours(-1))
            {
                match.Commentary.Add(new CommentaryLine(hour, "rain", null, _settings.Rain));
            }
            match.LastRainHour = hour;
        }

        public void Break(MatchState match, GameTime hour, string breakName)
        {
            var played = match.Innings.Where(i => i.Overs > 0 || i.Closed).ToList();
            var score = string.Join("; ", played.Skip(Math.Max(0, played.Count - 2)).Select(Score));
            if (score.Length == 0)
            {
                return;
            }

            // The break starts when the session's last hour of play ends.
            var breakHour = hour.AddHours(1);
            match.Commentary.Add(new CommentaryLine(breakHour, null, null, Fill(_settings.Session, match, breakHour, null, null, breakName, score, 0, null)));
        }

        public void Play(MatchState match, GameTime hour, StripState strip, PitchCharacteristics pitch, PitchDrivers drivers, InningsState innings, int wickets)
        {
            var unevenCauses = new[]
            {
                ("structureDamage", drivers.StructureDamage),
                ("loose", drivers.Loose),
                ("cracks", drivers.Cracks),
                ("footholes", drivers.Footholes),
                ("surfaceWear", drivers.SurfaceWear),
            };
            var seamCauses = new[] { ("grass", drivers.GrassSeam), ("damp", drivers.DampSeam) };
            var spinCauses = new[] { ("dry", drivers.DrySpin), ("rough", drivers.RoughSpin), ("cracks", drivers.CrackSpin), ("surfaceWear", drivers.WearSpin) };

            // Wickets fall to whoever is bowling, so seam and spin causes count by their share of the attack.
            var attack = innings.Bowling.Attack;
            var collapseCauses = unevenCauses
                .Concat(seamCauses.Select(c => (c.Item1, c.Item2 * attack.Seam)))
                .Concat(spinCauses.Select(c => (c.Item1, c.Item2 * attack.Spin)))
                .ToArray();
            var deadCauses = new[] { ("loose", drivers.Loose * 2), ("longGrass", drivers.Cushion), ("wet", drivers.Wetness * 0.5) };

            Raise(match, hour, strip, innings, "seam", pitch.Seam >= Threshold("seam"), seamCauses, wickets);
            Raise(match, hour, strip, innings, "dead", pitch.Carry <= Threshold("dead"), deadCauses, wickets);
            Raise(match, hour, strip, innings, "danger", pitch.Consistency <= Threshold("danger"), unevenCauses, wickets);
            Raise(match, hour, strip, innings, "uneven", pitch.Consistency <= Threshold("uneven"), unevenCauses, wickets);
            Raise(match, hour, strip, innings, "keepingLow", pitch.Bounce <= Threshold("keepingLow") && pitch.Consistency <= Threshold("uneven") + 2, unevenCauses, wickets);
            Raise(match, hour, strip, innings, "turn", pitch.Spin >= Threshold("turn"), spinCauses, wickets);
            Raise(match, hour, strip, innings, "dust", strip.SurfaceWear >= Threshold("dust"), new[] { ("surfaceWear", drivers.SurfaceWear), ("dry", drivers.DrySpin * 0.5) }, wickets);
            Raise(match, hour, strip, innings, "cracks", strip.Cracks >= Threshold("cracks"), new[] { ("cracks", drivers.Cracks), ("structureDamage", drivers.StructureDamage) }, wickets);
            Raise(match, hour, strip, innings, "footholes", strip.Footholes >= Threshold("footholes"), new[] { ("footholes", 1.0) }, wickets);
            Raise(match, hour, strip, innings, "collapse", wickets >= Threshold("collapse"), collapseCauses, wickets);
            Raise(match, hour, strip, innings, "true",
                pitch.Consistency >= Threshold("true") && pitch.Carry >= 6 && pitch.Seam < 3 && pitch.Spin < 3,
                new[] { ("hard", 1.0) }, wickets);
        }

        private double Threshold(string id) => _settings.Event(id).Threshold;

        private void Raise(MatchState match, GameTime hour, StripState strip, InningsState innings, string id, bool happened, (string Id, double Weight)[] causes, int wickets)
        {
            if (!happened || match.Fired.Contains(id))
            {
                return;
            }

            var cause = causes.OrderByDescending(c => c.Weight).First().Id;
            match.Fired.Add(id);
            var causeText = Fill(_settings.Cause(cause), match, hour, strip, innings, null, null, wickets, null);
            match.Commentary.Add(new CommentaryLine(hour, id, cause, Fill(_settings.Event(id).Text, match, hour, strip, innings, null, null, wickets, causeText)));
        }

        private string Fill(string template, MatchState match, GameTime hour, StripState? strip, InningsState? innings, string? breakName, string? score, int wickets, string? cause)
        {
            var text = template
                .Replace("{batting}", innings?.Batting.Name ?? "")
                .Replace("{bowling}", innings?.Bowling.Name ?? "")
                .Replace("{end}", _ends[hour.Hour % 2])
                .Replace("{wickets}", wickets.ToString(CultureInfo.InvariantCulture))
                .Replace("{grassMm}", strip == null ? "" : Math.Round(strip.GrassHeightMm).ToString(CultureInfo.InvariantCulture))
                .Replace("{break}", breakName ?? "")
                .Replace("{score}", score ?? (match.Current == null ? "" : Score(match.Current)))
                .Replace("{cause}", cause ?? "");
            return text;
        }

        private static string Score(InningsState innings)
        {
            var runs = (int)Math.Round(innings.Runs);
            var score = innings.Wickets == 10 ? $"{runs} all out" : $"{runs}-{innings.Wickets}{(innings.Declared ? "d" : "")}";
            return $"{innings.Batting.Name} {score}";
        }
    }
}
