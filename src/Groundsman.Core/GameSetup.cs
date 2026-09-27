using System;
using System.Collections.Generic;
using Groundsman.Core.Content;
using Groundsman.Core.Time;

namespace Groundsman.Core
{
    public sealed class GameSetup
    {
        public GameSetup(GameContent content, GameTime start, IEnumerable<DateTime> matchDays)
        {
            Content = content;
            Start = start;
            MatchDays = new List<DateTime>(matchDays).AsReadOnly();
        }

        public GameContent Content { get; }
        public GameTime Start { get; }

        /// <summary>Supplied directly until fixtures exist.</summary>
        public IReadOnlyList<DateTime> MatchDays { get; }
    }
}
