using Groundsman.Core.Simulation;
using Groundsman.Core.Strips;
using Groundsman.Core.Tasks;
using Groundsman.Core.Time;
using Groundsman.Core.Weather;

namespace Groundsman.Core.Grass
{
    internal sealed class GrassSystem : IHourlySystem
    {
        private readonly Square _square;
        private readonly WeatherSystem _weather;
        private readonly TasksSystem _tasks;
        private readonly GrassModel _model;

        public GrassSystem(Square square, WeatherSystem weather, TasksSystem tasks, GrassModel model)
        {
            _square = square;
            _weather = weather;
            _tasks = tasks;
            _model = model;
        }

        public TickStep Step => TickStep.Grass;

        public void RunHour(GameTime hour)
        {
            var weather = _weather.LastHour!.Value;
            foreach (var strip in _square.Strips)
            {
                if (_tasks.TakeMowing(strip.Id) is { } height)
                {
                    _model.Mow(strip, height);
                }
                _model.RunHour(strip, weather);
            }
        }
    }
}
