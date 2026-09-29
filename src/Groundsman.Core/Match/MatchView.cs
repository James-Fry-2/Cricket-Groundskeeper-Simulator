using System.Collections.Generic;

namespace Groundsman.Core.Match
{
    public enum ResultKind
    {
        Win = 1,
        Tie = 2,
        Draw = 3,
        NoResult = 4,
    }

    public sealed class ResultView
    {
        public ResultView(ResultKind kind, string? winner, string text)
        {
            Kind = kind;
            Winner = winner;
            Text = text;
        }

        public ResultKind Kind { get; }

        /// <summary>The winning side's name; null unless someone won.</summary>
        public string? Winner { get; }

        /// <summary>For example "Kestrelshire won by 45 runs".</summary>
        public string Text { get; }
    }

    public sealed class InningsView
    {
        public InningsView(string batting, int runs, int wickets, double overs, bool declared)
        {
            Batting = batting;
            Runs = runs;
            Wickets = wickets;
            Overs = overs;
            Declared = declared;
        }

        public string Batting { get; }
        public int Runs { get; }
        public int Wickets { get; }
        public double Overs { get; }
        public bool Declared { get; }
    }

    public sealed class CommentaryLine
    {
        public CommentaryLine(Groundsman.Core.Time.GameTime hour, string? eventId, string? causeId, string text)
        {
            Hour = hour;
            EventId = eventId;
            CauseId = causeId;
            Text = text;
        }

        public Groundsman.Core.Time.GameTime Hour { get; }

        /// <summary>The event that raised the line, or null for a break's score line.</summary>
        public string? EventId { get; }

        /// <summary>The cause the line names, if any.</summary>
        public string? CauseId { get; }

        public string Text { get; }
    }

    /// <summary>The scoreboard and commentary: public, unlike the pitch behaviour behind them.</summary>
    public sealed class MatchView
    {
        public MatchView(Fixture fixture, IReadOnlyList<InningsView> innings, bool finished, ResultView? result, IReadOnlyList<CommentaryLine> commentary)
        {
            Commentary = commentary;
            Fixture = fixture;
            Innings = innings;
            Finished = finished;
            Result = result;
        }

        public Fixture Fixture { get; }
        public IReadOnlyList<InningsView> Innings { get; }
        public bool Finished { get; }
        public ResultView? Result { get; }
        public IReadOnlyList<CommentaryLine> Commentary { get; }
    }
}
