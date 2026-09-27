using System.Collections.Generic;
using System.Linq;
using Groundsman.Core.Commands;
using Groundsman.Core.Simulation;
using Groundsman.Core.Strips;
using Groundsman.Core.Tasks;
using Groundsman.Core.Time;

namespace Groundsman.Core
{
    public sealed class Game : IGame
    {
        private readonly PaceRules _pace;
        private readonly TasksSystem _tasks;
        private readonly HourlyTick _tick;
        private GameTime _now;

        public Game(GameSetup setup)
            : this(setup, Enumerable.Empty<IHourlySystem>())
        {
        }

        internal Game(GameSetup setup, IEnumerable<IHourlySystem> extraSystems)
        {
            var content = setup.Content;
            _pace = new PaceRules(new PaceContext(content.Calendar, setup.MatchDays));
            Square = new Square(content.Ground);
            _tasks = new TasksSystem(Square, content.Tasks);
            _tick = new HourlyTick(new IHourlySystem[] { _tasks }.Concat(extraSystems));
            _now = setup.Start;
        }

        public GameView View => new GameView(_now);

        internal Square Square { get; }

        public CommandResult Submit(IGameCommand command)
        {
            switch (command)
            {
                case WaterStrip water:
                    return Water(water);
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
            return new AdvanceResult(from, to);
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
    }
}
