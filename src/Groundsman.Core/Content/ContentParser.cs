using System;
using System.Collections.Generic;
using System.Globalization;
using Groundsman.Core.Strips;
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
            DateParseHandling = DateParseHandling.None,
        };

        public static CalendarSettings ParseCalendar(string json)
        {
            const string file = "calendar";
            var dto = Deserialise<CalendarDto>(json, file);

            return new CalendarSettings(
                ParseMonthDay(file, "seasonStart", dto.SeasonStart),
                ParseMonthDay(file, "seasonEnd", dto.SeasonEnd),
                Required(file, "morningHour", dto.MorningHour),
                Required(file, "afternoonHour", dto.AfternoonHour),
                Required(file, "offSeasonStepDays", dto.OffSeasonStepDays),
                Required(file, "finalPrepDays", dto.FinalPrepDays),
                Required(file, "matchDayDecisionHours", dto.MatchDayDecisionHours));
        }

        public static GroundSettings ParseGround(string json)
        {
            const string file = "ground";
            var dto = Deserialise<GroundDto>(json, file);
            var stripDtos = Required(file, "strips", dto.Strips);

            var strips = new StripSettings[stripDtos.Count];
            for (var i = 0; i < strips.Length; i++)
            {
                var strip = stripDtos[i];
                var path = $"strips[{i}]";
                strips[i] = new StripSettings(
                    new StripId(Required(file, path + ".number", strip.Number)),
                    Required(file, path + ".surfaceMoisture", strip.SurfaceMoisture),
                    Required(file, path + ".subsurfaceMoisture", strip.SubsurfaceMoisture));
            }

            return new GroundSettings(
                Required(file, "name", dto.Name),
                Required(file, "saturation", dto.Saturation),
                strips);
        }

        public static TaskSettings ParseTasks(string json)
        {
            const string file = "tasks";
            var dto = Deserialise<TasksDto>(json, file);

            return new TaskSettings(Required(file, "waterSurfaceGain", dto.WaterSurfaceGain));
        }

        public static ReadingSettings ParseReadings(string json)
        {
            const string file = "readings";
            var dto = Deserialise<ReadingsDto>(json, file);

            return new ReadingSettings(Required(file, "moistureProbeWidth", dto.MoistureProbeWidth));
        }

        public static SeasonSettings ParseSeason(string json)
        {
            const string file = "season";
            var dto = Deserialise<SeasonDto>(json, file);
            var matchDayTexts = Required(file, "matchDays", dto.MatchDays);

            var matchDays = new DateTime[matchDayTexts.Count];
            for (var i = 0; i < matchDays.Length; i++)
            {
                matchDays[i] = ParseDate(file, $"matchDays[{i}]", matchDayTexts[i]);
            }

            return new SeasonSettings(ParseDate(file, "start", dto.Start), matchDays);
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

        private static MonthDay ParseMonthDay(string file, string field, string? text)
        {
            if (!MonthDay.TryParse(Required(file, field, text), out var value))
            {
                throw new ContentException($"{file}.{field} (\"{text}\") must be a date that occurs every year, as MM-dd.");
            }
            return value;
        }

        private static DateTime ParseDate(string file, string field, string? text)
        {
            if (!DateTime.TryParseExact(Required(file, field, text), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
            {
                throw new ContentException($"{file}.{field} (\"{text}\") must be a date as yyyy-MM-dd.");
            }
            return value;
        }

        private static T Required<T>(string file, string field, T? value)
            where T : class
            => value ?? throw Missing(file, field);

        private static T Required<T>(string file, string field, T? value)
            where T : struct
            => value ?? throw Missing(file, field);

        private static ContentException Missing(string file, string field) => new ContentException($"{file}.{field} is missing.");

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

        private sealed class GroundDto
        {
            public string? Name { get; set; }
            public double? Saturation { get; set; }
            public List<StripDto>? Strips { get; set; }
        }

        private sealed class StripDto
        {
            public int? Number { get; set; }
            public double? SurfaceMoisture { get; set; }
            public double? SubsurfaceMoisture { get; set; }
        }

        private sealed class TasksDto
        {
            public double? WaterSurfaceGain { get; set; }
        }

        private sealed class ReadingsDto
        {
            public double? MoistureProbeWidth { get; set; }
        }

        private sealed class SeasonDto
        {
            public string? Start { get; set; }
            public List<string>? MatchDays { get; set; }
        }
    }
}
