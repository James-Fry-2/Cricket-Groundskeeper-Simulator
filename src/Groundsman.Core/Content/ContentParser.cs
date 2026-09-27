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
                    Required(file, path + ".loam", strip.Loam),
                    Required(file, path + ".surfaceMoisture", strip.SurfaceMoisture),
                    Required(file, path + ".subsurfaceMoisture", strip.SubsurfaceMoisture));
            }

            return new GroundSettings(Required(file, "name", dto.Name), strips);
        }

        public static TaskSettings ParseTasks(string json)
        {
            const string file = "tasks";
            var dto = Deserialise<TasksDto>(json, file);

            return new TaskSettings(Required(file, "waterMm", dto.WaterMm));
        }

        public static IReadOnlyList<LoamSettings> ParseLoams(string json)
        {
            const string file = "loams";
            var dtos = Required(file, "loams", Deserialise<LoamsDto>(json, file).Loams);

            var loams = new List<LoamSettings>();
            var ids = new HashSet<string>();
            for (var i = 0; i < dtos.Count; i++)
            {
                var loam = dtos[i];
                var path = $"loams[{i}]";
                var id = Required(file, path + ".id", loam.Id);
                if (!ids.Add(id))
                {
                    throw new ContentException($"loams: two loams have the id \"{id}\".");
                }
                loams.Add(new LoamSettings(
                    id,
                    Required(file, path + ".name", loam.Name),
                    Required(file, path + ".clayPercent", loam.ClayPercent),
                    Required(file, path + ".saturation", loam.Saturation),
                    Required(file, path + ".fieldCapacity", loam.FieldCapacity),
                    Required(file, path + ".airDry", loam.AirDry),
                    Required(file, path + ".surfaceDrainageRate", loam.SurfaceDrainageRate),
                    Required(file, path + ".subsurfaceDrainageRate", loam.SubsurfaceDrainageRate),
                    Required(file, path + ".capillaryRate", loam.CapillaryRate),
                    Required(file, path + ".crackingTendency", loam.CrackingTendency)));
            }
            if (loams.Count == 0)
            {
                throw new ContentException("loams needs at least one loam.");
            }
            return loams.AsReadOnly();
        }

        public static MoistureSettings ParseMoisture(string json)
        {
            const string file = "moisture";
            var dto = Deserialise<MoistureDto>(json, file);
            var evaporation = Required(file, "evaporation", dto.Evaporation);

            return new MoistureSettings(
                Required(file, "surfaceDepthMm", dto.SurfaceDepthMm),
                Required(file, "subsurfaceDepthMm", dto.SubsurfaceDepthMm),
                Required(file, "evaporation.perDegreeMm", evaporation.PerDegreeMm),
                Required(file, "evaporation.windFactorPerKph", evaporation.WindFactorPerKph),
                Required(file, "evaporation.perSunshineHourMm", evaporation.PerSunshineHourMm));
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

        public static ClimateSettings ParseClimate(string json)
        {
            const string file = "climate";
            var dto = Deserialise<ClimateDto>(json, file);
            var spell = Required(file, "rainSpellHours", dto.RainSpellHours);
            var monthDtos = Required(file, "months", dto.Months);

            var months = new MonthClimate[monthDtos.Count];
            for (var i = 0; i < months.Length; i++)
            {
                var m = monthDtos[i];
                var path = $"months[{i}]";
                months[i] = new MonthClimate(
                    Required(file, path + ".month", m.Month),
                    Required(file, path + ".meanTemperature", m.MeanTemperature),
                    Required(file, path + ".dailyRange", m.DailyRange),
                    Required(file, path + ".rainDays", m.RainDays),
                    Required(file, path + ".rainTotalMm", m.RainTotalMm),
                    Required(file, path + ".sunshineHours", m.SunshineHours),
                    Required(file, path + ".meanWindKph", m.MeanWindKph),
                    Required(file, path + ".daylightHours", m.DaylightHours),
                    Required(file, path + ".sunWarmth", m.SunWarmth));
            }

            return new ClimateSettings(
                Required(file, "rainDayThresholdMm", dto.RainDayThresholdMm),
                Required(file, "wetDayPersistence", dto.WetDayPersistence),
                Required(file, "rainSpellHours.min", spell.Min),
                Required(file, "rainSpellHours.max", spell.Max),
                Required(file, "temperatureAnomalySd", dto.TemperatureAnomalySd),
                Required(file, "temperatureAnomalyPersistence", dto.TemperatureAnomalyPersistence),
                Required(file, "windVariability", dto.WindVariability),
                Required(file, "wetDaySunshineFactor", dto.WetDaySunshineFactor),
                Required(file, "cloudVariability", dto.CloudVariability),
                Required(file, "cloudPersistence", dto.CloudPersistence),
                Required(file, "sunshineRangeEffect", dto.SunshineRangeEffect),
                Required(file, "rainCooling", dto.RainCooling),
                Required(file, "wetDayWindFactor", dto.WetDayWindFactor),
                months);
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
            public List<StripDto>? Strips { get; set; }
        }

        private sealed class StripDto
        {
            public int? Number { get; set; }
            public string? Loam { get; set; }
            public double? SurfaceMoisture { get; set; }
            public double? SubsurfaceMoisture { get; set; }
        }

        private sealed class TasksDto
        {
            public double? WaterMm { get; set; }
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

        private sealed class ClimateDto
        {
            public double? RainDayThresholdMm { get; set; }
            public double? WetDayPersistence { get; set; }
            public HourRangeDto? RainSpellHours { get; set; }
            public double? TemperatureAnomalySd { get; set; }
            public double? TemperatureAnomalyPersistence { get; set; }
            public double? WindVariability { get; set; }
            public double? WetDaySunshineFactor { get; set; }
            public double? CloudVariability { get; set; }
            public double? CloudPersistence { get; set; }
            public double? SunshineRangeEffect { get; set; }
            public double? RainCooling { get; set; }
            public double? WetDayWindFactor { get; set; }
            public List<MonthClimateDto>? Months { get; set; }
        }

        private sealed class HourRangeDto
        {
            public int? Min { get; set; }
            public int? Max { get; set; }
        }

        private sealed class MonthClimateDto
        {
            public int? Month { get; set; }
            public double? MeanTemperature { get; set; }
            public double? DailyRange { get; set; }
            public double? RainDays { get; set; }
            public double? RainTotalMm { get; set; }
            public double? SunshineHours { get; set; }
            public double? MeanWindKph { get; set; }
            public double? DaylightHours { get; set; }
            public double? SunWarmth { get; set; }
        }

        private sealed class LoamsDto
        {
            public List<LoamDto>? Loams { get; set; }
        }

        private sealed class LoamDto
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public double? ClayPercent { get; set; }
            public double? Saturation { get; set; }
            public double? FieldCapacity { get; set; }
            public double? AirDry { get; set; }
            public double? SurfaceDrainageRate { get; set; }
            public double? SubsurfaceDrainageRate { get; set; }
            public double? CapillaryRate { get; set; }
            public double? CrackingTendency { get; set; }
        }

        private sealed class MoistureDto
        {
            public double? SurfaceDepthMm { get; set; }
            public double? SubsurfaceDepthMm { get; set; }
            public EvaporationDto? Evaporation { get; set; }
        }

        private sealed class EvaporationDto
        {
            public double? PerDegreeMm { get; set; }
            public double? WindFactorPerKph { get; set; }
            public double? PerSunshineHourMm { get; set; }
        }
    }
}
