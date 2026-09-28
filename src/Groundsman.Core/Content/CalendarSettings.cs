using System.Collections.Generic;

namespace Groundsman.Core.Content
{
    public sealed class CalendarSettings
    {
        public CalendarSettings(
            MonthDay seasonStart,
            MonthDay seasonEnd,
            int morningHour,
            int afternoonHour,
            int offSeasonStepDays,
            int finalPrepDays)
        {
            if (seasonEnd.CompareTo(seasonStart) <= 0)
            {
                throw new ContentException($"calendar.seasonEnd ({seasonEnd}) must be after seasonStart ({seasonStart}).");
            }
            CheckHour("morningHour", morningHour);
            CheckHour("afternoonHour", afternoonHour);
            if (afternoonHour <= morningHour)
            {
                throw new ContentException($"calendar.afternoonHour ({afternoonHour}) must be after morningHour ({morningHour}).");
            }
            if (offSeasonStepDays < 1)
            {
                throw new ContentException($"calendar.offSeasonStepDays ({offSeasonStepDays}) must be at least 1.");
            }
            if (finalPrepDays < 0)
            {
                throw new ContentException($"calendar.finalPrepDays ({finalPrepDays}) can't be negative.");
            }
            SeasonStart = seasonStart;
            SeasonEnd = seasonEnd;
            MorningHour = morningHour;
            AfternoonHour = afternoonHour;
            OffSeasonStepDays = offSeasonStepDays;
            FinalPrepDays = finalPrepDays;
        }

        public MonthDay SeasonStart { get; }
        public MonthDay SeasonEnd { get; }
        public int MorningHour { get; }
        public int AfternoonHour { get; }
        public int OffSeasonStepDays { get; }
        public int FinalPrepDays { get; }

        private static void CheckHour(string field, int hour)
        {
            if (hour < 0 || hour > 23)
            {
                throw new ContentException($"calendar.{field} ({hour}) must be an hour from 0 to 23.");
            }
        }
    }
}
