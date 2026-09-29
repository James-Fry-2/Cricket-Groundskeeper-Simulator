using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Groundsman.Core.Content
{
    /// <summary>The match referee's thresholds, on the ICC's scale, and the words for each reason.</summary>
    public sealed class RatingSettings
    {
        public static readonly IReadOnlyList<string> ReasonIds = new[]
        {
            "dangerous", "abandoned", "uneven", "dead", "bowlers", "limitedLow", "lifeless", "consistent", "carry", "movement", "flat",
        };

        private static readonly Regex Placeholder = new Regex(@"\{(\w+)\}");
        private readonly Dictionary<string, string> _reasons;

        public RatingSettings(
            double unfitBelow,
            double abandonBelow,
            double unsatisfactoryConsistencyBelow,
            double unsatisfactoryCarryBelow,
            double bowlersOversShare,
            double bowlersRunsPerWicket,
            double limitedParShare,
            double lifelessRunsPerWicket,
            double lifelessMovementBelow,
            double veryGoodConsistencyAtLeast,
            double veryGoodCarryAtLeast,
            double veryGoodMovementAtLeast,
            int unsatisfactoryDemerits,
            int unfitDemerits,
            int windowYears,
            int banAt,
            IDictionary<string, string> reasons)
        {
            CheckScale("unfitBelow", unfitBelow);
            if (abandonBelow < 0 || abandonBelow > unfitBelow)
            {
                throw new ContentException($"rating.abandonBelow ({abandonBelow}) must be from 0 to unfitBelow ({unfitBelow}).");
            }
            if (unsatisfactoryConsistencyBelow <= unfitBelow || unsatisfactoryConsistencyBelow > 10)
            {
                throw new ContentException($"rating.unsatisfactory.consistencyBelow ({unsatisfactoryConsistencyBelow}) must be above unfitBelow ({unfitBelow}) and at most 10.");
            }
            CheckScale("unsatisfactory.carryBelow", unsatisfactoryCarryBelow);
            CheckShare("unsatisfactory.bowlersOversShare", bowlersOversShare);
            CheckNotNegative("unsatisfactory.bowlersRunsPerWicket", bowlersRunsPerWicket);
            CheckShare("unsatisfactory.limitedParShare", limitedParShare);
            CheckNotNegative("unsatisfactory.lifelessRunsPerWicket", lifelessRunsPerWicket);
            CheckScale("unsatisfactory.lifelessMovementBelow", lifelessMovementBelow);
            if (veryGoodConsistencyAtLeast < unsatisfactoryConsistencyBelow || veryGoodConsistencyAtLeast > 10)
            {
                throw new ContentException($"rating.veryGood.consistencyAtLeast ({veryGoodConsistencyAtLeast}) must be from unsatisfactory.consistencyBelow ({unsatisfactoryConsistencyBelow}) to 10.");
            }
            CheckScale("veryGood.carryAtLeast", veryGoodCarryAtLeast);
            CheckScale("veryGood.movementAtLeast", veryGoodMovementAtLeast);
            if (unsatisfactoryDemerits < 0 || unfitDemerits < unsatisfactoryDemerits)
            {
                throw new ContentException("rating.demerits: unfit must cost at least as many as unsatisfactory, and neither can be negative.");
            }
            if (windowYears < 1)
            {
                throw new ContentException($"rating.demerits.windowYears ({windowYears}) must be at least 1.");
            }
            if (banAt < 1)
            {
                throw new ContentException($"rating.demerits.banAt ({banAt}) must be at least 1.");
            }
            foreach (var id in ReasonIds)
            {
                if (!reasons.TryGetValue(id, out var text) || string.IsNullOrWhiteSpace(text))
                {
                    throw new ContentException($"rating.reasons.{id} is missing.");
                }
                foreach (System.Text.RegularExpressions.Match match in Placeholder.Matches(text))
                {
                    var name = match.Groups[1].Value;
                    if (name != "value" && name != "cause")
                    {
                        throw new ContentException($"rating.reasons.{id} uses {{{name}}}, which isn't a placeholder the game fills.");
                    }
                }
            }

            UnfitBelow = unfitBelow;
            AbandonBelow = abandonBelow;
            UnsatisfactoryConsistencyBelow = unsatisfactoryConsistencyBelow;
            UnsatisfactoryCarryBelow = unsatisfactoryCarryBelow;
            BowlersOversShare = bowlersOversShare;
            BowlersRunsPerWicket = bowlersRunsPerWicket;
            LimitedParShare = limitedParShare;
            LifelessRunsPerWicket = lifelessRunsPerWicket;
            LifelessMovementBelow = lifelessMovementBelow;
            VeryGoodConsistencyAtLeast = veryGoodConsistencyAtLeast;
            VeryGoodCarryAtLeast = veryGoodCarryAtLeast;
            VeryGoodMovementAtLeast = veryGoodMovementAtLeast;
            UnsatisfactoryDemerits = unsatisfactoryDemerits;
            UnfitDemerits = unfitDemerits;
            WindowYears = windowYears;
            BanAt = banAt;
            _reasons = new Dictionary<string, string>(reasons);
        }

        /// <summary>Consistency at any hour below which the pitch is dangerous and rated unfit.</summary>
        public double UnfitBelow { get; }

        /// <summary>Consistency below which the referee stops play and abandons the match.</summary>
        public double AbandonBelow { get; }

        public double UnsatisfactoryConsistencyBelow { get; }
        public double UnsatisfactoryCarryBelow { get; }

        /// <summary>A multi-day result inside this share of the scheduled overs is too quick...</summary>
        public double BowlersOversShare { get; }

        /// <summary>...when the match also averaged fewer runs per wicket than this.</summary>
        public double BowlersRunsPerWicket { get; }

        /// <summary>Limited-overs innings under this share of par are too low.</summary>
        public double LimitedParShare { get; }

        public double LifelessRunsPerWicket { get; }
        public double LifelessMovementBelow { get; }
        public double VeryGoodConsistencyAtLeast { get; }
        public double VeryGoodCarryAtLeast { get; }
        public double VeryGoodMovementAtLeast { get; }
        public int UnsatisfactoryDemerits { get; }
        public int UnfitDemerits { get; }
        public int WindowYears { get; }

        /// <summary>Demerits in the window at which the ground is banned from hosting.</summary>
        public int BanAt { get; }

        public string Reason(string id) => _reasons[id];

        private static void CheckScale(string field, double value)
        {
            if (value < 0 || value > 10)
            {
                throw new ContentException($"rating.{field} ({value}) must be from 0 to 10.");
            }
        }

        private static void CheckShare(string field, double value)
        {
            if (value < 0 || value > 1)
            {
                throw new ContentException($"rating.{field} ({value}) must be from 0 to 1.");
            }
        }

        private static void CheckNotNegative(string field, double value)
        {
            if (value < 0)
            {
                throw new ContentException($"rating.{field} ({value}) can't be negative.");
            }
        }
    }
}
