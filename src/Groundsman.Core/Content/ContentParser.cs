using System.Collections.Generic;
using Newtonsoft.Json;

namespace Groundsman.Core.Content
{
    /// <summary>
    /// Parses content JSON handed in by a front end. File access stays with the caller so the
    /// core never touches the file system.
    /// </summary>
    public static class ContentParser
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error,
        };

        public static CalendarSettings ParseCalendar(string json)
        {
            var dto = Deserialise<CalendarDto>(json, "calendar");

            return new CalendarSettings(
                ParseMonthDay("seasonStart", dto.SeasonStart),
                ParseMonthDay("seasonEnd", dto.SeasonEnd),
                Required("morningHour", dto.MorningHour),
                Required("afternoonHour", dto.AfternoonHour),
                Required("offSeasonStepDays", dto.OffSeasonStepDays),
                Required("finalPrepDays", dto.FinalPrepDays),
                dto.MatchDayDecisionHours ?? throw Missing("matchDayDecisionHours"));
        }

        private static T Deserialise<T>(string json, string file)
            where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json, Settings)
                    ?? throw new ContentException($"{file} content is empty.");
            }
            catch (JsonException e)
            {
                throw new ContentException($"{file} content is not valid: {e.Message}", e);
            }
        }

        private static MonthDay ParseMonthDay(string field, string? text)
        {
            if (text == null)
            {
                throw Missing(field);
            }
            if (!MonthDay.TryParse(text, out var value))
            {
                throw new ContentException($"calendar.{field} (\"{text}\") must be a date that occurs every year, as MM-dd.");
            }
            return value;
        }

        private static int Required(string field, int? value) => value ?? throw Missing(field);

        private static ContentException Missing(string field) => new ContentException($"calendar.{field} is missing.");

        private sealed class CalendarDto
        {
            public string? SeasonStart { get; set; }
            public string? SeasonEnd { get; set; }
            public int? MorningHour { get; set; }
            public int? AfternoonHour { get; set; }
            public int? OffSeasonStepDays { get; set; }
            public int? FinalPrepDays { get; set; }
            public List<int>? MatchDayDecisionHours { get; set; }
        }
    }
}
