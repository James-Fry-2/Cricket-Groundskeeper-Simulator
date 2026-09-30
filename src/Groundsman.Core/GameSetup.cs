using System;
using System.Collections.Generic;
using System.Linq;
using Groundsman.Core.Content;
using Groundsman.Core.Time;

namespace Groundsman.Core
{
    public sealed class GameSetup
    {
        public GameSetup(GameContent content, GameTime start, IEnumerable<Fixture> fixtures, ulong seed)
        {
            var list = fixtures.OrderBy(f => f.Start).ToList();
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Strip.Number > content.Ground.Strips.Count)
                {
                    throw new ContentException($"season.fixtures[{i}].strip ({list[i].Strip.Number}) isn't on the ground, which has {content.Ground.Strips.Count} strips.");
                }
            }

            Content = content;
            Start = start;
            Fixtures = list.AsReadOnly();
            Seed = seed;
        }

        public GameContent Content { get; }
        public GameTime Start { get; }
        public IReadOnlyList<Fixture> Fixtures { get; }
        public ulong Seed { get; }

        public IReadOnlyList<DateTime> MatchDays => MatchDaysOf(Fixtures);

        internal static IReadOnlyList<DateTime> MatchDaysOf(IEnumerable<Fixture> fixtures)
        {
            var days = new List<DateTime>();
            foreach (var fixture in fixtures)
            {
                for (var date = fixture.Start; date <= fixture.End; date = date.AddDays(1))
                {
                    days.Add(date);
                }
            }
            return days.AsReadOnly();
        }
    }
}
