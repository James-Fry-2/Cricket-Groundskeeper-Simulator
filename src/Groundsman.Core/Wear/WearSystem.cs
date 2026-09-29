using Groundsman.Core.Grass;
using Groundsman.Core.Simulation;
using Groundsman.Core.Strips;
using Groundsman.Core.Tasks;
using Groundsman.Core.Time;
using Groundsman.Core.Weather;

namespace Groundsman.Core.Wear
{
    internal sealed class WearSystem : IHourlySystem
    {
        private readonly Square _square;
        private readonly WeatherSystem _weather;
        private readonly TasksSystem _tasks;
        private readonly GrassModel _grass;
        private readonly WearModel _model;

        public WearSystem(Square square, WeatherSystem weather, TasksSystem tasks, GrassModel grass, WearModel model)
        {
            _square = square;
            _weather = weather;
            _tasks = tasks;
            _grass = grass;
            _model = model;
        }

        public TickStep Step => TickStep.WearAndRecovery;

        public void RunHour(GameTime hour)
        {
            var weather = _weather.LastHour!.Value;
            foreach (var strip in _square.Strips)
            {
                if (_tasks.TakeRepair(strip.Id))
                {
                    _model.Repair(strip);
                }
                if (_tasks.TakeFootholeJob(strip.Id, FootholeJob.Clean))
                {
                    _model.CleanFootholes(strip);
                }
                if (_tasks.TakeFootholeJob(strip.Id, FootholeJob.Fill))
                {
                    _model.FillFootholes(strip);
                }
                _model.RunHour(strip, _grass.GrowthFactor(strip, weather));
            }
        }
    }
}
