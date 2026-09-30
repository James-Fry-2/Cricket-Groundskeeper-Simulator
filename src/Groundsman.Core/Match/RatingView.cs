using System;
using System.Collections.Generic;

namespace Groundsman.Core.Match
{
    /// <summary>The ICC's pitch ratings, best first.</summary>
    public enum PitchGrade
    {
        VeryGood = 1,
        Satisfactory = 2,
        Unsatisfactory = 3,
        Unfit = 4,
    }

    public sealed class RatingView
    {
        public RatingView(PitchGrade grade, int demerits, IReadOnlyList<string> reasons, IReadOnlyList<string> reasonIds, DateTime ratedOn)
        {
            Grade = grade;
            Demerits = demerits;
            Reasons = reasons;
            ReasonIds = reasonIds;
            RatedOn = ratedOn;
        }

        public PitchGrade Grade { get; }
        public int Demerits { get; }

        /// <summary>Why the referee rated it so, most important first.</summary>
        public IReadOnlyList<string> Reasons { get; }

        /// <summary>The reasons' ids from rating.json, in the same order.</summary>
        public IReadOnlyList<string> ReasonIds { get; }

        public DateTime RatedOn { get; }
    }
}
