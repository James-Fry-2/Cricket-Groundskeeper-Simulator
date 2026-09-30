using System;
using System.Globalization;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;

namespace Groundsman.Core
{
    /// <summary>A match on the season's list: its date, format, visitors and whether it's televised.</summary>
    public sealed class Fixture
    {
        /// <param name="presetStrip">A strip the season file fixes, which starts as the assignment; null leaves it to the player.</param>
        public Fixture(DateTime start, FormatSettings format, StripId? presetStrip, TeamSettings opponent, bool televised = false)
        {
            Start = start.Date;
            Format = format;
            PresetStrip = presetStrip;
            Opponent = opponent;
            Televised = televised;
        }

        /// <summary>The start date, which is unique because fixtures never overlap.</summary>
        public string Id => Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public DateTime Start { get; }
        public FormatSettings Format { get; }
        public StripId? PresetStrip { get; }
        public TeamSettings Opponent { get; }
        public bool Televised { get; }

        public int Days => Format.Days;
        public DateTime End => Start.AddDays(Days - 1);
    }
}
