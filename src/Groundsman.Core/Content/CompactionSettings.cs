namespace Groundsman.Core.Content
{
    public sealed class CompactionSettings
    {
        public CompactionSettings(
            double startingCompaction,
            double startingStructureDamage,
            double minRollingMinutes,
            double maxRollingMinutes,
            double wetDamagePerPoint,
            double hardnessDryWeight,
            double hardnessClayReference,
            double hardnessClayExponent)
        {
            CheckUnit("starting.compaction", startingCompaction);
            CheckUnit("starting.structureDamage", startingStructureDamage);
            if (minRollingMinutes <= 0 || maxRollingMinutes < minRollingMinutes)
            {
                throw new ContentException($"compaction.rollingMinutes ({minRollingMinutes} to {maxRollingMinutes}) must be above 0 with max no less than min.");
            }
            if (wetDamagePerPoint < 0)
            {
                throw new ContentException($"compaction.wetDamagePerPoint ({wetDamagePerPoint}) can't be negative.");
            }
            CheckUnit("hardness.dryWeight", hardnessDryWeight);
            if (hardnessClayReference <= 0)
            {
                throw new ContentException($"compaction.hardness.clayReference ({hardnessClayReference}) must be above 0.");
            }
            if (hardnessClayExponent < 0)
            {
                throw new ContentException($"compaction.hardness.clayExponent ({hardnessClayExponent}) can't be negative.");
            }

            StartingCompaction = startingCompaction;
            StartingStructureDamage = startingStructureDamage;
            MinRollingMinutes = minRollingMinutes;
            MaxRollingMinutes = maxRollingMinutes;
            WetDamagePerPoint = wetDamagePerPoint;
            HardnessDryWeight = hardnessDryWeight;
            HardnessClayReference = hardnessClayReference;
            HardnessClayExponent = hardnessClayExponent;
        }

        /// <summary>Compaction, 0 to 1, a strip starts the game with after pre-season rolling.</summary>
        public double StartingCompaction { get; }

        public double StartingStructureDamage { get; }
        public double MinRollingMinutes { get; }
        public double MaxRollingMinutes { get; }

        /// <summary>Extra share of wet-rolling damage for each point of surface moisture above the window.</summary>
        public double WetDamagePerPoint { get; }

        /// <summary>How much of hardness depends on the surface being dry, 0 to 1; the rest comes from compaction alone.</summary>
        public double HardnessDryWeight { get; }

        /// <summary>Clay % at which the loam neither adds nor takes away hardness.</summary>
        public double HardnessClayReference { get; }

        public double HardnessClayExponent { get; }

        private static void CheckUnit(string field, double value)
        {
            if (value < 0 || value > 1)
            {
                throw new ContentException($"compaction.{field} ({value}) must be from 0 to 1.");
            }
        }
    }
}
