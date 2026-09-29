using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Groundsman.Core.Content
{
    public sealed class CommentaryEvent
    {
        public CommentaryEvent(double threshold, string text)
        {
            Threshold = threshold;
            Text = text;
        }

        public double Threshold { get; }
        public string Text { get; }
    }

    /// <summary>
    /// Commentary wording, so tone can be rewritten without code changes. Every event has a
    /// template and a threshold; every cause has the words that explain it.
    /// </summary>
    public sealed class CommentarySettings
    {
        public static readonly IReadOnlyList<string> EventIds = new[]
        {
            "seam", "dead", "uneven", "keepingLow", "turn", "dust", "cracks", "footholes", "danger", "collapse", "true",
        };

        public static readonly IReadOnlyList<string> CauseIds = new[]
        {
            "grass", "damp", "wet", "loose", "longGrass", "structureDamage", "cracks", "footholes", "surfaceWear", "dry", "rough", "hard",
        };

        private static readonly HashSet<string> Placeholders = new HashSet<string>
        {
            "batting", "bowling", "end", "score", "break", "cause", "wickets", "grassMm",
        };

        private static readonly Regex Placeholder = new Regex(@"\{(\w+)\}");

        private readonly Dictionary<string, CommentaryEvent> _events;
        private readonly Dictionary<string, string> _causes;

        public CommentarySettings(IDictionary<string, CommentaryEvent> events, IDictionary<string, string> causes, string rain, string session)
        {
            foreach (var id in EventIds)
            {
                if (!events.TryGetValue(id, out var ev))
                {
                    throw new ContentException($"commentary.events.{id} is missing.");
                }
                CheckTemplate($"events.{id}.text", ev.Text);
            }
            foreach (var id in events.Keys)
            {
                if (!((IList<string>)EventIds).Contains(id))
                {
                    throw new ContentException($"commentary.events.{id} isn't an event the game raises.");
                }
            }
            foreach (var id in CauseIds)
            {
                if (!causes.TryGetValue(id, out var text))
                {
                    throw new ContentException($"commentary.causes.{id} is missing.");
                }
                CheckTemplate($"causes.{id}", text);
            }
            CheckTemplate("rain", rain);
            CheckTemplate("session", session);

            _events = new Dictionary<string, CommentaryEvent>(events);
            _causes = new Dictionary<string, string>(causes);
            Rain = rain;
            Session = session;
        }

        /// <summary>Said once when rain first stops play in a spell.</summary>
        public string Rain { get; }

        /// <summary>Said at each break, with the score.</summary>
        public string Session { get; }

        public CommentaryEvent Event(string id) => _events[id];

        public string Cause(string id) => _causes[id];

        private static void CheckTemplate(string field, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ContentException($"commentary.{field} can't be blank.");
            }
            foreach (System.Text.RegularExpressions.Match match in Placeholder.Matches(text))
            {
                if (!Placeholders.Contains(match.Groups[1].Value))
                {
                    throw new ContentException($"commentary.{field} uses {{{match.Groups[1].Value}}}, which isn't a placeholder the game fills.");
                }
            }
        }
    }
}
