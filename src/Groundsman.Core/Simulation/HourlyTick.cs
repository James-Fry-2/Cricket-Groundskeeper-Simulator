using System;
using System.Collections.Generic;
using Groundsman.Core.Time;

namespace Groundsman.Core.Simulation
{
    public sealed class HourlyTick
    {
        private readonly IHourlySystem?[] _systems = new IHourlySystem?[Enum.GetValues(typeof(TickStep)).Length];

        public HourlyTick(IEnumerable<IHourlySystem> systems)
        {
            foreach (var system in systems)
            {
                var index = (int)system.Step;
                if (_systems[index] != null)
                {
                    throw new ArgumentException($"Two systems registered for the {system.Step} step.", nameof(systems));
                }
                _systems[index] = system;
            }
        }

        public void RunHour(GameTime hour)
        {
            foreach (var system in _systems)
            {
                system?.RunHour(hour);
            }
        }
    }
}
