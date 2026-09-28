using System.Collections.Generic;
using Groundsman.Core.Simulation;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Core.Tasks
{
    /// <summary>
    /// Jobs ordered during a turn. Each job is carried out by the system it affects, in its
    /// own step of the next hour, so every change to true state happens inside the tick:
    /// watering is an input to the Moisture step and mowing to the Grass step.
    /// </summary>
    internal sealed class TasksSystem : IHourlySystem
    {
        private readonly List<StripId> _waterQueue = new List<StripId>();
        private readonly List<(StripId Strip, double HeightMm)> _mowQueue = new List<(StripId, double)>();

        public TickStep Step => TickStep.Tasks;

        public bool IsWateringQueued(StripId strip) => _waterQueue.Contains(strip);

        public void QueueWatering(StripId strip) => _waterQueue.Add(strip);

        /// <summary>True, and removes the order, if the strip was down for watering.</summary>
        public bool TakeWatering(StripId strip) => _waterQueue.Remove(strip);

        public bool IsMowingQueued(StripId strip) => _mowQueue.Exists(m => m.Strip == strip);

        public void QueueMowing(StripId strip, double heightMm) => _mowQueue.Add((strip, heightMm));

        /// <summary>The height to cut to, and removes the order, if the strip was down for mowing.</summary>
        public double? TakeMowing(StripId strip)
        {
            var index = _mowQueue.FindIndex(m => m.Strip == strip);
            if (index < 0)
            {
                return null;
            }
            var height = _mowQueue[index].HeightMm;
            _mowQueue.RemoveAt(index);
            return height;
        }

        public void RunHour(GameTime hour)
        {
        }
    }
}
