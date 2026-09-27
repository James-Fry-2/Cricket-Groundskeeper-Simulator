namespace Groundsman.Core
{
    /// <summary>
    /// What the ground's own instruments show: exact, since gauges and thermometers aren't part
    /// of the information puzzle.
    /// </summary>
    public sealed class WeatherObservation
    {
        public WeatherObservation(double rainLast24HoursMm, double temperature, double windKph)
        {
            RainLast24HoursMm = rainLast24HoursMm;
            Temperature = temperature;
            WindKph = windKph;
        }

        public double RainLast24HoursMm { get; }

        /// <summary>°C over the hour just gone.</summary>
        public double Temperature { get; }

        public double WindKph { get; }
    }
}
