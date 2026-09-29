namespace Groundsman.Core.Content
{
    public sealed class MatchSettings
    {
        public MatchSettings(
            double strengthScale,
            double seamWickets,
            double spinWickets,
            double unevenWickets,
            double deadWickets,
            double carryRuns,
            double unevenRuns,
            double deadRuns,
            double runsSpread,
            double declarationLead,
            int declarationFromDay,
            double rainRestartLoss,
            double tossBowlFirstSeamAbove)
        {
            CheckNotNegative("strengthScale", strengthScale);
            CheckNotNegative("wickets.seam", seamWickets);
            CheckNotNegative("wickets.spin", spinWickets);
            CheckNotNegative("wickets.uneven", unevenWickets);
            CheckUnit("wickets.dead", deadWickets);
            CheckNotNegative("runs.carry", carryRuns);
            CheckUnit("runs.uneven", unevenRuns);
            CheckUnit("runs.dead", deadRuns);
            CheckNotNegative("runs.spread", runsSpread);
            CheckNotNegative("declaration.lead", declarationLead);
            if (declarationFromDay < 1)
            {
                throw new ContentException($"match.declaration.fromDay ({declarationFromDay}) must be at least 1.");
            }
            CheckUnit("rainRestartLoss", rainRestartLoss);
            if (tossBowlFirstSeamAbove < 0 || tossBowlFirstSeamAbove > 10)
            {
                throw new ContentException($"match.tossBowlFirstSeamAbove ({tossBowlFirstSeamAbove}) must be from 0 to 10.");
            }

            StrengthScale = strengthScale;
            SeamWickets = seamWickets;
            SpinWickets = spinWickets;
            UnevenWickets = unevenWickets;
            DeadWickets = deadWickets;
            CarryRuns = carryRuns;
            UnevenRuns = unevenRuns;
            DeadRuns = deadRuns;
            RunsSpread = runsSpread;
            DeclarationLead = declarationLead;
            DeclarationFromDay = declarationFromDay;
            RainRestartLoss = rainRestartLoss;
            TossBowlFirstSeamAbove = tossBowlFirstSeamAbove;
        }

        /// <summary>How strongly a 50-point strength gap shifts rates, on a log scale.</summary>
        public double StrengthScale { get; }

        /// <summary>Extra wicket share from a fully seaming pitch against an all-seam attack.</summary>
        public double SeamWickets { get; }

        public double SpinWickets { get; }

        /// <summary>Extra wicket share from bounce with no consistency at all.</summary>
        public double UnevenWickets { get; }

        /// <summary>Share of wickets lost on a completely dead pitch.</summary>
        public double DeadWickets { get; }

        /// <summary>Run share gained per unit of carry above the middle of the scale.</summary>
        public double CarryRuns { get; }

        public double UnevenRuns { get; }
        public double DeadRuns { get; }

        /// <summary>Spread of runs in an hour, as a share of the expected runs per over.</summary>
        public double RunsSpread { get; }

        /// <summary>Lead at which the side batting third declares in multi-day cricket.</summary>
        public double DeclarationLead { get; }

        /// <summary>Earliest day on which a declaration is made.</summary>
        public int DeclarationFromDay { get; }

        /// <summary>Share of the next hour's overs lost while the ground recovers from rain.</summary>
        public double RainRestartLoss { get; }

        /// <summary>Seam, 0 to 10, above which the toss winner chooses to bowl.</summary>
        public double TossBowlFirstSeamAbove { get; }

        private static void CheckNotNegative(string field, double value)
        {
            if (value < 0)
            {
                throw new ContentException($"match.{field} ({value}) can't be negative.");
            }
        }

        private static void CheckUnit(string field, double value)
        {
            if (value < 0 || value > 1)
            {
                throw new ContentException($"match.{field} ({value}) must be from 0 to 1.");
            }
        }
    }
}
