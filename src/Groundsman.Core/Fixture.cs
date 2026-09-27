using System;
using Groundsman.Core.Strips;

namespace Groundsman.Core
{
    /// <summary>A match on the season's list, with the strip it's played on.</summary>
    public sealed class Fixture
    {
        public Fixture(DateTime start, int days, StripId strip)
        {
            Start = start.Date;
            Days = days;
            Strip = strip;
        }

        public DateTime Start { get; }
        public int Days { get; }
        public StripId Strip { get; }

        public DateTime End => Start.AddDays(Days - 1);
    }
}
