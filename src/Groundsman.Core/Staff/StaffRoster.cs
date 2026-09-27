using System;
using Groundsman.Core.Content;

namespace Groundsman.Core.Staff
{
    /// <summary>
    /// Hours each person has left. Hours belong to a calendar day, so a day's morning and
    /// afternoon turns share them.
    /// </summary>
    internal sealed class StaffRoster
    {
        private readonly StaffSettings _settings;
        private readonly double[] _hoursLeft;
        private DateTime _day;

        public StaffRoster(StaffSettings settings, DateTime firstDay)
        {
            _settings = settings;
            _hoursLeft = new double[settings.Members.Count];
            StartDay(firstDay);
        }

        public StaffId Player => _settings.Members[0].Id;

        public StaffMemberSettings? Find(StaffId id)
        {
            foreach (var member in _settings.Members)
            {
                if (member.Id == id)
                {
                    return member;
                }
            }
            return null;
        }

        public double HoursLeft(StaffId id) => _hoursLeft[IndexOf(id)];

        public void Spend(StaffId id, double hours) => _hoursLeft[IndexOf(id)] -= hours;

        /// <summary>Refills everyone's hours if the date has moved on.</summary>
        public void StartDay(DateTime date)
        {
            if (date.Date == _day)
            {
                return;
            }

            _day = date.Date;
            for (var i = 0; i < _hoursLeft.Length; i++)
            {
                _hoursLeft[i] = _settings.Members[i].HoursPerDay;
            }
        }

        private int IndexOf(StaffId id)
        {
            for (var i = 0; i < _settings.Members.Count; i++)
            {
                if (_settings.Members[i].Id == id)
                {
                    return i;
                }
            }
            throw new ArgumentOutOfRangeException(nameof(id), id, "No such member of staff.");
        }
    }
}
