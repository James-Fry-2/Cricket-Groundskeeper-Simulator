using System;

namespace Groundsman.Core.Time
{
    public sealed class PaceRules
    {
        private readonly PaceContext _context;

        public PaceRules(PaceContext context)
        {
            _context = context;
        }

        public GameTime NextDecisionPoint(GameTime now)
        {
            foreach (var hour in _context.DecisionHoursOn(now.Date))
            {
                if (hour > now.Hour)
                {
                    return now.AtHour(hour);
                }
            }

            var horizon = _context.PaceOn(now.Date) == DayPace.OffSeason
                ? _context.Settings.OffSeasonStepDays
                : 1;

            // A week-long step must not skip a day that needs finer turns, so stop at the first one.
            for (var days = 1; days < horizon; days++)
            {
                var date = now.Date.AddDays(days);
                if (_context.PaceOn(date) != DayPace.OffSeason)
                {
                    return FirstDecisionOn(date);
                }
            }

            return FirstDecisionOn(now.Date.AddDays(horizon));
        }

        private GameTime FirstDecisionOn(DateTime date) => GameTime.OnDate(date, _context.DecisionHoursOn(date)[0]);
    }
}
