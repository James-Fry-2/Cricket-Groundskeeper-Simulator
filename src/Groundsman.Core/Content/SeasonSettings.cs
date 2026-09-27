using System;
using System.Collections.Generic;

namespace Groundsman.Core.Content
{
    /// <summary>
    /// When a new game starts and which days have matches. Stands in until fixtures exist.
    /// </summary>
    public sealed class SeasonSettings
    {
        public SeasonSettings(DateTime start, IReadOnlyList<DateTime> matchDays)
        {
            for (var i = 0; i < matchDays.Count; i++)
            {
                if (matchDays[i] < start)
                {
                    throw new ContentException($"season.matchDays[{i}] ({matchDays[i]:yyyy-MM-dd}) is before the start ({start:yyyy-MM-dd}).");
                }
                if (i > 0 && matchDays[i] <= matchDays[i - 1])
                {
                    throw new ContentException("season.matchDays must be in date order with no repeats.");
                }
            }

            Start = start.Date;
            MatchDays = new List<DateTime>(matchDays).AsReadOnly();
        }

        public DateTime Start { get; }
        public IReadOnlyList<DateTime> MatchDays { get; }
    }
}
