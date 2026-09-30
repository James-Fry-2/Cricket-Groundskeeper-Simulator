namespace Groundsman.Core.Content
{
    public sealed class WearSettings
    {
        public WearSettings(
            double resistanceCompactionWeight,
            double resistanceClayWeight,
            double resistanceClayReference,
            double resistanceRootWeight,
            double resistanceWetLoss,
            double footholesPerOver,
            double heavyFootedExtra,
            double roughPerOver,
            double leftArmExtra,
            double surfaceWearPerOver,
            double dryDustExtra,
            double coverLossPerOver,
            double wetPlay,
            double crackStartDryness,
            double cracksPerHour,
            double crackDamageExtra,
            double cracksClosePerHour,
            double recoveryPerDay,
            double repairedRecoveryPerDay,
            double repairFills,
            double cleanShare,
            double fillShare,
            double neighbourShare,
            double lastingShare,
            double lastingResistanceLoss,
            double endsLossPerWear,
            double endsEstablishDays,
            double endsUnrepairedDays,
            double endsGerminateShare,
            double endsResistanceLoss)
        {
            CheckNotNegative("resistance.compactionWeight", resistanceCompactionWeight);
            CheckNotNegative("resistance.clayWeight", resistanceClayWeight);
            if (resistanceClayReference <= 0)
            {
                throw new ContentException($"wear.resistance.clayReference ({resistanceClayReference}) must be above 0.");
            }
            CheckNotNegative("resistance.rootWeight", resistanceRootWeight);
            CheckUnit("resistance.wetLoss", resistanceWetLoss);
            CheckNotNegative("perOver.footholes", footholesPerOver);
            CheckNotNegative("perOver.heavyFootedExtra", heavyFootedExtra);
            CheckNotNegative("perOver.rough", roughPerOver);
            CheckNotNegative("perOver.leftArmExtra", leftArmExtra);
            CheckNotNegative("perOver.surface", surfaceWearPerOver);
            CheckNotNegative("perOver.dryDustExtra", dryDustExtra);
            CheckNotNegative("perOver.coverLoss", coverLossPerOver);
            if (wetPlay < 1)
            {
                throw new ContentException($"wear.wetPlay ({wetPlay}) must be at least 1.");
            }
            if (crackStartDryness < 0 || crackStartDryness >= 1)
            {
                throw new ContentException($"wear.cracks.startDryness ({crackStartDryness}) must be from 0 to under 1.");
            }
            CheckNotNegative("cracks.perHour", cracksPerHour);
            CheckNotNegative("cracks.damageExtra", crackDamageExtra);
            CheckNotNegative("cracks.closePerHour", cracksClosePerHour);
            CheckNotNegative("recovery.perDay", recoveryPerDay);
            CheckNotNegative("recovery.repairedPerDay", repairedRecoveryPerDay);
            CheckUnit("recovery.repairFills", repairFills);
            CheckUnit("duringMatch.cleanShare", cleanShare);
            CheckUnit("duringMatch.fillShare", fillShare);
            CheckUnit("runUps.neighbourShare", neighbourShare);
            CheckUnit("lasting.share", lastingShare);
            CheckUnit("lasting.resistanceLoss", lastingResistanceLoss);
            CheckNotNegative("ends.lossPerWear", endsLossPerWear);
            if (endsEstablishDays <= 0)
            {
                throw new ContentException($"wear.ends.establishDays ({endsEstablishDays}) must be above 0.");
            }
            if (endsUnrepairedDays < endsEstablishDays)
            {
                throw new ContentException($"wear.ends.unrepairedDays ({endsUnrepairedDays}) can't be shorter than establishDays ({endsEstablishDays}): repairs speed regrowth.");
            }
            CheckUnit("ends.germinateShare", endsGerminateShare);
            CheckUnit("ends.resistanceLoss", endsResistanceLoss);

            ResistanceCompactionWeight = resistanceCompactionWeight;
            ResistanceClayWeight = resistanceClayWeight;
            ResistanceClayReference = resistanceClayReference;
            ResistanceRootWeight = resistanceRootWeight;
            ResistanceWetLoss = resistanceWetLoss;
            FootholesPerOver = footholesPerOver;
            HeavyFootedExtra = heavyFootedExtra;
            RoughPerOver = roughPerOver;
            LeftArmExtra = leftArmExtra;
            SurfaceWearPerOver = surfaceWearPerOver;
            DryDustExtra = dryDustExtra;
            CoverLossPerOver = coverLossPerOver;
            WetPlay = wetPlay;
            CrackStartDryness = crackStartDryness;
            CracksPerHour = cracksPerHour;
            CrackDamageExtra = crackDamageExtra;
            CracksClosePerHour = cracksClosePerHour;
            RecoveryPerDay = recoveryPerDay;
            RepairedRecoveryPerDay = repairedRecoveryPerDay;
            RepairFills = repairFills;
            CleanShare = cleanShare;
            FillShare = fillShare;
            NeighbourShare = neighbourShare;
            LastingShare = lastingShare;
            LastingResistanceLoss = lastingResistanceLoss;
            EndsLossPerWear = endsLossPerWear;
            EndsEstablishDays = endsEstablishDays;
            EndsUnrepairedDays = endsUnrepairedDays;
            EndsGerminateShare = endsGerminateShare;
            EndsResistanceLoss = endsResistanceLoss;
        }

        public double ResistanceCompactionWeight { get; }
        public double ResistanceClayWeight { get; }

        /// <summary>Clay % at which the loam binds as well as it can against wear.</summary>
        public double ResistanceClayReference { get; }

        public double ResistanceRootWeight { get; }

        /// <summary>Share of resistance lost when the surface is wetter than the rolling window.</summary>
        public double ResistanceWetLoss { get; }

        /// <summary>Foothole depth added per seam over on a strip with no resistance.</summary>
        public double FootholesPerOver { get; }

        /// <summary>Extra foothole digging, as a share, for a fully heavy-footed attack.</summary>
        public double HeavyFootedExtra { get; }

        public double RoughPerOver { get; }

        /// <summary>
        /// Extra useful rough, as a share, for an all left-arm attack: their follow-through lands
        /// outside a right-hander's off stump, where spinners aim.
        /// </summary>
        public double LeftArmExtra { get; }

        public double SurfaceWearPerOver { get; }

        /// <summary>Extra surface wear, as a share, when the surface is drier than the rolling window.</summary>
        public double DryDustExtra { get; }

        /// <summary>Grass cover lost per over, percentage points, on a strip with no resistance.</summary>
        public double CoverLossPerOver { get; }

        /// <summary>Multiplier on all wear when play goes on with the surface wetter than the rolling window.</summary>
        public double WetPlay { get; }

        /// <summary>Surface dryness, 0 to 1, beyond which cracks start to open.</summary>
        public double CrackStartDryness { get; }

        public double CracksPerHour { get; }

        /// <summary>Extra cracking, as a share per unit of structure damage.</summary>
        public double CrackDamageExtra { get; }

        /// <summary>Cracks closed per hour while the surface is at or above field capacity.</summary>
        public double CracksClosePerHour { get; }

        /// <summary>Wear healed per day while grass grows at full rate.</summary>
        public double RecoveryPerDay { get; }

        public double RepairedRecoveryPerDay { get; }

        /// <summary>Share of foothole and rough damage a repair fills straight away.</summary>
        public double RepairFills { get; }

        /// <summary>Share of foothole damage cleaning and drying them at a break takes away.</summary>
        public double CleanShare { get; }

        /// <summary>Share of foothole damage filled at close of play in a multi-day match.</summary>
        public double FillShare { get; }

        /// <summary>Footholes dug in a neighbour's ends by run-ups, as a share of the match strip's.</summary>
        public double NeighbourShare { get; }

        /// <summary>Share of the wear a match digs that stays until renovation.</summary>
        public double LastingShare { get; }

        /// <summary>Share of resistance lost per unit of lasting wear.</summary>
        public double LastingResistanceLoss { get; }

        /// <summary>Establishment the ends lose per unit of footholes dug.</summary>
        public double EndsLossPerWear { get; }

        /// <summary>Days of full grass growth for repaired ends to establish from bare.</summary>
        public double EndsEstablishDays { get; }

        /// <summary>The same for ends left unrepaired.</summary>
        public double EndsUnrepairedDays { get; }

        /// <summary>Establishment below which repaired ends still look only seeded.</summary>
        public double EndsGerminateShare { get; }

        /// <summary>Share of resistance lost with bare ends, less as they establish.</summary>
        public double EndsResistanceLoss { get; }

        private static void CheckNotNegative(string field, double value)
        {
            if (value < 0)
            {
                throw new ContentException($"wear.{field} ({value}) can't be negative.");
            }
        }

        private static void CheckUnit(string field, double value)
        {
            if (value < 0 || value > 1)
            {
                throw new ContentException($"wear.{field} ({value}) must be from 0 to 1.");
            }
        }
    }
}
