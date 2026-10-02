using System;
using Groundsman.Core.Match;

namespace Groundsman.Core.Pressures
{
    public enum SatisfactionReason
    {
        RequestDelivered = 1,
        RequestNotDelivered = 2,
        RequestDeclined = 3,
        RequestIgnored = 4,
        HomeWin = 5,
        HomeLoss = 6,
        DayFourReached = 7,
        ShortFourDay = 8,
        NoResult = 9,
        TelevisedCentre = 10,
        TelevisedOffCentre = 11,
        Demerits = 12,
        Rated = 13,
    }

    /// <summary>One move in a stakeholder's satisfaction and why, for the season review.</summary>
    public sealed class SatisfactionChange
    {
        public SatisfactionChange(Stakeholder stakeholder, double delta, SatisfactionReason reason, Fixture fixture, DateTime date, RequestKind? request = null, PitchGrade? grade = null, int demerits = 0)
        {
            Stakeholder = stakeholder;
            Delta = delta;
            Reason = reason;
            Fixture = fixture;
            Date = date;
            Request = request;
            Grade = grade;
            Demerits = demerits;
        }

        public Stakeholder Stakeholder { get; }

        /// <summary>Points on the 0 to 100 scale; less than asked if satisfaction hit a limit.</summary>
        public double Delta { get; }

        public SatisfactionReason Reason { get; }
        public Fixture Fixture { get; }
        public DateTime Date { get; }

        /// <summary>What was asked, for the request reasons.</summary>
        public RequestKind? Request { get; }

        /// <summary>The referee's grade, for <see cref="SatisfactionReason.Rated"/>.</summary>
        public PitchGrade? Grade { get; }

        public int Demerits { get; }
    }
}
