using System;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;
using Groundsman.Core.Weather;

namespace Groundsman.Core.Moisture
{
    /// <summary>
    /// One hour of water movement in a strip's two layers: a starting point to tune, not a
    /// claim about real soil physics. Works in mm of water so layers of different depths
    /// exchange water without creating or losing any.
    /// </summary>
    internal sealed class MoistureModel
    {
        private readonly MoistureSettings _settings;
        private readonly CoverSettings _covers;

        public MoistureModel(MoistureSettings settings, CoverSettings covers)
        {
            _settings = settings;
            _covers = covers;
        }

        public void RunHour(StripState strip, HourWeather weather, bool covered, double wateringMm)
        {
            var loam = strip.Loam;
            var surfaceDepth = _settings.SurfaceDepthMm;
            var subsurfaceDepth = _settings.SubsurfaceDepthMm;
            var surface = ToMm(strip.SurfaceMoisture, surfaceDepth);
            var subsurface = ToMm(strip.SubsurfaceMoisture, subsurfaceDepth);

            // In: rain unless covered, plus watering. What the surface can't hold soaks straight
            // into the subsurface; what that can't hold runs off.
            surface += (covered ? 0 : weather.RainMm) + wateringMm;
            var surfaceSaturation = ToMm(loam.Saturation, surfaceDepth);
            var subsurfaceSaturation = ToMm(loam.Saturation, subsurfaceDepth);
            if (surface > surfaceSaturation)
            {
                subsurface += surface - surfaceSaturation;
                surface = surfaceSaturation;
            }
            subsurface = Math.Min(subsurface, subsurfaceSaturation);

            // Gravity drainage only moves water held above field capacity.
            var surfaceExcess = surface - ToMm(loam.FieldCapacity, surfaceDepth);
            if (surfaceExcess > 0)
            {
                var drained = Math.Min(loam.SurfaceDrainageRate * surfaceExcess, subsurfaceSaturation - subsurface);
                surface -= drained;
                subsurface += drained;
            }
            var subsurfaceExcess = subsurface - ToMm(loam.FieldCapacity, subsurfaceDepth);
            if (subsurfaceExcess > 0)
            {
                subsurface -= loam.SubsurfaceDrainageRate * subsurfaceExcess;
            }

            // Capillary rise: a surface drier than the layer below draws water up, which is how
            // the subsurface slowly loses water in dry weather.
            var gap = ToPercent(subsurface, subsurfaceDepth) - ToPercent(surface, surfaceDepth);
            if (gap > 0)
            {
                var rise = loam.CapillaryRate * ToMm(gap, surfaceDepth);
                surface += rise;
                subsurface -= rise;
            }

            // Evaporation from the surface, slowing as it dries towards air-dry, and much slower
            // under a sheet.
            var airDry = ToMm(loam.AirDry, surfaceDepth);
            var availability = Clamp01((surface - airDry) / (ToMm(loam.FieldCapacity, surfaceDepth) - airDry));
            var potential = PotentialEvaporationMm(weather) * (covered ? _covers.EvaporationFactor : 1);
            var evaporation = Math.Min(potential * availability, Math.Max(0, surface - airDry));
            surface -= evaporation;

            // Roots draw from the subsurface, falling away sharply as it dries towards air-dry.
            var subsurfaceAirDry = ToMm(loam.AirDry, subsurfaceDepth);
            var rootAvailability = Math.Pow(Clamp01((subsurface - subsurfaceAirDry) / (ToMm(loam.FieldCapacity, subsurfaceDepth) - subsurfaceAirDry)), _settings.RootUptakeCurve);
            var uptake = Math.Min(potential * _settings.RootUptakeShare * rootAvailability, Math.Max(0, subsurface - subsurfaceAirDry));
            subsurface -= uptake;

            strip.SurfaceMoisture = ToPercent(surface, surfaceDepth);
            strip.SubsurfaceMoisture = ToPercent(subsurface, subsurfaceDepth);
        }

        private double PotentialEvaporationMm(HourWeather weather) =>
            _settings.EvaporationPerDegreeMm * Math.Max(0, weather.Temperature) * (1 + _settings.EvaporationWindFactorPerKph * weather.WindKph)
            + _settings.EvaporationPerSunshineHourMm * weather.Sunshine;

        private static double ToMm(double percent, double depthMm) => percent / 100 * depthMm;

        private static double ToPercent(double mm, double depthMm) => mm / depthMm * 100;

        private static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
    }
}
