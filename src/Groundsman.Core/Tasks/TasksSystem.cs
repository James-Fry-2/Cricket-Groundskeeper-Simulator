using System.Collections.Generic;
using Groundsman.Core.Compaction;
using Groundsman.Core.Content;
using Groundsman.Core.Simulation;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Core.Tasks
{
    internal enum FootholeJob
    {
        Clean = 1,
        Fill = 2,
    }

    /// <summary>
    /// Jobs ordered during a turn. Each job is carried out by the system it affects, in its
    /// own step of the next hour, so every change to true state happens inside the tick:
    /// watering is an input to the Moisture step and mowing to the Grass step.
    /// </summary>
    internal sealed class TasksSystem : IHourlySystem
    {
        private readonly Square _square;
        private readonly RollingModel _rolling;
        private readonly List<StripId> _waterQueue = new List<StripId>();
        private readonly List<(StripId Strip, RollerSettings Roller, double Minutes)> _rollQueue = new List<(StripId, RollerSettings, double)>();

        public TasksSystem(Square square, RollingModel rolling)
        {
            _square = square;
            _rolling = rolling;
        }
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

        private readonly List<StripId> _repairQueue = new List<StripId>();
        private readonly List<(StripId Strip, FootholeJob Job)> _footholeQueue = new List<(StripId, FootholeJob)>();

        public bool IsFootholeJobQueued(StripId strip, FootholeJob job) => _footholeQueue.Contains((strip, job));

        public void QueueFootholeJob(StripId strip, FootholeJob job) => _footholeQueue.Add((strip, job));

        public bool TakeFootholeJob(StripId strip, FootholeJob job) => _footholeQueue.Remove((strip, job));

        public bool IsRepairQueued(StripId strip) => _repairQueue.Contains(strip);

        public void QueueRepair(StripId strip) => _repairQueue.Add(strip);

        /// <summary>True, and removes the order, if the strip was down for end repairs.</summary>
        public bool TakeRepair(StripId strip) => _repairQueue.Remove(strip);

        public bool IsRollingQueued(StripId strip) => _rollQueue.Exists(r => r.Strip == strip);

        public void QueueRolling(StripId strip, RollerSettings roller, double minutes) => _rollQueue.Add((strip, roller, minutes));

        /// <summary>Rolling is carried out here, after the hour's moisture, so the window is judged on the strip as it is.</summary>
        public void RunHour(GameTime hour)
        {
            foreach (var (strip, roller, minutes) in _rollQueue)
            {
                _rolling.Roll(_square.Get(strip), roller, minutes);
            }
            _rollQueue.Clear();
        }
    }
}
