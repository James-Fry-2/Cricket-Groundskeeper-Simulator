using System;
using System.Collections.Generic;
using System.Globalization;
using Groundsman.Core.Staff;
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
                Required(file, "finalPrepDays", dto.FinalPrepDays));
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
                Required(file, "evaporation.perSunshineHourMm", evaporation.PerSunshineHourMm),
                Required(file, "rootUptakeShare", dto.RootUptakeShare),
                Required(file, "rootUptakeCurve", dto.RootUptakeCurve));
        }

        public static CoverSettings ParseCovers(string json)
        {
            const string file = "covers";
            var dto = Deserialise<CoversDto>(json, file);

            return new CoverSettings(
                Required(file, "count", dto.Count),
                Required(file, "evaporationFactor", dto.EvaporationFactor));
        }

        public static StaffSettings ParseStaff(string json)
        {
            const string file = "staff";
            var dto = Deserialise<StaffDto>(json, file);
            var memberDtos = Required(file, "staff", dto.Staff);
            var jobs = Required(file, "jobHours", dto.JobHours);

            var members = new StaffMemberSettings[memberDtos.Count];
            for (var i = 0; i < members.Length; i++)
            {
                var member = memberDtos[i];
                var path = $"staff[{i}]";
                members[i] = new StaffMemberSettings(
                    new StaffId(Required(file, path + ".id", member.Id)),
                    Required(file, path + ".name", member.Name),
                    Required(file, path + ".hoursPerDay", member.HoursPerDay),
                    Required(file, path + ".readingSkill", member.ReadingSkill));
            }

            return new StaffSettings(
                members,
                Required(file, "jobHours.water", jobs.Water),
                Required(file, "jobHours.cover", jobs.Cover),
                Required(file, "jobHours.uncover", jobs.Uncover),
                Required(file, "jobHours.probeReading", jobs.ProbeReading),
                Required(file, "jobHours.feelReading", jobs.FeelReading),
                Required(file, "jobHours.soilCore", jobs.SoilCore));
        }

        public static ForecastSettings ParseForecast(string json)
        {
            const string file = "forecast";
            var dto = Deserialise<ForecastDto>(json, file);
            var rain = Required(file, "rain", dto.Rain);
            var temperature = Required(file, "maxTemperature", dto.MaxTemperature);

            return new ForecastSettings(
                Required(file, "days", dto.Days),
                Required(file, "rangeSpreads", dto.RangeSpreads),
                Required(file, "rain.errorSd", rain.ErrorSd),
                Required(file, "rain.growthPerDay", rain.GrowthPerDay),
                Required(file, "maxTemperature.errorSd", temperature.ErrorSd),
                Required(file, "maxTemperature.growthPerDay", temperature.GrowthPerDay));
        }

        public static ScoringSettings ParseScoring(string json)
        {
            const string file = "scoring";
            var morning = Required(file, "matchMorning", Deserialise<ScoringDto>(json, file).MatchMorning);

            return new ScoringSettings(
                Required(file, "matchMorning.subsurfaceMin", morning.SubsurfaceMin),
                Required(file, "matchMorning.subsurfaceMax", morning.SubsurfaceMax),
                Required(file, "matchMorning.surfaceMax", morning.SurfaceMax));
        }

        public static ReadingSettings ParseReadings(string json)
        {
            const string file = "readings";
            var dto = Deserialise<ReadingsDto>(json, file);
            var probe = Required(file, "moistureProbe", dto.MoistureProbe);
            var core = Required(file, "soilCore", dto.SoilCore);
            var feel = Required(file, "feel", dto.Feel);
            var ageing = Required(file, "ageing", dto.Ageing);
            var bandDtos = Required(file, "feel.bands", feel.Bands);

            var bands = new FeelBand[bandDtos.Count];
            for (var i = 0; i < bands.Length; i++)
            {
                var band = bandDtos[i];
                var path = $"feel.bands[{i}]";
                bands[i] = new FeelBand(
                    Required(file, path + ".word", band.Word),
                    Required(file, path + ".from", band.From),
                    Required(file, path + ".to", band.To));
            }

            return new ReadingSettings(
                Required(file, "moistureProbe.width", probe.Width),
                Required(file, "moistureProbe.missRate", probe.MissRate),
                Required(file, "soilCore.width", core.Width),
                Required(file, "soilCore.missRate", core.MissRate),
                Required(file, "feel.judgementSd", feel.JudgementSd),
                bands,
                Required(file, "ageing.widenPerDay", ageing.WidenPerDay),
                Required(file, "ageing.widenPerMmWater", ageing.WidenPerMmWater));
        }

        public static SeasonSettings ParseSeason(string json, IReadOnlyList<FormatSettings> formats, TeamsSettings teams)
        {
            const string file = "season";
            var dto = Deserialise<SeasonDto>(json, file);
            var fixtureDtos = Required(file, "fixtures", dto.Fixtures);

            var fixtures = new Fixture[fixtureDtos.Count];
            for (var i = 0; i < fixtures.Length; i++)
            {
                var fixture = fixtureDtos[i];
                var path = $"fixtures[{i}]";
                var formatId = Required(file, path + ".format", fixture.Format);
                var format = FindFormat(formats, formatId)
                    ?? throw new ContentException($"{file}.{path}.format (\"{formatId}\") isn't in formats.json.");
                var opponentId = Required(file, path + ".opponent", fixture.Opponent);
                var opponent = teams.Find(opponentId)
                    ?? throw new ContentException($"{file}.{path}.opponent (\"{opponentId}\") isn't in teams.json.");
                if (opponentId == teams.HomeId)
                {
                    throw new ContentException($"{file}.{path}.opponent (\"{opponentId}\") is the home team.");
                }
                fixtures[i] = new Fixture(
                    ParseDate(file, path + ".start", fixture.Start),
                    format,
                    new StripId(Required(file, path + ".strip", fixture.Strip)),
                    opponent);
            }

            return new SeasonSettings(ParseDate(file, "start", dto.Start), fixtures);
        }

        public static IReadOnlyList<FormatSettings> ParseFormats(string json)
        {
            const string file = "formats";
            var dtos = Required(file, "formats", Deserialise<FormatsDto>(json, file).Formats);

            var formats = new List<FormatSettings>();
            var ids = new HashSet<string>();
            for (var i = 0; i < dtos.Count; i++)
            {
                var format = dtos[i];
                var path = $"formats[{i}]";
                var id = Required(file, path + ".id", format.Id);
                if (!ids.Add(id))
                {
                    throw new ContentException($"formats: two formats have the id \"{id}\".");
                }
                var sessionDtos = Required(file, path + ".sessions", format.Sessions);
                var sessions = new PlaySession[sessionDtos.Count];
                for (var s = 0; s < sessions.Length; s++)
                {
                    sessions[s] = new PlaySession(
                        Required(file, $"{path}.sessions[{s}].start", sessionDtos[s].Start),
                        Required(file, $"{path}.sessions[{s}].end", sessionDtos[s].End));
                }
                formats.Add(new FormatSettings(
                    id,
                    Required(file, path + ".name", format.Name),
                    Required(file, path + ".days", format.Days),
                    Required(file, path + ".inningsPerSide", format.InningsPerSide),
                    format.OversPerInnings,
                    format.OversPerDay,
                    sessions,
                    Required(file, path + ".decisionHours", format.DecisionHours)));
            }
            return formats.AsReadOnly();
        }

        public static TeamsSettings ParseTeams(string json)
        {
            const string file = "teams";
            var dto = Deserialise<TeamsDto>(json, file);
            var teamDtos = Required(file, "teams", dto.Teams);

            var teams = new TeamSettings[teamDtos.Count];
            for (var i = 0; i < teams.Length; i++)
            {
                var team = teamDtos[i];
                var path = $"teams[{i}]";
                var attack = Required(file, path + ".attack", team.Attack);
                teams[i] = new TeamSettings(
                    Required(file, path + ".id", team.Id),
                    Required(file, path + ".name", team.Name),
                    Required(file, path + ".batting", team.Batting),
                    Required(file, path + ".bowling", team.Bowling),
                    new AttackProfile(
                        Required(file, path + ".attack.seam", attack.Seam),
                        Required(file, path + ".attack.leftArm", attack.LeftArm),
                        Required(file, path + ".attack.heavyFooted", attack.HeavyFooted)));
            }

            return new TeamsSettings(teams, Required(file, "home", dto.Home));
        }

        private static FormatSettings? FindFormat(IReadOnlyList<FormatSettings> formats, string id)
        {
            foreach (var format in formats)
            {
                if (format.Id == id)
                {
                    return format;
                }
            }
            return null;
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
            public ProbeDto? MoistureProbe { get; set; }
            public ProbeDto? SoilCore { get; set; }
            public FeelDto? Feel { get; set; }
            public AgeingDto? Ageing { get; set; }
        }

        private sealed class ProbeDto
        {
            public double? Width { get; set; }
            public double? MissRate { get; set; }
        }

        private sealed class FeelDto
        {
            public double? JudgementSd { get; set; }
            public List<FeelBandDto>? Bands { get; set; }
        }

        private sealed class FeelBandDto
        {
            public string? Word { get; set; }
            public double? From { get; set; }
            public double? To { get; set; }
        }

        private sealed class AgeingDto
        {
            public double? WidenPerDay { get; set; }
            public double? WidenPerMmWater { get; set; }
        }

        private sealed class SeasonDto
        {
            public string? Start { get; set; }
            public List<FixtureDto>? Fixtures { get; set; }
        }

        private sealed class FixtureDto
        {
            public string? Start { get; set; }
            public string? Format { get; set; }
            public string? Opponent { get; set; }
            public int? Strip { get; set; }
        }

        private sealed class FormatsDto
        {
            public List<FormatDto>? Formats { get; set; }
        }

        private sealed class FormatDto
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public int? Days { get; set; }
            public int? InningsPerSide { get; set; }
            public int? OversPerInnings { get; set; }
            public int? OversPerDay { get; set; }
            public List<SessionDto>? Sessions { get; set; }
            public List<int>? DecisionHours { get; set; }
        }

        private sealed class SessionDto
        {
            public int? Start { get; set; }
            public int? End { get; set; }
        }

        private sealed class TeamsDto
        {
            public string? Home { get; set; }
            public List<TeamDto>? Teams { get; set; }
        }

        private sealed class TeamDto
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public double? Batting { get; set; }
            public double? Bowling { get; set; }
            public AttackDto? Attack { get; set; }
        }

        private sealed class AttackDto
        {
            public double? Seam { get; set; }
            public double? LeftArm { get; set; }
            public double? HeavyFooted { get; set; }
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
            public double? RootUptakeShare { get; set; }
            public double? RootUptakeCurve { get; set; }
        }

        private sealed class EvaporationDto
        {
            public double? PerDegreeMm { get; set; }
            public double? WindFactorPerKph { get; set; }
            public double? PerSunshineHourMm { get; set; }
        }

        private sealed class CoversDto
        {
            public int? Count { get; set; }
            public double? EvaporationFactor { get; set; }
        }

        private sealed class StaffDto
        {
            public List<StaffMemberDto>? Staff { get; set; }
            public JobHoursDto? JobHours { get; set; }
        }

        private sealed class StaffMemberDto
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public double? HoursPerDay { get; set; }
            public double? ReadingSkill { get; set; }
        }

        private sealed class JobHoursDto
        {
            public double? Water { get; set; }
            public double? Cover { get; set; }
            public double? Uncover { get; set; }
            public double? ProbeReading { get; set; }
            public double? FeelReading { get; set; }
            public double? SoilCore { get; set; }
        }

        private sealed class ForecastDto
        {
            public int? Days { get; set; }
            public double? RangeSpreads { get; set; }
            public ForecastErrorDto? Rain { get; set; }
            public ForecastErrorDto? MaxTemperature { get; set; }
        }

        private sealed class ForecastErrorDto
        {
            public double? ErrorSd { get; set; }
            public double? GrowthPerDay { get; set; }
        }

        private sealed class ScoringDto
        {
            public MatchMorningDto? MatchMorning { get; set; }
        }

        private sealed class MatchMorningDto
        {
            public double? SubsurfaceMin { get; set; }
            public double? SubsurfaceMax { get; set; }
            public double? SurfaceMax { get; set; }
        }
    }
}
