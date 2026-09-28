using System.Collections.Generic;

namespace Groundsman.Core.Content
{
    public sealed class FormatSettings
    {
        public FormatSettings(
            string id,
            string name,
            int days,
            int inningsPerSide,
            int? oversPerInnings,
            int? oversPerDay,
            IReadOnlyList<PlaySession> sessions,
            IReadOnlyList<int> decisionHours)
        {
            var path = $"formats[{id}]";
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ContentException("formats: every format needs an id.");
            }
            if (days < 1)
            {
                throw new ContentException($"{path}.days ({days}) must be at least 1.");
            }
            if (inningsPerSide < 1 || inningsPerSide > 2)
            {
                throw new ContentException($"{path}.inningsPerSide ({inningsPerSide}) must be 1 or 2.");
            }
            if (oversPerInnings <= 0)
            {
                throw new ContentException($"{path}.oversPerInnings ({oversPerInnings}) must be above 0.");
            }
            if (oversPerDay <= 0)
            {
                throw new ContentException($"{path}.oversPerDay ({oversPerDay}) must be above 0.");
            }
            if (oversPerInnings == null && oversPerDay == null)
            {
                throw new ContentException($"{path} needs oversPerInnings, oversPerDay or both, or play would never end.");
            }
            CheckSessions(path, sessions);
            CheckDecisionHours(path, decisionHours, sessions);

            Id = id;
            Name = name;
            Days = days;
            InningsPerSide = inningsPerSide;
            OversPerInnings = oversPerInnings;
            OversPerDay = oversPerDay;
            Sessions = new List<PlaySession>(sessions).AsReadOnly();
            DecisionHours = new List<int>(decisionHours).AsReadOnly();
        }

        public string Id { get; }
        public string Name { get; }
        public int Days { get; }
        public int InningsPerSide { get; }

        /// <summary>Overs limit per innings, or null for unlimited innings in multi-day cricket.</summary>
        public int? OversPerInnings { get; }

        /// <summary>Overs scheduled per day in multi-day cricket; null in limited-overs formats.</summary>
        public int? OversPerDay { get; }

        /// <summary>Each day's sessions of play.</summary>
        public IReadOnlyList<PlaySession> Sessions { get; }

        /// <summary>The player's turns on a match day: before play and at each break.</summary>
        public IReadOnlyList<int> DecisionHours { get; }

        private static void CheckSessions(string path, IReadOnlyList<PlaySession> sessions)
        {
            if (sessions.Count == 0)
            {
                throw new ContentException($"{path}.sessions needs at least one session.");
            }
            for (var i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                var earliest = i == 0 ? 0 : sessions[i - 1].End;
                if (session.Start < earliest || session.End <= session.Start || session.End > 24)
                {
                    throw new ContentException($"{path}.sessions[{i}] ({session.Start} to {session.End}) must start after the one before, end after it starts, and finish by 24.");
                }
            }
        }

        private static void CheckDecisionHours(string path, IReadOnlyList<int> hours, IReadOnlyList<PlaySession> sessions)
        {
            if (hours.Count == 0)
            {
                throw new ContentException($"{path}.decisionHours needs at least one hour.");
            }
            for (var i = 0; i < hours.Count; i++)
            {
                if (hours[i] < 0 || hours[i] > 23 || (i > 0 && hours[i] <= hours[i - 1]))
                {
                    throw new ContentException($"{path}.decisionHours must be hours from 0 to 23 in ascending order with no repeats.");
                }
            }
            if (hours[0] > sessions[0].Start)
            {
                throw new ContentException($"{path}.decisionHours must start by the first session ({sessions[0].Start}), so there's a turn before play.");
            }
        }
    }
}
