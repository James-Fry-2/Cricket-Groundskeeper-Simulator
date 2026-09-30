using System;
using System.Collections.Generic;

namespace Groundsman.Core.Content
{
    /// <summary>When a new game starts, and the season's fixtures.</summary>
    public sealed class SeasonSettings
    {
        public SeasonSettings(DateTime start, IReadOnlyList<Fixture> fixtures)
        {
            for (var i = 0; i < fixtures.Count; i++)
            {
                var fixture = fixtures[i];
                var path = $"season.fixtures[{i}]";
                if (fixture.PresetStrip is { } preset && preset.Number < 1)
                {
                    throw new ContentException($"{path}.strip ({preset.Number}) must be a strip number from 1.");
                }
                if (fixture.Start < start.Date)
                {
                    throw new ContentException($"{path} ({fixture.Start:yyyy-MM-dd}) starts before the game does ({start:yyyy-MM-dd}).");
                }
                if (i > 0 && fixture.Start <= fixtures[i - 1].End)
                {
                    throw new ContentException($"{path} ({fixture.Start:yyyy-MM-dd}) must start after the previous fixture ends: fixtures run in date order without overlapping.");
                }
            }

            Start = start.Date;
            Fixtures = new List<Fixture>(fixtures).AsReadOnly();
        }

        public DateTime Start { get; }
        public IReadOnlyList<Fixture> Fixtures { get; }

        public IReadOnlyList<DateTime> MatchDays => GameSetup.MatchDaysOf(Fixtures);
    }
}
