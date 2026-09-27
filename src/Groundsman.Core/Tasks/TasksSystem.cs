using System.Collections.Generic;
using Groundsman.Core.Simulation;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Core.Tasks
{
    /// <summary>
    /// Jobs ordered during a turn. Each job is carried out by the system it affects, in its
    /// own step of the next hour, so every change to true state happens inside the tick:
    /// watering is an input to the Moisture step.
    /// </summary>
    internal sealed class TasksSystem : IHourlySystem
    {
        private readonly List<StripId> _waterQueue = new List<StripId>();

        public TickStep Step => TickStep.Tasks;

        public bool IsWateringQueued(StripId strip) => _waterQueue.Contains(strip);

        public void QueueWatering(StripId strip) => _waterQueue.Add(strip);

        /// <summary>True, and removes the order, if the strip was down for watering.</summary>
        public bool TakeWatering(StripId strip) => _waterQueue.Remove(strip);

        public void RunHour(GameTime hour)
        {
        }
    }
}
