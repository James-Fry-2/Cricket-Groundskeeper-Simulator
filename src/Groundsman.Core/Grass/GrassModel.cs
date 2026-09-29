using System;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;
using Groundsman.Core.Weather;

namespace Groundsman.Core.Grass
{
    /// <summary>One hour of grass growth, and mowing, for a strip.</summary>
    internal sealed class GrassModel
    {
        private readonly GrassSettings _settings;

        public GrassModel(GrassSettings settings)
        {
            _settings = settings;
        }

        /// <summary>How fast grass is growing on the strip now, 0 to 1; 0 in drought.</summary>
        public double GrowthFactor(StripState strip, HourWeather weather)
        {
            var water = WaterFactor(strip);
            return water < _settings.DroughtWaterBelow ? 0 : TemperatureFactor(weather.Temperature) * water;
        }

        public void RunHour(StripState strip, HourWeather weather)
        {
            var temperature = TemperatureFactor(weather.Temperature);
            var water = WaterFactor(strip);

            if (water < _settings.DroughtWaterBelow)
            {
                strip.GrassCover = Math.Max(0, strip.GrassCover - _settings.DroughtCoverLossPerDay / 24);
                return;
            }

            var growth = temperature * water / 24;
            strip.GrassHeightMm += _settings.HeightPerDayMm * growth;
            strip.GrassCover = Math.Min(_settings.MaxCover, strip.GrassCover + _settings.CoverPerDay * growth);
            strip.RootDepthMm = Math.Min(_settings.MaxRootDepthMm, strip.RootDepthMm + _settings.RootsPerDayMm * growth);
        }

        public void Mow(StripState strip, double heightMm)
        {
            var cut = strip.GrassHeightMm - heightMm;
            if (cut <= 0)
            {
                return;
            }

            // Taking off more than a set share of the height at once scalps the strip.
            var overCut = cut - _settings.ScalpShare * strip.GrassHeightMm;
            if (overCut > 0)
            {
                strip.GrassCover = Math.Max(0, strip.GrassCover - overCut * _settings.ScalpCoverLossPerMm);
            }
            strip.GrassHeightMm = heightMm;
        }

        private double TemperatureFactor(double temperature)
        {
            if (temperature <= _settings.MinTemperature || temperature >= _settings.MaxTemperature)
            {
                return 0;
            }
            return temperature <= _settings.OptimumTemperature
                ? (temperature - _settings.MinTemperature) / (_settings.OptimumTemperature - _settings.MinTemperature)
                : (_settings.MaxTemperature - temperature) / (_settings.MaxTemperature - _settings.OptimumTemperature);
        }

        // Roots feed from the subsurface, so growth follows water there: full at field capacity,
        // none at air-dry.
        private static double WaterFactor(StripState strip)
        {
            var loam = strip.Loam;
            var share = (strip.SubsurfaceMoisture - loam.AirDry) / (loam.FieldCapacity - loam.AirDry);
            return share < 0 ? 0 : share > 1 ? 1 : share;
        }
    }
}
