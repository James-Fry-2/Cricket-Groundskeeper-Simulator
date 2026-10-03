using System.Collections.Generic;
using System.Linq;
using Groundsman.Core.Commands;
using Groundsman.Core.Compaction;
using Groundsman.Core.Content;
using Groundsman.Core.Covers;
using Groundsman.Core.Forecasting;
using Groundsman.Core.Grass;
using Groundsman.Core.Inspection;
using Groundsman.Core.Match;
using Groundsman.Core.Moisture;
using Groundsman.Core.Pitch;
using Groundsman.Core.Pressures;
using Groundsman.Core.Randomness;
using Groundsman.Core.Readings;
using Groundsman.Core.Simulation;
using Groundsman.Core.Staff;
using Groundsman.Core.Strips;
using Groundsman.Core.Tasks;
using Groundsman.Core.Time;
using Groundsman.Core.Wear;
using Groundsman.Core.Weather;

namespace Groundsman.Core
{
    public sealed class Game : IGame
    {
        private readonly string _groundName;
        private readonly PaceContext _paceContext;
        private readonly PaceRules _pace;
        private readonly IReadOnlyList<Fixture> _fixtures;
        private readonly Schedule.StripBook _book;
        private readonly Pressures.StakeholderBook _stakeholders;
        private readonly TasksSystem _tasks;
        private readonly CoversSystem _covers;
        private readonly int _coversOwned;
        private readonly HourlyTick _tick;
        private readonly IHourlySystem[] _observers;
        private readonly KnowledgeStore _knowledge;
        private readonly ReadingTaker _readingTaker;
        private readonly StaffSettings _staffSettings;
        private readonly StaffRoster _staff;
        private readonly Forecaster _forecaster;
        private readonly double _waterMm;
        private readonly GrassSettings _grassSettings;
        private readonly MowRecord?[] _lastMown;
        private readonly RollRecord?[] _lastRolled;
        private readonly GameContent _content;
        private readonly RollingModel _rolling;
        private readonly PitchModel _pitch;
        private readonly WearSystem _wear;
        private readonly GameTime?[] _lastRepaired;
        private readonly EndsLook?[] _endsLook;
        private readonly List<(StripId Strip, Quantity Quantity)> _readThisTurn = new List<(StripId, Quantity)>();
        private GameTime _now;

        public Game(GameSetup setup)
            : this(setup, Enumerable.Empty<IHourlySystem>())
        {
        }

        /// <param name="observers">Test hooks run after every hour's steps; they take no step of their own.</param>
        internal Game(GameSetup setup, IEnumerable<IHourlySystem> observers)
        {
            _observers = observers.ToArray();
            var content = setup.Content;
            var random = new RandomStreams(setup.Seed);

            _groundName = content.Ground.Name;
            _fixtures = setup.Fixtures;
            _book = new Schedule.StripBook(setup.Fixtures, content.Calendar.AssignLockDaysOut, content.Ground.Strips.Count);
            _paceContext = new PaceContext(content.Calendar, setup.Fixtures);
            _pace = new PaceRules(_paceContext);
            Square = new Square(content);
            Weather = new WeatherSystem(new WeatherGenerator(content.Climate, random.Get(RandomStream.Weather)), setup.Start.Date);
            _content = content;
            _rolling = new RollingModel(content.Compaction);
            _pitch = new PitchModel(content.Pitch, _rolling);
            _tasks = new TasksSystem(Square, _rolling);
            _coversOwned = content.Covers.Count;
            _covers = new CoversSystem(content.Covers.Count, Square.Strips.Count);
            Moisture = new MoistureSystem(Square, Weather, _covers, _tasks, new MoistureModel(content.Moisture, content.Covers), content.Tasks.WaterMm);
            _grassSettings = content.Grass;
            var grassModel = new GrassModel(content.Grass);
            Grass = new GrassSystem(Square, Weather, _tasks, grassModel);
            WearModel = new WearModel(content.Wear, content.Grass);
            _wear = new WearSystem(Square, Weather, _tasks, grassModel, WearModel);
            _lastRepaired = new GameTime?[Square.Strips.Count];
            _endsLook = new EndsLook?[Square.Strips.Count];
            _lastMown = new MowRecord?[Square.Strips.Count];
            _lastRolled = new RollRecord?[Square.Strips.Count];
            Matches = new MatchSystem(
                setup.Fixtures,
                _book,
                content.HomeTeam,
                content.Match,
                Square,
                Weather,
                _pitch,
                WearModel,
                random.Get(RandomStream.Match),
                new Commentator(content.Commentary, content.Ground.Ends),
                new Referee(content.Rating, content.Commentary),
                content.Rating,
                new DemeritLedger(content.Rating));
            _covers.Override = Matches.CoverOverride;
            _covers.At(setup.Start);
            _tick = new HourlyTick(new IHourlySystem[] { Weather, _covers, Moisture, Grass, _tasks, Matches, _wear });
            _knowledge = new KnowledgeStore(content.Readings, Square.Strips.Count);
            _readingTaker = new ReadingTaker(content.Readings, random.Get(RandomStream.Readings));
            _staffSettings = content.Staff;
            _waterMm = content.Tasks.WaterMm;
            _staff = new StaffRoster(content.Staff, setup.Start.Date);
            _forecaster = new Forecaster(content.Forecast, content.Climate, Weather, random.Get(RandomStream.Forecast));
            _stakeholders = new Pressures.StakeholderBook(content.Stakeholders, setup.Fixtures, _book, random.Get(RandomStream.Events));
            _now = setup.Start;
            _forecaster.IssueIfNewDay(_now.Date);
            _book.LockDue(_now.Date);
            _stakeholders.Update(_now.Date);
        }

        public GameView View
        {
            get
            {
                var strips = new StripView[Square.Strips.Count];
                for (var i = 0; i < strips.Length; i++)
                {
                    var id = Square.Strips[i].Id;
                    strips[i] = new StripView(
                        id,
                        _knowledge.LatestSurfaceMoisture(id),
                        _knowledge.CurrentSurfaceMoisture(id, _now),
                        _knowledge.LatestSubsurfaceMoisture(id),
                        _knowledge.CurrentSubsurfaceMoisture(id, _now),
                        _tasks.IsWateringQueued(id),
                        _covers.IsCovered(id),
                        _covers.OrderFor(id),
                        _lastMown[i],
                        _tasks.IsMowingQueued(id),
                        _lastRolled[i],
                        _tasks.IsRollingQueued(id),
                        _lastRepaired[i],
                        _tasks.IsRepairQueued(id),
                        _content.Ground.IsCentre(id),
                        _endsLook[i],
                        Matches.Played.LastOrDefault(m => m.Strip == id)?.Fixture);
                }
                var staff = _staffSettings.Members
                    .Select(m => new StaffView(m.Id, m.Name, m.HoursPerDay, _staff.HoursLeft(m.Id)))
                    .ToArray();
                return new GameView(_now, _paceContext.PaceOn(_now.Date), _paceContext.NextMatchDayFrom(_now.Date), _fixtures.Select(_book.View).ToArray(), _fixtures.Where(f => f.End >= _now.Date).Select(_book.View).FirstOrDefault(), Observe(), _groundName, strips, _covers.Free, _coversOwned, staff, _forecaster.Current, _content.Rollers, Matches.Played.Select(m => m.ToView()).ToArray(), Interval(), Matches.Ledger.Active(_now.Date), Matches.Ledger.Banned(_now.Date), _book.Notices.Concat(_stakeholders.Notices).ToArray(), _stakeholders.Requests, _stakeholders.Stakeholders, Review());
            }
        }

        internal Square Square { get; }

        /// <summary>True state, for the harness, replays and the debug view only.</summary>
        public TruthSnapshot Inspect()
        {
            var strips = new StripTruth[Square.Strips.Count];
            for (var i = 0; i < strips.Length; i++)
            {
                var strip = Square.Strips[i];
                strips[i] = new StripTruth(strip.Id, strip.SurfaceMoisture, strip.SubsurfaceMoisture, strip.GrassCover, strip.GrassHeightMm, strip.RootDepthMm, strip.Compaction, strip.StructureDamage, _rolling.Hardness(strip), _pitch.Characterise(strip), strip.Footholes, strip.Rough, strip.SurfaceWear, strip.Cracks, strip.LastingWear, strip.EndsEstablishment);
            }
            return new TruthSnapshot(_now, Weather.LastHour, strips);
        }

        internal WeatherSystem Weather { get; }

        internal MoistureSystem Moisture { get; }

        internal GrassSystem Grass { get; }

        internal WearModel WearModel { get; }

        internal MatchSystem Matches { get; }

        internal CoversSystem Covers => _covers;

        internal WearSystem Wear => _wear;

        public CommandResult Submit(IGameCommand command)
        {
            switch (command)
            {
                case WaterStrip water:
                    return Water(water);
                case TakeReading reading:
                    return Read(reading);
                case CoverStrip cover:
                    return Cover(cover);
                case UncoverStrip uncover:
                    return Uncover(uncover);
                case MowStrip mow:
                    return Mow(mow);
                case RollStrip roll:
                    return Roll(roll);
                case RepairEnds repair:
                    return Repair(repair);
                case CleanFootholes clean:
                    return FootholeJob(clean.Strip, clean.By, Tasks.FootholeJob.Clean);
                case FillFootholes fill:
                    return FootholeJob(fill.Strip, fill.By, Tasks.FootholeJob.Fill);
                case AssignStrip assign:
                    return _book.Assign(assign.FixtureId, assign.Strip);
                case AnswerRequest answer:
                    return _stakeholders.Answer(answer.RequestId, answer.Accept, _now.Date);
                default:
                    return CommandResult.Rejected($"Unknown command: {command.GetType().Name}.");
            }
        }

        public AdvanceResult Advance()
        {
            var from = _now;
            var to = _pace.NextDecisionPoint(from);
            _book.ClearNotices();
            _stakeholders.ClearNotices();

            foreach (var strip in Square.Strips)
            {
                if (_tasks.IsWateringQueued(strip.Id))
                {
                    _knowledge.AddWater(strip.Id, _waterMm);
                }
            }

            for (var hour = from; hour < to; hour = hour.AddHours(1))
            {
                _tick.RunHour(hour);
                foreach (var observer in _observers)
                {
                    observer.RunHour(hour);
                }
                ObserveRain();
            }

            _now = to;
            _covers.At(_now);
            _readThisTurn.Clear();
            _staff.StartDay(_now.Date);
            _forecaster.IssueIfNewDay(_now.Date);
            _book.LockDue(_now.Date);
            _stakeholders.Update(_now.Date);
            _stakeholders.Judge(Matches.Played, _content.Ground.IsCentre, _content.HomeTeam.Name, _now.Date);
            return new AdvanceResult(from, to);
        }

        // The rain gauge and the cover state are both known, so the player knows how much rain
        // each open strip has taken; covered strips took none.
        private void ObserveRain()
        {
            var rain = Weather.LastHour!.Value.RainMm;
            if (rain <= 0)
            {
                return;
            }

            foreach (var strip in Square.Strips)
            {
                if (!_covers.IsCovered(strip.Id))
                {
                    _knowledge.AddWater(strip.Id, rain);
                }
            }
        }

        private WeatherObservation? Observe()
        {
            if (!(Weather.LastHour is { } lastHour))
            {
                return null;
            }

            var yesterday = _now.Date.AddDays(-1);
            var day = yesterday >= Weather.FirstDate ? Weather.Day(yesterday) : null;
            return new WeatherObservation(Weather.RainLast24HoursMm, lastHour.Temperature, lastHour.WindKph, day?.MinTemperature, day?.MaxTemperature);
        }

        private CommandResult Water(WaterStrip water)
        {
            if (!Square.Contains(water.Strip))
            {
                return CommandResult.Rejected($"{water.Strip} isn't on this square.");
            }
            if (_tasks.IsWateringQueued(water.Strip))
            {
                return CommandResult.Rejected($"{water.Strip} is already down for watering.");
            }
            if (Assign(water.By, _staffSettings.WaterHours, out var who) is { } refused)
            {
                return refused;
            }

            _tasks.QueueWatering(water.Strip);
            _staff.Spend(who, _staffSettings.WaterHours);
            return CommandResult.Ok();
        }

        private CommandResult Repair(RepairEnds repair)
        {
            if (!Square.Contains(repair.Strip))
            {
                return CommandResult.Rejected($"{repair.Strip} isn't on this square.");
            }
            if (_tasks.IsRepairQueued(repair.Strip))
            {
                return CommandResult.Rejected($"{repair.Strip} is already down for end repairs.");
            }
            if (StripInPlay() == repair.Strip)
            {
                return CommandResult.Rejected($"Law 9: {repair.Strip}'s ends can't be repaired until the match is over. Clean the footholes at a break, or fill them at close of play in a multi-day match.");
            }
            if (Assign(repair.By, _staffSettings.RepairEndsHours, out var who) is { } refused)
            {
                return refused;
            }

            _tasks.QueueRepair(repair.Strip);
            _lastRepaired[repair.Strip.Number - 1] = _now;
            _staff.Spend(who, _staffSettings.RepairEndsHours);
            return CommandResult.Ok();
        }

        /// <summary>Null until every fixture has been played and settled with the stakeholders.</summary>
        private SeasonReview? Review()
        {
            if (_fixtures.Count == 0 || _stakeholders.Judged < _fixtures.Count)
            {
                return null;
            }

            var matches = Matches.Played;
            int Graded(PitchGrade grade) => matches.Count(m => m.Rating?.Grade == grade);
            int Results(System.Func<ResultView, bool> test) => matches.Count(m => m.Result != null && test(m.Result));
            var home = _content.HomeTeam.Name;
            var square = Square.Strips
                .Select(s => new StripReview(s.Id, matches.Count(m => m.Strip == s.Id), _content.Stakeholders.WearFor(s.LastingWear)))
                .ToArray();

            return new SeasonReview(
                _stakeholders.Review(),
                Graded(PitchGrade.VeryGood),
                Graded(PitchGrade.Satisfactory),
                Graded(PitchGrade.Unsatisfactory),
                Graded(PitchGrade.Unfit),
                Results(r => r.Kind == ResultKind.Win && r.Winner == home),
                Results(r => r.Kind == ResultKind.Win && r.Winner != home),
                Results(r => r.Kind == ResultKind.Draw || r.Kind == ResultKind.Tie),
                Results(r => r.Kind == ResultKind.NoResult),
                Matches.Ledger.Active(_now.Date),
                Matches.Ledger.Banned(_now.Date),
                square);
        }

        /// <summary>The strip of the fixture on today, if any.</summary>
        private StripId? StripInPlay() => Matches.FixtureOn(_now.Date) is { } fixture ? _book.StripFor(fixture) : (StripId?)null;

        private bool CanFill(Fixture fixture)
        {
            var closeOfPlay = fixture.Format.DecisionHours[fixture.Format.DecisionHours.Count - 1];
            return fixture.Days >= 2 && _now.Hour == closeOfPlay && _now.Date < fixture.End;
        }

        private IntervalView? Interval()
        {
            var fixture = Matches.FixtureOn(_now.Date);
            if (fixture == null || (Matches.Latest is { } match && match.Fixture == fixture && match.Finished))
            {
                return null;
            }

            var name = "Before play";
            var sessions = fixture.Format.Sessions;
            for (var i = 0; i < sessions.Count; i++)
            {
                if (sessions[i].End <= _now.Hour)
                {
                    name = fixture.Format.BreakNames[i];
                }
            }
            return new IntervalView(name, _book.StripFor(fixture), canClean: true, CanFill(fixture));
        }

        private CommandResult FootholeJob(StripId strip, StaffId? by, FootholeJob job)
        {
            var fixture = Matches.FixtureOn(_now.Date);
            if (fixture == null || _book.StripFor(fixture) != strip)
            {
                return CommandResult.Rejected($"Footholes are cleaned and filled during a match, on the match strip. {strip} isn't being played on.");
            }
            if (job == Tasks.FootholeJob.Fill)
            {
                if (!CanFill(fixture))
                {
                    return CommandResult.Rejected("Law 9: footholes can only be filled at close of play on a day with more cricket to come in a match over one day.");
                }
            }
            if (_tasks.IsFootholeJobQueued(strip, job))
            {
                return CommandResult.Rejected($"That's already ordered for {strip}.");
            }
            var hours = job == Tasks.FootholeJob.Clean ? _staffSettings.CleanFootholesHours : _staffSettings.FillFootholesHours;
            if (Assign(by, hours, out var who) is { } refused)
            {
                return refused;
            }

            _tasks.QueueFootholeJob(strip, job);
            _staff.Spend(who, hours);
            return CommandResult.Ok();
        }

        private CommandResult Roll(RollStrip roll)
        {
            if (!Square.Contains(roll.Strip))
            {
                return CommandResult.Rejected($"{roll.Strip} isn't on this square.");
            }
            var roller = _content.Roller(roll.RollerId);
            if (roller == null)
            {
                return CommandResult.Rejected($"There's no \"{roll.RollerId}\" roller. The ground has: {string.Join(", ", _content.Rollers.Select(r => r.Id))}.");
            }
            var compaction = _content.Compaction;
            if (roll.Minutes < compaction.MinRollingMinutes || roll.Minutes > compaction.MaxRollingMinutes)
            {
                return CommandResult.Rejected($"Roll for {compaction.MinRollingMinutes:0} to {compaction.MaxRollingMinutes:0} minutes.");
            }
            if (_tasks.IsRollingQueued(roll.Strip))
            {
                return CommandResult.Rejected($"{roll.Strip} is already down for rolling.");
            }
            var hours = roll.Minutes / 60;
            if (Assign(roll.By, hours, out var who) is { } refused)
            {
                return refused;
            }

            _tasks.QueueRolling(roll.Strip, roller, roll.Minutes);
            _lastRolled[roll.Strip.Number - 1] = new RollRecord(roller.Id, roll.Minutes, _now);
            _staff.Spend(who, hours);
            return CommandResult.Ok();
        }

        private CommandResult Mow(MowStrip mow)
        {
            if (!Square.Contains(mow.Strip))
            {
                return CommandResult.Rejected($"{mow.Strip} isn't on this square.");
            }
            if (mow.HeightMm < _grassSettings.MinMowHeightMm || mow.HeightMm > _grassSettings.MaxMowHeightMm)
            {
                return CommandResult.Rejected($"The mower cuts from {_grassSettings.MinMowHeightMm:0.#} to {_grassSettings.MaxMowHeightMm:0.#} mm.");
            }
            if (_tasks.IsMowingQueued(mow.Strip))
            {
                return CommandResult.Rejected($"{mow.Strip} is already down for mowing.");
            }
            if (Assign(mow.By, _staffSettings.MowHours, out var who) is { } refused)
            {
                return refused;
            }

            _tasks.QueueMowing(mow.Strip, mow.HeightMm);
            _lastMown[mow.Strip.Number - 1] = new MowRecord(mow.HeightMm, _now);
            _staff.Spend(who, _staffSettings.MowHours);
            return CommandResult.Ok();
        }

        private CommandResult Cover(CoverStrip cover)
        {
            var strip = cover.Strip;
            if (StripInPlay() == strip)
            {
                return CommandResult.Rejected($"{strip} is being played on: the ground staff cover it under the playing conditions until the match is over.");
            }
            if (!Square.Contains(strip))
            {
                return CommandResult.Rejected($"{strip} isn't on this square.");
            }
            if (_covers.OrderFor(strip) != CoverOrder.None)
            {
                return CommandResult.Rejected($"{strip} already has a cover order this turn.");
            }
            if (_covers.IsCovered(strip))
            {
                return CommandResult.Rejected($"{strip} is already covered.");
            }
            if (_covers.Free == 0)
            {
                return CommandResult.Rejected("No covers free. Uncover another strip first.");
            }
            if (Assign(cover.By, _staffSettings.CoverHours, out var who) is { } refused)
            {
                return refused;
            }

            _covers.Order(strip, CoverOrder.Cover);
            _staff.Spend(who, _staffSettings.CoverHours);
            return CommandResult.Ok();
        }

        private CommandResult Uncover(UncoverStrip uncover)
        {
            var strip = uncover.Strip;
            if (StripInPlay() == strip)
            {
                return CommandResult.Rejected($"{strip} is being played on: the ground staff cover it under the playing conditions until the match is over.");
            }
            if (!Square.Contains(strip))
            {
                return CommandResult.Rejected($"{strip} isn't on this square.");
            }
            if (_covers.OrderFor(strip) != CoverOrder.None)
            {
                return CommandResult.Rejected($"{strip} already has a cover order this turn.");
            }
            if (!_covers.IsCovered(strip))
            {
                return CommandResult.Rejected($"{strip} isn't covered.");
            }
            if (Assign(uncover.By, _staffSettings.UncoverHours, out var who) is { } refused)
            {
                return refused;
            }

            _covers.Order(strip, CoverOrder.Uncover);
            _staff.Spend(who, _staffSettings.UncoverHours);
            return CommandResult.Ok();
        }

        private CommandResult Read(TakeReading reading)
        {
            if (!Square.Contains(reading.Strip))
            {
                return CommandResult.Rejected($"{reading.Strip} isn't on this square.");
            }

            // Repeat readings of an unchanged strip could be intersected to pin down the truth.
            var quantity = reading.Tool == ReadingSource.SoilCore ? Quantity.SubsurfaceMoisture : Quantity.SurfaceMoisture;
            if (_readThisTurn.Contains((reading.Strip, quantity)))
            {
                return CommandResult.Rejected(quantity == Quantity.SubsurfaceMoisture
                    ? $"{reading.Strip} has already been cored this turn."
                    : $"{reading.Strip} has already been read this turn.");
            }
            var hours = reading.Tool switch
            {
                ReadingSource.Feel => _staffSettings.FeelReadingHours,
                ReadingSource.SoilCore => _staffSettings.SoilCoreHours,
                _ => _staffSettings.ProbeReadingHours,
            };
            if (Assign(reading.By, hours, out var who) is { } refused)
            {
                return refused;
            }

            var strip = Square.Get(reading.Strip);
            var taker = _staff.Find(who)!;
            _knowledge.Record(reading.Tool switch
            {
                ReadingSource.Feel => _readingTaker.Feel(strip, _now, taker),
                ReadingSource.SoilCore => _readingTaker.SoilCore(strip, _now, taker),
                _ => _readingTaker.Probe(strip, _now, taker),
            });
            if (reading.Tool == ReadingSource.Feel)
            {
                _endsLook[reading.Strip.Number - 1] = new EndsLook(WearModel.EndsLook(strip), _now);
            }
            _readThisTurn.Add((reading.Strip, quantity));
            _staff.Spend(who, hours);
            return CommandResult.Ok();
        }

        /// <summary>
        /// Picks who does a job (the player unless named) and returns a refusal if they don't
        /// exist or haven't the hours left today; null if the job can go ahead.
        /// </summary>
        private CommandResult? Assign(StaffId? by, double hours, out StaffId who)
        {
            who = by ?? _staff.Player;
            var member = _staff.Find(who);
            if (member == null)
            {
                return CommandResult.Rejected($"No one called \"{who}\" works here.");
            }

            // A small tolerance so hours spent in quarters add up exactly to a full day.
            var left = _staff.HoursLeft(who);
            if (left + 1e-9 < hours)
            {
                return CommandResult.Rejected($"{member.Name} has {left:0.##} hours left today; that job takes {hours:0.##}.");
            }
            return null;
        }
    }
}
