using System;
using System.Collections.Generic;
using Groundsman.Core.Content;

namespace Groundsman.Core.Time
{
    public sealed class PaceContext
    {
        private readonly SortedSet<DateTime> _matchDays = new SortedSet<DateTime>();
        private readonly Dictionary<DateTime, IReadOnlyList<int>> _matchDayHours = new Dictionary<DateTime, IReadOnlyList<int>>();

        public PaceContext(CalendarSettings settings, IEnumerable<Fixture> fixtures)
        {
            Settings = settings;
            foreach (var fixture in fixtures)
            {
                for (var day = fixture.Start; day <= fixture.End; day = day.AddDays(1))
                {
                    _matchDays.Add(day);
                    _matchDayHours[day] = fixture.Format.DecisionHours;
                }
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

        public DateTime? NextMatchDayFrom(DateTime date)
        {
            foreach (var day in _matchDays.GetViewBetween(date.Date, DateTime.MaxValue.Date))
            {
                return day;
            }
            return null;
        }

        public IReadOnlyList<int> DecisionHoursOn(DateTime date)
        {
            switch (PaceOn(date))
            {
                case DayPace.MatchDay:
                    return _matchDayHours[date.Date];
                case DayPace.FinalPrep:
                    return new[] { Settings.MorningHour, Settings.AfternoonHour };
                default:
                    return new[] { Settings.MorningHour };
            }
        }
    }
}
