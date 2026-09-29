namespace Groundsman.Core.Content
{
    /// <summary>Coefficients turning a strip's state into how it plays. All placeholders to tune.</summary>
    public sealed class PitchSettings
    {
        public PitchSettings(
            double grassReferenceMm,
            double grassCushion,
            double bounceDepthBase,
            double bounceClayReference,
            double bounceClayExponent,
            double consistencyDamageWeight,
            double consistencyLooseBelow,
            double consistencyLooseWeight,
            double consistencyCrackWeight,
            double seamBase,
            double seamWetWeight,
            double spinDryWeight,
            double spinCrackWeight,
            double spinGrassWeight,
            double crackingDryExponent)
        {
            if (grassReferenceMm <= 0)
            {
                throw new ContentException($"pitch.grassReferenceMm ({grassReferenceMm}) must be above 0.");
            }
            CheckUnit("grassCushion", grassCushion);
            CheckUnit("bounce.depthBase", bounceDepthBase);
            if (bounceClayReference <= 0)
            {
                throw new ContentException($"pitch.bounce.clayReference ({bounceClayReference}) must be above 0.");
            }
            CheckNotNegative("bounce.clayExponent", bounceClayExponent);
            CheckNotNegative("consistency.damageWeight", consistencyDamageWeight);
            CheckUnit("consistency.looseBelow", consistencyLooseBelow);
            CheckNotNegative("consistency.looseWeight", consistencyLooseWeight);
            CheckNotNegative("consistency.crackWeight", consistencyCrackWeight);
            CheckNotNegative("seam.base", seamBase);
            CheckNotNegative("seam.wetWeight", seamWetWeight);
            CheckNotNegative("spin.dryWeight", spinDryWeight);
            CheckNotNegative("spin.crackWeight", spinCrackWeight);
            CheckNotNegative("spin.grassWeight", spinGrassWeight);
            if (crackingDryExponent <= 0)
            {
                throw new ContentException($"pitch.cracking.dryExponent ({crackingDryExponent}) must be above 0.");
            }

            GrassReferenceMm = grassReferenceMm;
            GrassCushion = grassCushion;
            BounceDepthBase = bounceDepthBase;
            BounceClayReference = bounceClayReference;
            BounceClayExponent = bounceClayExponent;
            ConsistencyDamageWeight = consistencyDamageWeight;
            ConsistencyLooseBelow = consistencyLooseBelow;
            ConsistencyLooseWeight = consistencyLooseWeight;
            ConsistencyCrackWeight = consistencyCrackWeight;
            SeamBase = seamBase;
            SeamWetWeight = seamWetWeight;
            SpinDryWeight = spinDryWeight;
            SpinCrackWeight = spinCrackWeight;
            SpinGrassWeight = spinGrassWeight;
            CrackingDryExponent = crackingDryExponent;
        }

        /// <summary>Grass height at which a full sward gives the seamers all it can.</summary>
        public double GrassReferenceMm { get; }

        /// <summary>
        /// Share of pace and bounce lost to grass twice the reference height or longer; nothing
        /// is lost at or below the reference.
        /// </summary>
        public double GrassCushion { get; }

        /// <summary>Share of bounce a compacted strip keeps with no moisture at depth.</summary>
        public double BounceDepthBase { get; }

        /// <summary>Clay % at which the loam neither adds nor takes away bounce.</summary>
        public double BounceClayReference { get; }

        public double BounceClayExponent { get; }
        public double ConsistencyDamageWeight { get; }

        /// <summary>Compaction below which the strip is loose enough to bounce unevenly.</summary>
        public double ConsistencyLooseBelow { get; }

        public double ConsistencyLooseWeight { get; }
        public double ConsistencyCrackWeight { get; }

        /// <summary>Seam from grass on a dry surface, as a share of the most grass can give.</summary>
        public double SeamBase { get; }

        /// <summary>Extra seam share from a damp surface.</summary>
        public double SeamWetWeight { get; }

        public double SpinDryWeight { get; }
        public double SpinCrackWeight { get; }
        public double SpinGrassWeight { get; }

        /// <summary>How sharply cracking rises as the surface dries.</summary>
        public double CrackingDryExponent { get; }

        private static void CheckNotNegative(string field, double value)
        {
            if (value < 0)
            {
                throw new ContentException($"pitch.{field} ({value}) can't be negative.");
            }
        }

        private static void CheckUnit(string field, double value)
        {
            if (value < 0 || value > 1)
            {
                throw new ContentException($"pitch.{field} ({value}) must be from 0 to 1.");
            }
        }
    }
}
