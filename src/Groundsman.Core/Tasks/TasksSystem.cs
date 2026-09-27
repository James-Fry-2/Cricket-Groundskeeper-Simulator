using System;
using System.Collections.Generic;
using Groundsman.Core.Content;
using Groundsman.Core.Simulation;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Core.Tasks
{
    /// <summary>
    /// Jobs ordered during a turn wait here and are carried out in the Tasks step of the next
    /// hour, so every change to true state happens inside the hourly tick.
    /// </summary>
    internal sealed class TasksSystem : IHourlySystem
    {
        private readonly Square _square;
        private readonly TaskSettings _settings;
        private readonly List<StripId> _waterQueue = new List<StripId>();

        public TasksSystem(Square square, TaskSettings settings)
        {
            _square = square;
            _settings = settings;
        }

        public TickStep Step => TickStep.Tasks;

        public bool IsWateringQueued(StripId strip) => _waterQueue.Contains(strip);

        public void QueueWatering(StripId strip) => _waterQueue.Add(strip);

        public void RunHour(GameTime hour)
        {
            foreach (var id in _waterQueue)
            {
                var strip = _square.Get(id);
                strip.SurfaceMoisture = Math.Min(_square.Saturation, strip.SurfaceMoisture + _settings.WaterSurfaceGain);
            }
            _waterQueue.Clear();
        }
    }
}
