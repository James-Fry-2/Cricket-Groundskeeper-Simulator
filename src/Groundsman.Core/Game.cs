using System.Collections.Generic;
using System.Linq;
using Groundsman.Core.Commands;
using Groundsman.Core.Inspection;
using Groundsman.Core.Randomness;
using Groundsman.Core.Readings;
using Groundsman.Core.Simulation;
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
        private readonly TasksSystem _tasks;
        private readonly HourlyTick _tick;
        private readonly KnowledgeStore _knowledge;
        private readonly ReadingTaker _readingTaker;
        private readonly List<StripId> _readThisTurn = new List<StripId>();
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
            _paceContext = new PaceContext(content.Calendar, setup.MatchDays);
            _pace = new PaceRules(_paceContext);
            Square = new Square(content.Ground);
            Weather = new WeatherSystem(new WeatherGenerator(content.Climate, random.Get(RandomStream.Weather)), setup.Start.Date);
            _tasks = new TasksSystem(Square, content.Tasks);
            _tick = new HourlyTick(new IHourlySystem[] { Weather, _tasks }.Concat(extraSystems));
            _knowledge = new KnowledgeStore(Square.Strips.Count);
            _readingTaker = new ReadingTaker(content.Readings, random.Get(RandomStream.Readings));
            _now = setup.Start;
        }

        public GameView View
        {
            get
            {
                var strips = new StripView[Square.Strips.Count];
                for (var i = 0; i < strips.Length; i++)
                {
                    var id = Square.Strips[i].Id;
                    strips[i] = new StripView(id, _knowledge.LatestSurfaceMoisture(id), _tasks.IsWateringQueued(id));
                }
                return new GameView(_now, _paceContext.PaceOn(_now.Date), _paceContext.NextMatchDayFrom(_now.Date), Observe(), _groundName, strips);
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
                strips[i] = new StripTruth(strip.Id, strip.SurfaceMoisture, strip.SubsurfaceMoisture);
            }
            return new TruthSnapshot(_now, Weather.LastHour, strips);
        }

        internal WeatherSystem Weather { get; }

        public CommandResult Submit(IGameCommand command)
        {
            switch (command)
            {
                case WaterStrip water:
                    return Water(water);
                case TakeReading reading:
                    return Read(reading);
                default:
                    return CommandResult.Rejected($"Unknown command: {command.GetType().Name}.");
            }
        }

        public AdvanceResult Advance()
        {
            var from = _now;
            var to = _pace.NextDecisionPoint(from);

            for (var hour = from; hour < to; hour = hour.AddHours(1))
            {
                _tick.RunHour(hour);
            }

            _now = to;
            _readThisTurn.Clear();
            return new AdvanceResult(from, to);
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

            _tasks.QueueWatering(water.Strip);
            return CommandResult.Ok();
        }

        private CommandResult Read(TakeReading reading)
        {
            if (!Square.Contains(reading.Strip))
            {
                return CommandResult.Rejected($"{reading.Strip} isn't on this square.");
            }

            // Repeat readings of an unchanged strip could be intersected to pin down the truth.
            if (_readThisTurn.Contains(reading.Strip))
            {
                return CommandResult.Rejected($"{reading.Strip} has already been read this turn.");
            }

            _knowledge.Record(_readingTaker.ProbeSurfaceMoisture(Square.Get(reading.Strip), _now));
            _readThisTurn.Add(reading.Strip);
            return CommandResult.Ok();
        }
    }
}
