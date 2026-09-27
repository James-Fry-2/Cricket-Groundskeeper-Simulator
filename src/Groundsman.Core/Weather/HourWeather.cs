namespace Groundsman.Core.Weather
{
    public readonly struct HourWeather
    {
        public HourWeather(double rainMm, double temperature, double windKph, double sunshine)
        {
            RainMm = rainMm;
            Temperature = temperature;
            WindKph = windKph;
            Sunshine = sunshine;
        }

        public double RainMm { get; }

        /// <summary>°C.</summary>
        public double Temperature { get; }

        public double WindKph { get; }

        /// <summary>Fraction of the hour in bright sunshine, 0 to 1.</summary>
        public double Sunshine { get; }
    }
}
