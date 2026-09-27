using System;
using System.Collections.Generic;
using Groundsman.Core.Simulation;
using Groundsman.Core.Time;

namespace Groundsman.Core.Weather
{
    internal sealed class WeatherSystem : IHourlySystem
    {
        private readonly WeatherGenerator _generator;
        private readonly DateTime _firstDate;
        private readonly List<DayWeather> _days = new List<DayWeather>();
        private readonly double[] _gauge = new double[24];
        private int _hoursRun;

        public WeatherSystem(WeatherGenerator generator, DateTime firstDate)
        {
            _generator = generator;
            _firstDate = firstDate.Date;
        }

        public TickStep Step => TickStep.Weather;

        /// <summary>The hour most recently run, or null before the first tick.</summary>
        public HourWeather? LastHour { get; private set; }

        public double RainLast24HoursMm
        {
            get
            {
                var total = 0.0;
                foreach (var rain in _gauge)
                {
                    total += rain;
                }
                return total;
            }
        }

        /// <summary>
        /// True weather for a day, generating ahead as needed. Days are always generated in date
        /// order, so asking ahead (for a forecast) never changes what the day turns out to be.
        /// </summary>
        public DateTime FirstDate => _firstDate;

        public DayWeather Day(DateTime date)
        {
            date = date.Date;
            if (date < _firstDate)
            {
                throw new ArgumentOutOfRangeException(nameof(date), date, "Before the first day of weather.");
            }

            while (_days.Count == 0 || _days[_days.Count - 1].Date < date)
            {
                _days.Add(_generator.Next(_firstDate.AddDays(_days.Count)));
            }
            return _days[(date - _firstDate).Days];
        }

        public void RunHour(GameTime hour)
        {
            var weather = Day(hour.Date).Hours[hour.Hour];
            LastHour = weather;
            _gauge[_hoursRun % 24] = weather.RainMm;
            _hoursRun++;
        }
    }
}
