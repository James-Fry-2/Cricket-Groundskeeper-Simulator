using System;
using System.Collections.Generic;
using System.Globalization;
using Groundsman.Core.Pressures;
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
                Required(file, "finalPrepDays", dto.FinalPrepDays),
                Required(file, "assignLockDaysOut", dto.AssignLockDaysOut));
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

            return new GroundSettings(Required(file, "name", dto.Name), Required(file, "ends", dto.Ends), strips, Required(file, "centreStrips", dto.CentreStrips));
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
                    Required(file, path + ".crackingTendency", loam.CrackingTendency),
                    Required(file, path + ".rollingWindow.min", Required(file, path + ".rollingWindow", loam.RollingWindow).Min),
                    Required(file, path + ".rollingWindow.max", loam.RollingWindow!.Max)));
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
                Required(file, "jobHours.soilCore", jobs.SoilCore),
                Required(file, "jobHours.mow", jobs.Mow),
                Required(file, "jobHours.repairEnds", jobs.RepairEnds),
                Required(file, "jobHours.cleanFootholes", jobs.CleanFootholes),
                Required(file, "jobHours.fillFootholes", jobs.FillFootholes));
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

        public static IReadOnlyList<RollerSettings> ParseRollers(string json)
        {
            const string file = "rollers";
            var dtos = Required(file, "rollers", Deserialise<RollersDto>(json, file).Rollers);

            var rollers = new List<RollerSettings>();
            var ids = new HashSet<string>();
            for (var i = 0; i < dtos.Count; i++)
            {
                var roller = dtos[i];
                var path = $"rollers[{i}]";
                var id = Required(file, path + ".id", roller.Id);
                if (!ids.Add(id))
                {
                    throw new ContentException($"rollers: two rollers have the id \"{id}\".");
                }
                rollers.Add(new RollerSettings(
                    id,
                    Required(file, path + ".name", roller.Name),
                    Required(file, path + ".compactionPerHour", roller.CompactionPerHour),
                    Required(file, path + ".wetDamagePerHour", roller.WetDamagePerHour),
                    Required(file, path + ".overuseAbove", roller.OveruseAbove),
                    Required(file, path + ".overuseDamagePerHour", roller.OveruseDamagePerHour)));
            }
            return rollers.AsReadOnly();
        }

        public static CompactionSettings ParseCompaction(string json)
        {
            const string file = "compaction";
            var dto = Deserialise<CompactionDto>(json, file);
            var starting = Required(file, "starting", dto.Starting);
            var minutes = Required(file, "rollingMinutes", dto.RollingMinutes);
            var hardness = Required(file, "hardness", dto.Hardness);

            return new CompactionSettings(
                Required(file, "starting.compaction", starting.Compaction),
                Required(file, "starting.structureDamage", starting.StructureDamage),
                Required(file, "rollingMinutes.min", minutes.Min),
                Required(file, "rollingMinutes.max", minutes.Max),
                Required(file, "wetDamagePerPoint", dto.WetDamagePerPoint),
                Required(file, "hardness.dryWeight", hardness.DryWeight),
                Required(file, "hardness.clayReference", hardness.ClayReference),
                Required(file, "hardness.clayExponent", hardness.ClayExponent));
        }

        public static StakeholderSettings ParseStakeholders(string json)
        {
            const string file = "stakeholders";
            var dto = Deserialise<StakeholdersDto>(json, file);
            var captain = Required(file, "captain", dto.Captain);
            var board = Required(file, "board", dto.Board);
            var referee = Required(file, "referee", dto.Referee);
            var delivery = Required(file, "delivery", dto.Delivery);
            var review = Required(file, "review", dto.Review);
            var wear = Required(file, "review.wear", review.Wear);

            var characters = new Dictionary<string, IReadOnlyDictionary<RequestKind, double>>();
            foreach (var format in Required(file, "captain.characters", captain.Characters))
            {
                var weights = new Dictionary<RequestKind, double>();
                foreach (var character in format.Value)
                {
                    var kind = character.Key switch
                    {
                        "green" => RequestKind.Green,
                        "turning" => RequestKind.Turning,
                        "pace" => RequestKind.Pace,
                        "flat" => RequestKind.Flat,
                        _ => throw new ContentException($"{file}.captain.characters.{format.Key}.{character.Key} isn't a pitch character: use green, turning, pace or flat."),
                    };
                    weights[kind] = character.Value;
                }
                characters[format.Key] = weights;
            }

            AnswerEffects Answers(string path, AnswersDto? answers)
            {
                var a = Required(file, path, answers);
                return new AnswerEffects(
                    Required(file, path + ".delivered", a.Delivered),
                    Required(file, path + ".notDelivered", a.NotDelivered),
                    Required(file, path + ".declined", a.Declined),
                    Required(file, path + ".ignored", a.Ignored));
            }

            return new StakeholderSettings(
                Required(file, "startingSatisfaction", dto.StartingSatisfaction),
                Required(file, "requestDaysBeforeLock", dto.RequestDaysBeforeLock),
                Required(file, "captain.requestChance", captain.RequestChance),
                characters,
                Required(file, "board.requestChance", board.RequestChance),
                Answers("captain.answers", captain.Answers),
                Answers("board.answers", board.Answers),
                Required(file, "captain.homeWin", captain.HomeWin),
                Required(file, "captain.homeLoss", captain.HomeLoss),
                Required(file, "board.dayFourReached", board.DayFourReached),
                Required(file, "board.shortFourDay", board.ShortFourDay),
                Required(file, "board.noResult", board.NoResult),
                Required(file, "board.televisedCentre", board.TelevisedCentre),
                Required(file, "board.televisedOffCentre", board.TelevisedOffCentre),
                Required(file, "board.perDemerit", board.PerDemerit),
                Required(file, "referee.veryGood", referee.VeryGood),
                Required(file, "referee.satisfactory", referee.Satisfactory),
                Required(file, "referee.unsatisfactory", referee.Unsatisfactory),
                Required(file, "referee.unfit", referee.Unfit),
                Required(file, "delivery.greenSeamDayOne", delivery.GreenSeamDayOne),
                Required(file, "delivery.turningSpinLastDay", delivery.TurningSpinLastDay),
                Required(file, "delivery.paceCarry", delivery.PaceCarry),
                Required(file, "delivery.trueConsistency", delivery.TrueConsistency),
                Required(file, "delivery.flatMovementBelow", delivery.FlatMovementBelow),
                Required(file, "review.contentFrom", review.ContentFrom),
                Required(file, "review.delightedFrom", review.DelightedFrom),
                Required(file, "review.uneasyFrom", review.UneasyFrom),
                Required(file, "review.wear.light", wear.Light),
                Required(file, "review.wear.worn", wear.Worn),
                Required(file, "review.wear.heavy", wear.Heavy));
        }

        public static RatingSettings ParseRating(string json)
        {
            const string file = "rating";
            var dto = Deserialise<RatingDto>(json, file);
            var unsatisfactory = Required(file, "unsatisfactory", dto.Unsatisfactory);
            var veryGood = Required(file, "veryGood", dto.VeryGood);
            var demerits = Required(file, "demerits", dto.Demerits);

            return new RatingSettings(
                Required(file, "unfitBelow", dto.UnfitBelow),
                Required(file, "abandonBelow", dto.AbandonBelow),
                Required(file, "unsatisfactory.consistencyBelow", unsatisfactory.ConsistencyBelow),
                Required(file, "unsatisfactory.carryBelow", unsatisfactory.CarryBelow),
                Required(file, "unsatisfactory.bowlersOversShare", unsatisfactory.BowlersOversShare),
                Required(file, "unsatisfactory.bowlersRunsPerWicket", unsatisfactory.BowlersRunsPerWicket),
                Required(file, "unsatisfactory.limitedParShare", unsatisfactory.LimitedParShare),
                Required(file, "unsatisfactory.lifelessRunsPerWicket", unsatisfactory.LifelessRunsPerWicket),
                Required(file, "unsatisfactory.lifelessMovementBelow", unsatisfactory.LifelessMovementBelow),
                Required(file, "veryGood.consistencyAtLeast", veryGood.ConsistencyAtLeast),
                Required(file, "veryGood.carryAtLeast", veryGood.CarryAtLeast),
                Required(file, "veryGood.movementAtLeast", veryGood.MovementAtLeast),
                Required(file, "demerits.unsatisfactory", demerits.Unsatisfactory),
                Required(file, "demerits.unfit", demerits.Unfit),
                Required(file, "demerits.windowYears", demerits.WindowYears),
                Required(file, "demerits.banAt", demerits.BanAt),
                Required(file, "reasons", dto.Reasons));
        }

        public static CommentarySettings ParseCommentary(string json)
        {
            const string file = "commentary";
            var dto = Deserialise<CommentaryDto>(json, file);
            var eventDtos = Required(file, "events", dto.Events);
            var events = new Dictionary<string, CommentaryEvent>();
            foreach (var pair in eventDtos)
            {
                events[pair.Key] = new CommentaryEvent(
                    Required(file, $"events.{pair.Key}.threshold", pair.Value.Threshold),
                    Required(file, $"events.{pair.Key}.text", pair.Value.Text));
            }

            return new CommentarySettings(
                events,
                Required(file, "causes", dto.Causes),
                Required(file, "rain", dto.Rain),
                Required(file, "session", dto.Session));
        }

        public static MatchSettings ParseMatch(string json)
        {
            const string file = "match";
            var dto = Deserialise<MatchDto>(json, file);
            var wickets = Required(file, "wickets", dto.Wickets);
            var runs = Required(file, "runs", dto.Runs);
            var declaration = Required(file, "declaration", dto.Declaration);

            return new MatchSettings(
                Required(file, "strengthScale", dto.StrengthScale),
                Required(file, "wickets.seam", wickets.Seam),
                Required(file, "wickets.spin", wickets.Spin),
                Required(file, "wickets.uneven", wickets.Uneven),
                Required(file, "wickets.dead", wickets.Dead),
                Required(file, "runs.carry", runs.Carry),
                Required(file, "runs.uneven", runs.Uneven),
                Required(file, "runs.dead", runs.Dead),
                Required(file, "runs.spread", runs.Spread),
                Required(file, "declaration.lead", declaration.Lead),
                Required(file, "declaration.fromDay", declaration.FromDay),
                Required(file, "rainRestartLoss", dto.RainRestartLoss),
                Required(file, "tossBowlFirstSeamAbove", dto.TossBowlFirstSeamAbove));
        }

        public static WearSettings ParseWear(string json)
        {
            const string file = "wear";
            var dto = Deserialise<WearDto>(json, file);
            var resistance = Required(file, "resistance", dto.Resistance);
            var perOver = Required(file, "perOver", dto.PerOver);
            var cracks = Required(file, "cracks", dto.Cracks);
            var recovery = Required(file, "recovery", dto.Recovery);
            var duringMatch = Required(file, "duringMatch", dto.DuringMatch);
            var lasting = Required(file, "lasting", dto.Lasting);
            var ends = Required(file, "ends", dto.Ends);

            return new WearSettings(
                Required(file, "resistance.compactionWeight", resistance.CompactionWeight),
                Required(file, "resistance.clayWeight", resistance.ClayWeight),
                Required(file, "resistance.clayReference", resistance.ClayReference),
                Required(file, "resistance.rootWeight", resistance.RootWeight),
                Required(file, "resistance.wetLoss", resistance.WetLoss),
                Required(file, "perOver.footholes", perOver.Footholes),
                Required(file, "perOver.heavyFootedExtra", perOver.HeavyFootedExtra),
                Required(file, "perOver.rough", perOver.Rough),
                Required(file, "perOver.leftArmExtra", perOver.LeftArmExtra),
                Required(file, "perOver.surface", perOver.Surface),
                Required(file, "perOver.dryDustExtra", perOver.DryDustExtra),
                Required(file, "perOver.coverLoss", perOver.CoverLoss),
                Required(file, "wetPlay", dto.WetPlay),
                Required(file, "cracks.startDryness", cracks.StartDryness),
                Required(file, "cracks.perHour", cracks.PerHour),
                Required(file, "cracks.damageExtra", cracks.DamageExtra),
                Required(file, "cracks.closePerHour", cracks.ClosePerHour),
                Required(file, "recovery.perDay", recovery.PerDay),
                Required(file, "recovery.repairedPerDay", recovery.RepairedPerDay),
                Required(file, "recovery.repairFills", recovery.RepairFills),
                Required(file, "duringMatch.cleanShare", duringMatch.CleanShare),
                Required(file, "duringMatch.fillShare", duringMatch.FillShare),
                Required(file, "runUps.neighbourShare", Required(file, "runUps", dto.RunUps).NeighbourShare),
                Required(file, "lasting.share", lasting.Share),
                Required(file, "lasting.resistanceLoss", lasting.ResistanceLoss),
                Required(file, "ends.lossPerWear", ends.LossPerWear),
                Required(file, "ends.establishDays", ends.EstablishDays),
                Required(file, "ends.unrepairedDays", ends.UnrepairedDays),
                Required(file, "ends.germinateShare", ends.GerminateShare),
                Required(file, "ends.resistanceLoss", ends.ResistanceLoss));
        }

        public static PitchSettings ParsePitch(string json)
        {
            const string file = "pitch";
            var dto = Deserialise<PitchDto>(json, file);
            var bounce = Required(file, "bounce", dto.Bounce);
            var consistency = Required(file, "consistency", dto.Consistency);
            var seam = Required(file, "seam", dto.Seam);
            var spin = Required(file, "spin", dto.Spin);

            return new PitchSettings(
                Required(file, "grassReferenceMm", dto.GrassReferenceMm),
                Required(file, "grassCushion", dto.GrassCushion),
                Required(file, "bounce.depthBase", bounce.DepthBase),
                Required(file, "bounce.clayReference", bounce.ClayReference),
                Required(file, "bounce.clayExponent", bounce.ClayExponent),
                Required(file, "consistency.damageWeight", consistency.DamageWeight),
                Required(file, "consistency.looseBelow", consistency.LooseBelow),
                Required(file, "consistency.looseWeight", consistency.LooseWeight),
                Required(file, "consistency.crackWeight", consistency.CrackWeight),
                Required(file, "consistency.footholeWeight", consistency.FootholeWeight),
                Required(file, "consistency.surfaceWearWeight", consistency.SurfaceWearWeight),
                Required(file, "consistency.lastingWeight", consistency.LastingWeight),
                Required(file, "consistency.endsWeight", consistency.EndsWeight),
                Required(file, "seam.base", seam.Base),
                Required(file, "seam.wetWeight", seam.WetWeight),
                Required(file, "spin.dryWeight", spin.DryWeight),
                Required(file, "spin.crackWeight", spin.CrackWeight),
                Required(file, "spin.grassWeight", spin.GrassWeight),
                Required(file, "spin.roughWeight", spin.RoughWeight),
                Required(file, "spin.surfaceWearWeight", spin.SurfaceWearWeight));
        }

        public static GrassSettings ParseGrass(string json)
        {
            const string file = "grass";
            var dto = Deserialise<GrassDto>(json, file);
            var starting = Required(file, "starting", dto.Starting);
            var temperature = Required(file, "temperature", dto.Temperature);
            var growth = Required(file, "growth", dto.Growth);
            var drought = Required(file, "drought", dto.Drought);
            var mowing = Required(file, "mowing", dto.Mowing);

            return new GrassSettings(
                Required(file, "starting.cover", starting.Cover),
                Required(file, "starting.heightMm", starting.HeightMm),
                Required(file, "starting.rootDepthMm", starting.RootDepthMm),
                Required(file, "temperature.min", temperature.Min),
                Required(file, "temperature.optimum", temperature.Optimum),
                Required(file, "temperature.max", temperature.Max),
                Required(file, "growth.heightPerDayMm", growth.HeightPerDayMm),
                Required(file, "growth.coverPerDay", growth.CoverPerDay),
                Required(file, "growth.maxCover", growth.MaxCover),
                Required(file, "growth.rootsPerDayMm", growth.RootsPerDayMm),
                Required(file, "growth.maxRootDepthMm", growth.MaxRootDepthMm),
                Required(file, "drought.waterBelow", drought.WaterBelow),
                Required(file, "drought.coverLossPerDay", drought.CoverLossPerDay),
                Required(file, "mowing.minHeightMm", mowing.MinHeightMm),
                Required(file, "mowing.maxHeightMm", mowing.MaxHeightMm),
                Required(file, "mowing.scalpShare", mowing.ScalpShare),
                Required(file, "mowing.scalpCoverLossPerMm", mowing.ScalpCoverLossPerMm));
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
                    fixture.Strip is { } strip ? new StripId(strip) : (StripId?)null,
                    opponent,
                    fixture.Televised ?? false);
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
                    Required(file, path + ".runsPerOver", format.RunsPerOver),
                    Required(file, path + ".wicketsPerOver", format.WicketsPerOver),
                    sessions,
                    Required(file, path + ".breakNames", format.BreakNames),
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

        private sealed class StakeholdersDto
        {
            public double? StartingSatisfaction { get; set; }
            public int? RequestDaysBeforeLock { get; set; }
            public CaptainDto? Captain { get; set; }
            public BoardDto? Board { get; set; }
            public RefereeDto? Referee { get; set; }
            public DeliveryDto? Delivery { get; set; }
            public ReviewDto? Review { get; set; }
        }

        private sealed class ReviewDto
        {
            public double? ContentFrom { get; set; }
            public double? DelightedFrom { get; set; }
            public double? UneasyFrom { get; set; }
            public ReviewWearDto? Wear { get; set; }
        }

        private sealed class ReviewWearDto
        {
            public double? Light { get; set; }
            public double? Worn { get; set; }
            public double? Heavy { get; set; }
        }

        private sealed class CaptainDto
        {
            public double? RequestChance { get; set; }
            public Dictionary<string, Dictionary<string, double>>? Characters { get; set; }
            public AnswersDto? Answers { get; set; }
            public double? HomeWin { get; set; }
            public double? HomeLoss { get; set; }
        }

        private sealed class BoardDto
        {
            public double? RequestChance { get; set; }
            public AnswersDto? Answers { get; set; }
            public double? DayFourReached { get; set; }
            public double? ShortFourDay { get; set; }
            public double? NoResult { get; set; }
            public double? TelevisedCentre { get; set; }
            public double? TelevisedOffCentre { get; set; }
            public double? PerDemerit { get; set; }
        }

        private sealed class RefereeDto
        {
            public double? VeryGood { get; set; }
            public double? Satisfactory { get; set; }
            public double? Unsatisfactory { get; set; }
            public double? Unfit { get; set; }
        }

        private sealed class AnswersDto
        {
            public double? Delivered { get; set; }
            public double? NotDelivered { get; set; }
            public double? Declined { get; set; }
            public double? Ignored { get; set; }
        }

        private sealed class DeliveryDto
        {
            public double? GreenSeamDayOne { get; set; }
            public double? TurningSpinLastDay { get; set; }
            public double? PaceCarry { get; set; }
            public double? TrueConsistency { get; set; }
            public double? FlatMovementBelow { get; set; }
        }

        private sealed class CalendarDto
        {
            public string? SeasonStart { get; set; }
            public string? SeasonEnd { get; set; }
            public int? MorningHour { get; set; }
            public int? AfternoonHour { get; set; }
            public int? OffSeasonStepDays { get; set; }
            public int? FinalPrepDays { get; set; }
            public int? AssignLockDaysOut { get; set; }
        }

        private sealed class GroundDto
        {
            public string? Name { get; set; }
            public List<string>? Ends { get; set; }
            public List<int>? CentreStrips { get; set; }
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
            public bool? Televised { get; set; }
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
            public double? RunsPerOver { get; set; }
            public double? WicketsPerOver { get; set; }
            public List<SessionDto>? Sessions { get; set; }
            public List<string>? BreakNames { get; set; }
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
            public RangeDto? RollingWindow { get; set; }
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
            public double? Mow { get; set; }
            public double? RepairEnds { get; set; }
            public double? CleanFootholes { get; set; }
            public double? FillFootholes { get; set; }
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

        private sealed class GrassDto
        {
            public GrassStartDto? Starting { get; set; }
            public GrassTemperatureDto? Temperature { get; set; }
            public GrassGrowthDto? Growth { get; set; }
            public GrassDroughtDto? Drought { get; set; }
            public MowingDto? Mowing { get; set; }
        }

        private sealed class GrassStartDto
        {
            public double? Cover { get; set; }
            public double? HeightMm { get; set; }
            public double? RootDepthMm { get; set; }
        }

        private sealed class GrassTemperatureDto
        {
            public double? Min { get; set; }
            public double? Optimum { get; set; }
            public double? Max { get; set; }
        }

        private sealed class GrassGrowthDto
        {
            public double? HeightPerDayMm { get; set; }
            public double? CoverPerDay { get; set; }
            public double? MaxCover { get; set; }
            public double? RootsPerDayMm { get; set; }
            public double? MaxRootDepthMm { get; set; }
        }

        private sealed class GrassDroughtDto
        {
            public double? WaterBelow { get; set; }
            public double? CoverLossPerDay { get; set; }
        }

        private sealed class MowingDto
        {
            public double? MinHeightMm { get; set; }
            public double? MaxHeightMm { get; set; }
            public double? ScalpShare { get; set; }
            public double? ScalpCoverLossPerMm { get; set; }
        }

        private sealed class RangeDto
        {
            public double? Min { get; set; }
            public double? Max { get; set; }
        }

        private sealed class RollersDto
        {
            public List<RollerDto>? Rollers { get; set; }
        }

        private sealed class RollerDto
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public double? CompactionPerHour { get; set; }
            public double? WetDamagePerHour { get; set; }
            public double? OveruseAbove { get; set; }
            public double? OveruseDamagePerHour { get; set; }
        }

        private sealed class CompactionDto
        {
            public CompactionStartDto? Starting { get; set; }
            public RangeDto? RollingMinutes { get; set; }
            public double? WetDamagePerPoint { get; set; }
            public HardnessDto? Hardness { get; set; }
        }

        private sealed class CompactionStartDto
        {
            public double? Compaction { get; set; }
            public double? StructureDamage { get; set; }
        }

        private sealed class HardnessDto
        {
            public double? DryWeight { get; set; }
            public double? ClayReference { get; set; }
            public double? ClayExponent { get; set; }
        }

        private sealed class PitchDto
        {
            public double? GrassReferenceMm { get; set; }
            public double? GrassCushion { get; set; }
            public BounceDto? Bounce { get; set; }
            public ConsistencyDto? Consistency { get; set; }
            public SeamDto? Seam { get; set; }
            public SpinDto? Spin { get; set; }
        }

        private sealed class BounceDto
        {
            public double? DepthBase { get; set; }
            public double? ClayReference { get; set; }
            public double? ClayExponent { get; set; }
        }

        private sealed class ConsistencyDto
        {
            public double? DamageWeight { get; set; }
            public double? LooseBelow { get; set; }
            public double? LooseWeight { get; set; }
            public double? CrackWeight { get; set; }
            public double? FootholeWeight { get; set; }
            public double? SurfaceWearWeight { get; set; }
            public double? LastingWeight { get; set; }
            public double? EndsWeight { get; set; }
        }

        private sealed class SeamDto
        {
            public double? Base { get; set; }
            public double? WetWeight { get; set; }
        }

        private sealed class SpinDto
        {
            public double? DryWeight { get; set; }
            public double? CrackWeight { get; set; }
            public double? GrassWeight { get; set; }
            public double? RoughWeight { get; set; }
            public double? SurfaceWearWeight { get; set; }
        }

        private sealed class WearDto
        {
            public ResistanceDto? Resistance { get; set; }
            public PerOverDto? PerOver { get; set; }
            public double? WetPlay { get; set; }
            public CracksDto? Cracks { get; set; }
            public RecoveryDto? Recovery { get; set; }
            public DuringMatchDto? DuringMatch { get; set; }
            public RunUpsDto? RunUps { get; set; }
            public LastingDto? Lasting { get; set; }
            public EndsDto? Ends { get; set; }
        }

        private sealed class LastingDto
        {
            public double? Share { get; set; }
            public double? ResistanceLoss { get; set; }
        }

        private sealed class EndsDto
        {
            public double? LossPerWear { get; set; }
            public double? EstablishDays { get; set; }
            public double? UnrepairedDays { get; set; }
            public double? GerminateShare { get; set; }
            public double? ResistanceLoss { get; set; }
        }

        private sealed class RunUpsDto
        {
            public double? NeighbourShare { get; set; }
        }

        private sealed class ResistanceDto
        {
            public double? CompactionWeight { get; set; }
            public double? ClayWeight { get; set; }
            public double? ClayReference { get; set; }
            public double? RootWeight { get; set; }
            public double? WetLoss { get; set; }
        }

        private sealed class PerOverDto
        {
            public double? Footholes { get; set; }
            public double? HeavyFootedExtra { get; set; }
            public double? Rough { get; set; }
            public double? LeftArmExtra { get; set; }
            public double? Surface { get; set; }
            public double? DryDustExtra { get; set; }
            public double? CoverLoss { get; set; }
        }

        private sealed class CracksDto
        {
            public double? StartDryness { get; set; }
            public double? PerHour { get; set; }
            public double? DamageExtra { get; set; }
            public double? ClosePerHour { get; set; }
        }

        private sealed class RecoveryDto
        {
            public double? PerDay { get; set; }
            public double? RepairedPerDay { get; set; }
            public double? RepairFills { get; set; }
        }

        private sealed class MatchDto
        {
            public double? StrengthScale { get; set; }
            public MatchWicketsDto? Wickets { get; set; }
            public MatchRunsDto? Runs { get; set; }
            public DeclarationDto? Declaration { get; set; }
            public double? RainRestartLoss { get; set; }
            public double? TossBowlFirstSeamAbove { get; set; }
        }

        private sealed class MatchWicketsDto
        {
            public double? Seam { get; set; }
            public double? Spin { get; set; }
            public double? Uneven { get; set; }
            public double? Dead { get; set; }
        }

        private sealed class MatchRunsDto
        {
            public double? Carry { get; set; }
            public double? Uneven { get; set; }
            public double? Dead { get; set; }
            public double? Spread { get; set; }
        }

        private sealed class DeclarationDto
        {
            public double? Lead { get; set; }
            public int? FromDay { get; set; }
        }

        private sealed class DuringMatchDto
        {
            public double? CleanShare { get; set; }
            public double? FillShare { get; set; }
        }

        private sealed class CommentaryDto
        {
            public Dictionary<string, CommentaryEventDto>? Events { get; set; }
            public Dictionary<string, string>? Causes { get; set; }
            public string? Rain { get; set; }
            public string? Session { get; set; }
        }

        private sealed class CommentaryEventDto
        {
            public double? Threshold { get; set; }
            public string? Text { get; set; }
        }

        private sealed class RatingDto
        {
            public double? UnfitBelow { get; set; }
            public double? AbandonBelow { get; set; }
            public UnsatisfactoryDto? Unsatisfactory { get; set; }
            public VeryGoodDto? VeryGood { get; set; }
            public DemeritsDto? Demerits { get; set; }
            public Dictionary<string, string>? Reasons { get; set; }
        }

        private sealed class UnsatisfactoryDto
        {
            public double? ConsistencyBelow { get; set; }
            public double? CarryBelow { get; set; }
            public double? BowlersOversShare { get; set; }
            public double? BowlersRunsPerWicket { get; set; }
            public double? LimitedParShare { get; set; }
            public double? LifelessRunsPerWicket { get; set; }
            public double? LifelessMovementBelow { get; set; }
        }

        private sealed class VeryGoodDto
        {
            public double? ConsistencyAtLeast { get; set; }
            public double? CarryAtLeast { get; set; }
            public double? MovementAtLeast { get; set; }
        }

        private sealed class DemeritsDto
        {
            public int? Unsatisfactory { get; set; }
            public int? Unfit { get; set; }
            public int? WindowYears { get; set; }
            public int? BanAt { get; set; }
        }
    }
}
