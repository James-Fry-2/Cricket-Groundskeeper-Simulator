using System;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;

namespace Groundsman.Core
{
    /// <summary>A match on the season's list, with its format, the visitors and the strip it's played on.</summary>
    public sealed class Fixture
    {
        public Fixture(DateTime start, FormatSettings format, StripId strip, TeamSettings opponent)
        {
            Start = start.Date;
            Format = format;
            Strip = strip;
            Opponent = opponent;
        }

        public DateTime Start { get; }
        public FormatSettings Format { get; }
        public StripId Strip { get; }
        public TeamSettings Opponent { get; }

        public int Days => Format.Days;
        public DateTime End => Start.AddDays(Days - 1);
    }
}
