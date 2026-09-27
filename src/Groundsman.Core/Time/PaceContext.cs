using System;
using System.Collections.Generic;
using Groundsman.Core.Content;

namespace Groundsman.Core.Time
{
    public sealed class PaceContext
    {
        private readonly SortedSet<DateTime> _matchDays = new SortedSet<DateTime>();

        public PaceContext(CalendarSettings settings, IEnumerable<DateTime> matchDays)
        {
            Settings = settings;
            foreach (var day in matchDays)
            {
                _matchDays.Add(day.Date);
            }
        }

        public CalendarSettings Settings { get; }

        public DayPace PaceOn(DateTime date)
        {
            date = date.Date;
            if (_matchDays.Contains(date))
            {
                return DayPace.MatchDay;
            }
            if (Settings.FinalPrepDays > 0 && _matchDays.GetViewBetween(date.AddDays(1), date.AddDays(Settings.FinalPrepDays)).Count > 0)
            {
                return DayPace.FinalPrep;
            }
            if (date >= Settings.SeasonStart.In(date.Year) && date <= Settings.SeasonEnd.In(date.Year))
            {
                return DayPace.InSeason;
            }
            return DayPace.OffSeason;
        }

        public IReadOnlyList<int> DecisionHoursOn(DateTime date)
        {
            switch (PaceOn(date))
            {
                case DayPace.MatchDay:
                    return Settings.MatchDayDecisionHours;
                case DayPace.FinalPrep:
                    return new[] { Settings.MorningHour, Settings.AfternoonHour };
                default:
                    return new[] { Settings.MorningHour };
            }
        }
    }
}
