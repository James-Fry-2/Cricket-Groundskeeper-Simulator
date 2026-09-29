using Groundsman.Core.Strips;

namespace Groundsman.Core.Match
{
    /// <summary>A match-day turn and the jobs Law 9 allows on the match strip during it.</summary>
    public sealed class IntervalView
    {
        public IntervalView(string name, StripId strip, bool canClean, bool canFill)
        {
            Name = name;
            Strip = strip;
            CanClean = canClean;
            CanFill = canFill;
        }

        /// <summary>"Before play", or the break's name from the format, such as "Tea".</summary>
        public string Name { get; }

        public StripId Strip { get; }
        public bool CanClean { get; }

        /// <summary>Only at close of play with more of the match to come.</summary>
        public bool CanFill { get; }
    }
}
