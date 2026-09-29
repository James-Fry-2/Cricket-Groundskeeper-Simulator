using System;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;

namespace Groundsman.Core.Wear
{
    /// <summary>
    /// Footholes, rough and surface wear from play, cracks from drying, and recovery. What
    /// resists wear follows the design: compaction, clay binding, roots, and moisture (too wet
    /// sinks, too dry dusts).
    /// </summary>
    internal sealed class WearModel
    {
        private readonly WearSettings _settings;
        private readonly GrassSettings _grass;

        public WearModel(WearSettings settings, GrassSettings grass)
        {
            _settings = settings;
            _grass = grass;
        }

        /// <summary>0 to 1: how well the strip stands up to play right now.</summary>
        public double Resistance(StripState strip)
        {
            var clay = Math.Min(1, strip.Loam.ClayPercent / _settings.ResistanceClayReference);
            var roots = strip.RootDepthMm / _grass.MaxRootDepthMm;
            var resistance = Clamp01(
                _settings.ResistanceCompactionWeight * strip.Compaction
                + _settings.ResistanceClayWeight * clay
                + _settings.ResistanceRootWeight * roots);
            return IsWet(strip) ? resistance * (1 - _settings.ResistanceWetLoss) : resistance;
        }

        public void ApplyOvers(StripState strip, double overs, AttackProfile attack)
        {
            var exposure = overs * (1 - Resistance(strip)) * (IsWet(strip) ? _settings.WetPlay : 1);
            var dusty = strip.SurfaceMoisture < strip.Loam.RollingWindowMin;

            strip.Footholes = Clamp01(strip.Footholes + exposure * _settings.FootholesPerOver * attack.Seam * (1 + _settings.HeavyFootedExtra * attack.HeavyFooted));
            strip.Rough = Clamp01(strip.Rough + exposure * _settings.RoughPerOver * (1 + _settings.LeftArmExtra * attack.LeftArm));
            strip.SurfaceWear = Clamp01(strip.SurfaceWear + exposure * _settings.SurfaceWearPerOver * (dusty ? 1 + _settings.DryDustExtra : 1));
            strip.GrassCover = Math.Max(0, strip.GrassCover - exposure * _settings.CoverLossPerOver);
            strip.EndsRepaired = false;
        }

        /// <summary>
        /// One hour of cracking and recovery. <paramref name="growth"/> is how fast grass is
        /// growing, 0 to 1: worn areas heal as it grows back into them.
        /// </summary>
        public void RunHour(StripState strip, double growth)
        {
            var loam = strip.Loam;
            var dryness = (loam.FieldCapacity - strip.SurfaceMoisture) / (loam.FieldCapacity - loam.AirDry);
            if (dryness > _settings.CrackStartDryness)
            {
                var drive = (dryness - _settings.CrackStartDryness) / (1 - _settings.CrackStartDryness);
                strip.Cracks = Clamp01(strip.Cracks
                    + _settings.CracksPerHour * loam.CrackingTendency * Math.Min(1, drive)
                    * (1 + _settings.CrackDamageExtra * strip.StructureDamage) * (1 - strip.Cracks));
            }
            else if (strip.SurfaceMoisture >= loam.FieldCapacity)
            {
                strip.Cracks = Math.Max(0, strip.Cracks - _settings.CracksClosePerHour);
            }

            var perDay = strip.EndsRepaired ? _settings.RepairedRecoveryPerDay : _settings.RecoveryPerDay;
            var healed = perDay / 24 * growth;
            strip.Footholes = Math.Max(0, strip.Footholes - healed);
            strip.Rough = Math.Max(0, strip.Rough - healed);
            strip.SurfaceWear = Math.Max(0, strip.SurfaceWear - healed);
        }

        /// <summary>Fills and seeds the footholes and rough, and speeds their recovery from now on.</summary>
        public void Repair(StripState strip)
        {
            strip.Footholes *= 1 - _settings.RepairFills;
            strip.Rough *= 1 - _settings.RepairFills;
            strip.EndsRepaired = true;
        }

        public void CleanFootholes(StripState strip) => strip.Footholes *= 1 - _settings.CleanShare;

        public void FillFootholes(StripState strip) => strip.Footholes *= 1 - _settings.FillShare;

        private static bool IsWet(StripState strip) => strip.SurfaceMoisture > strip.Loam.RollingWindowMax;

        private static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
    }
}
