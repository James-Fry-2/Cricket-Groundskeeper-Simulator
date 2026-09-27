using System.Collections.Generic;
using Groundsman.Core.Simulation;
using Groundsman.Core.Time;

namespace Groundsman.Core
{
    public sealed class Game : IGame
    {
        private readonly PaceRules _pace;
        private readonly HourlyTick _tick;
        private GameTime _now;

        public Game(PaceContext pace, GameTime start, IEnumerable<IHourlySystem> systems)
        {
            _pace = new PaceRules(pace);
            _tick = new HourlyTick(systems);
            _now = start;
        }

        public GameView View => new GameView(_now);

        public CommandResult Submit(IGameCommand command) => CommandResult.Rejected("No commands yet.");

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
    }
}
