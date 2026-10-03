using System.Collections.Generic;
using Groundsman.Core.Strips;

namespace Groundsman.Core.Pressures
{
    public enum Mood
    {
        Unhappy = 1,
        Uneasy = 2,
        Content = 3,
        Delighted = 4,
    }

    /// <summary>How worn a strip is at the end of the season, from the head groundsman's walk.</summary>
    public enum SquareWear
    {
        Fresh = 1,
        Light = 2,
        Worn = 3,
        Heavy = 4,
    }

    /// <summary>The end of the season: what everyone thought, the record, and the state of the square.</summary>
    public sealed class SeasonReview
    {
        public SeasonReview(
            IReadOnlyList<StakeholderReview> stakeholders,
            int veryGood,
            int satisfactory,
            int unsatisfactory,
            int unfit,
            int wins,
            int losses,
            int draws,
            int noResults,
            int demerits,
            bool banned,
            IReadOnlyList<StripReview> square)
        {
            Stakeholders = stakeholders;
            VeryGood = veryGood;
            Satisfactory = satisfactory;
            Unsatisfactory = unsatisfactory;
            Unfit = unfit;
            Wins = wins;
            Losses = losses;
            Draws = draws;
            NoResults = noResults;
            Demerits = demerits;
            Banned = banned;
            Square = square;
        }

        /// <summary>The captain, the board and the referee, in that order.</summary>
        public IReadOnlyList<StakeholderReview> Stakeholders { get; }

        // Pitch ratings this season.
        public int VeryGood { get; }
        public int Satisfactory { get; }
        public int Unsatisfactory { get; }
        public int Unfit { get; }

        // The county's results; ties count as draws.
        public int Wins { get; }
        public int Losses { get; }
        public int Draws { get; }
        public int NoResults { get; }

        /// <summary>Demerits within the rolling window.</summary>
        public int Demerits { get; }

        public bool Banned { get; }

        /// <summary>
        /// Every strip, from the head groundsman's walk of the square once the season is over.
        /// It's the one look at lasting wear the player gets, after the last decision it could
        /// have informed.
        /// </summary>
        public IReadOnlyList<StripReview> Square { get; }
    }

    public sealed class StakeholderReview
    {
        public StakeholderReview(Stakeholder stakeholder, double satisfaction, Mood mood, IReadOnlyList<ReasonSummary> topReasons)
        {
            Stakeholder = stakeholder;
            Satisfaction = satisfaction;
            Mood = mood;
            TopReasons = topReasons;
        }

        public Stakeholder Stakeholder { get; }
        public double Satisfaction { get; }
        public Mood Mood { get; }

        /// <summary>The reasons that moved them most over the season, biggest first.</summary>
        public IReadOnlyList<ReasonSummary> TopReasons { get; }
    }

    /// <summary>All of a stakeholder's changes for one reason (and request kind) added up.</summary>
    public sealed class ReasonSummary
    {
        public ReasonSummary(SatisfactionReason reason, RequestKind? request, double total, int count)
        {
            Reason = reason;
            Request = request;
            Total = total;
            Count = count;
        }

        public SatisfactionReason Reason { get; }
        public RequestKind? Request { get; }
        public double Total { get; }
        public int Count { get; }
    }

    public sealed class StripReview
    {
        public StripReview(StripId strip, int matches, SquareWear wear)
        {
            Strip = strip;
            Matches = matches;
            Wear = wear;
        }

        public StripId Strip { get; }

        /// <summary>Matches played on it this season.</summary>
        public int Matches { get; }

        public SquareWear Wear { get; }
    }
}
