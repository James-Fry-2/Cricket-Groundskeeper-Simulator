using Groundsman.Core.Covers;
using Groundsman.Core.Simulation;
using Groundsman.Core.Strips;
using Groundsman.Core.Tasks;
using Groundsman.Core.Time;
using Groundsman.Core.Weather;

namespace Groundsman.Core.Moisture
{
    internal sealed class MoistureSystem : IHourlySystem
    {
        private readonly Square _square;
        private readonly WeatherSystem _weather;
        private readonly CoversSystem _covers;
        private readonly TasksSystem _tasks;
        private readonly MoistureModel _model;
        private readonly double _waterMm;

        public MoistureSystem(Square square, WeatherSystem weather, CoversSystem covers, TasksSystem tasks, MoistureModel model, double waterMm)
        {
            _square = square;
            _weather = weather;
            _covers = covers;
            _tasks = tasks;
            _model = model;
            _waterMm = waterMm;
        }

        public TickStep Step => TickStep.Moisture;

        public void RunHour(GameTime hour)
        {
            // The Weather step has already run this hour, so LastHour is this hour's weather.
            var weather = _weather.LastHour!.Value;
            foreach (var strip in _square.Strips)
            {
                var watering = _tasks.TakeWatering(strip.Id) ? _waterMm : 0;
                _model.RunHour(strip, weather, _covers.IsCovered(strip.Id), watering);
            }
        }
    }
}
