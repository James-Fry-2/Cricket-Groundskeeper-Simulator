using System;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;

namespace Groundsman.Core.Compaction
{
    /// <summary>
    /// Rolling and the hardness it builds. Rolling only compacts inside the loam's moisture
    /// window: too dry does nothing, too wet compacts but damages the soil's structure, and a
    /// heavy roller on an already tight strip does harm too (docs/research.md section 3).
    /// </summary>
    internal sealed class RollingModel
    {
        private readonly CompactionSettings _settings;

        public RollingModel(CompactionSettings settings)
        {
            _settings = settings;
        }

        public void Roll(StripState strip, RollerSettings roller, double minutes)
        {
            var loam = strip.Loam;
            var surface = strip.SurfaceMoisture;
            if (surface < loam.RollingWindowMin)
            {
                return;
            }

            var hours = minutes / 60;
            var before = strip.Compaction;
            strip.Compaction = Math.Min(1, before + roller.CompactionPerHour * hours * (1 - before));

            var damage = 0.0;
            if (surface > loam.RollingWindowMax)
            {
                damage += roller.WetDamagePerHour * hours * (1 + (surface - loam.RollingWindowMax) * _settings.WetDamagePerPoint);
            }
            if (before > roller.OveruseAbove)
            {
                damage += roller.OveruseDamagePerHour * hours * (before - roller.OveruseAbove) / (1 - roller.OveruseAbove);
            }
            strip.StructureDamage = Math.Min(1, strip.StructureDamage + damage);
        }

        /// <summary>0 to 1: compaction, made harder by a dry surface and by clay.</summary>
        public double Hardness(StripState strip)
        {
            var loam = strip.Loam;
            var dryness = Clamp01((loam.FieldCapacity - strip.SurfaceMoisture) / (loam.FieldCapacity - loam.AirDry));
            var moisture = 1 - _settings.HardnessDryWeight + _settings.HardnessDryWeight * dryness;
            var clay = Math.Pow(loam.ClayPercent / _settings.HardnessClayReference, _settings.HardnessClayExponent);
            return Clamp01(strip.Compaction * moisture * clay);
        }

        private static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
    }
}
