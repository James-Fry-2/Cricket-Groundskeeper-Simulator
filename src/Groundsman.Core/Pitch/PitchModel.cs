using System;
using Groundsman.Core.Compaction;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;

namespace Groundsman.Core.Pitch
{
    /// <summary>
    /// Turns a strip's true state into how it plays, following the design's driver table:
    /// pace from hardness; bounce from compaction, clay and water at depth; consistency lost to
    /// structure damage, loose soil, cracks, footholes and wear; seam from grass and a damp
    /// surface; spin from a dry, bare, cracked, rough and worn surface.
    /// </summary>
    internal sealed class PitchModel
    {
        private readonly PitchSettings _settings;
        private readonly RollingModel _rolling;

        public PitchModel(PitchSettings settings, RollingModel rolling)
        {
            _settings = settings;
            _rolling = rolling;
        }

        public PitchCharacteristics Characterise(StripState strip)
        {
            var loam = strip.Loam;
            var dryness = Share(loam.FieldCapacity - strip.SurfaceMoisture, loam.FieldCapacity - loam.AirDry);
            var depthMoisture = Share(strip.SubsurfaceMoisture - loam.AirDry, loam.FieldCapacity - loam.AirDry);
            var grass = strip.GrassCover / 100 * Math.Min(1, strip.GrassHeightMm / _settings.GrassReferenceMm);

            var cracking = strip.Cracks;

            // Long grass cushions the ball, taking pace and bounce off it.
            var cushion = 1 - _settings.GrassCushion * Clamp01((strip.GrassHeightMm - _settings.GrassReferenceMm) / _settings.GrassReferenceMm);

            var pace = _rolling.Hardness(strip) * cushion;
            var bounce = Clamp01(
                cushion
                * strip.Compaction
                * Math.Pow(loam.ClayPercent / _settings.BounceClayReference, _settings.BounceClayExponent)
                * (_settings.BounceDepthBase + (1 - _settings.BounceDepthBase) * depthMoisture));
            var consistency = Clamp01(
                1
                - _settings.ConsistencyDamageWeight * strip.StructureDamage
                - _settings.ConsistencyLooseWeight * Math.Max(0, _settings.ConsistencyLooseBelow - strip.Compaction)
                - _settings.ConsistencyCrackWeight * cracking
                - _settings.ConsistencyFootholeWeight * strip.Footholes
                - _settings.ConsistencySurfaceWearWeight * strip.SurfaceWear);
            var carry = Math.Sqrt(pace * bounce);
            var seam = Clamp01(grass * (_settings.SeamBase + _settings.SeamWetWeight * (1 - dryness)));
            var spin = Clamp01(
                _settings.SpinDryWeight * dryness
                + _settings.SpinCrackWeight * cracking
                + _settings.SpinRoughWeight * strip.Rough
                + _settings.SpinSurfaceWearWeight * strip.SurfaceWear
                - _settings.SpinGrassWeight * grass);

            return new PitchCharacteristics(10 * pace, 10 * bounce, 10 * consistency, 10 * carry, 10 * seam, 10 * spin, 10 * cracking);
        }

        private static double Share(double part, double whole) => Clamp01(part / whole);

        private static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
    }
}
