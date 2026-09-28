using System.Collections.Generic;
using System.Linq;
using Groundsman.Core.Commands;
using Groundsman.Core.Content;
using Groundsman.Core.Covers;
using Groundsman.Core.Forecasting;
using Groundsman.Core.Grass;
using Groundsman.Core.Inspection;
using Groundsman.Core.Moisture;
using Groundsman.Core.Randomness;
using Groundsman.Core.Readings;
using Groundsman.Core.Simulation;
using Groundsman.Core.Staff;
using Groundsman.Core.Strips;
using Groundsman.Core.Tasks;
using Groundsman.Core.Time;
using Groundsman.Core.Weather;

namespace Groundsman.Core
{
    public sealed class Game : IGame
    {
        private readonly string _groundName;
        private readonly PaceContext _paceContext;
        private readonly PaceRules _pace;
        private readonly IReadOnlyList<Fixture> _fixtures;
        private readonly TasksSystem _tasks;
        private readonly CoversSystem _covers;
        private readonly int _coversOwned;
        private readonly HourlyTick _tick;
        private readonly KnowledgeStore _knowledge;
        private readonly ReadingTaker _readingTaker;
        private readonly StaffSettings _staffSettings;
        private readonly StaffRoster _staff;
        private readonly Forecaster _forecaster;
        private readonly double _waterMm;
        private readonly GrassSettings _grassSettings;
        private readonly MowRecord?[] _lastMown;
        private readonly List<(StripId Strip, Quantity Quantity)> _readThisTurn = new List<(StripId, Quantity)>();
        private GameTime _now;

        public Game(GameSetup setup)
            : this(setup, Enumerable.Empty<IHourlySystem>())
        {
        }

        internal Game(GameSetup setup, IEnumerable<IHourlySystem> extraSystems)
        {
            var content = setup.Content;
            var random = new RandomStreams(setup.Seed);

            _groundName = content.Ground.Name;
            _fixtures = setup.Fixtures;
            _paceContext = new PaceContext(content.Calendar, setup.Fixtures);
            _pace = new PaceRules(_paceContext);
            Square = new Square(content);
            Weather = new WeatherSystem(new WeatherGenerator(content.Climate, random.Get(RandomStream.Weather)), setup.Start.Date);
            _tasks = new TasksSystem();
            _coversOwned = content.Covers.Count;
            _covers = new CoversSystem(content.Covers.Count, Square.Strips.Count);
            Moisture = new MoistureSystem(Square, Weather, _covers, _tasks, new MoistureModel(content.Moisture, content.Covers), content.Tasks.WaterMm);
            _grassSettings = content.Grass;
            Grass = new GrassSystem(Square, Weather, _tasks, new GrassModel(content.Grass));
            _lastMown = new MowRecord?[Square.Strips.Count];
            _tick = new HourlyTick(new IHourlySystem[] { Weather, _covers, Moisture, Grass, _tasks }.Concat(extraSystems));
            _knowledge = new KnowledgeStore(content.Readings, Square.Strips.Count);
            _readingTaker = new ReadingTaker(content.Readings, random.Get(RandomStream.Readings));
            _staffSettings = content.Staff;
            _waterMm = content.Tasks.WaterMm;
            _staff = new StaffRoster(content.Staff, setup.Start.Date);
            _forecaster = new Forecaster(content.Forecast, content.Climate, Weather, random.Get(RandomStream.Forecast));
            _now = setup.Start;
            _forecaster.IssueIfNewDay(_now.Date);
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
                        _tasks.IsMowingQueued(id));
                }
                var staff = _staffSettings.Members
                    .Select(m => new StaffView(m.Id, m.Name, m.HoursPerDay, _staff.HoursLeft(m.Id)))
                    .ToArray();
                return new GameView(_now, _paceContext.PaceOn(_now.Date), _paceContext.NextMatchDayFrom(_now.Date), _fixtures.FirstOrDefault(f => f.End >= _now.Date), Observe(), _groundName, strips, _covers.Free, _coversOwned, staff, _forecaster.Current);
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
                strips[i] = new StripTruth(strip.Id, strip.SurfaceMoisture, strip.SubsurfaceMoisture, strip.GrassCover, strip.GrassHeightMm, strip.RootDepthMm);
            }
            return new TruthSnapshot(_now, Weather.LastHour, strips);
        }

        internal WeatherSystem Weather { get; }

        internal MoistureSystem Moisture { get; }

        internal GrassSystem Grass { get; }

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
                default:
                    return CommandResult.Rejected($"Unknown command: {command.GetType().Name}.");
            }
        }

        public AdvanceResult Advance()
        {
            var from = _now;
            var to = _pace.NextDecisionPoint(from);

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
                ObserveRain();
            }

            _now = to;
            _readThisTurn.Clear();
            _staff.StartDay(_now.Date);
            _forecaster.IssueIfNewDay(_now.Date);
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
