using System;
using System.Collections.Generic;
using System.Linq;
using Groundsman.Core.Strips;

namespace Groundsman.Core.Schedule
{
    /// <summary>
    /// Which strip each fixture is played on. The player assigns and changes strips freely until
    /// a fixture's build-up starts, when its strip locks; an unassigned fixture then gets the
    /// strip rested longest. A strip is booked from the start of a build-up to the match's last
    /// day, and can't be booked for two fixtures at once.
    /// </summary>
    internal sealed class StripBook
    {
        private readonly IReadOnlyList<Fixture> _fixtures;
        private readonly int _lockDaysOut;
        private readonly int _stripCount;
        private readonly Dictionary<Fixture, StripId> _strips = new Dictionary<Fixture, StripId>();
        private readonly HashSet<Fixture> _locked = new HashSet<Fixture>();
        private readonly List<Notice> _notices = new List<Notice>();

        public StripBook(IReadOnlyList<Fixture> fixtures, int lockDaysOut, int stripCount)
        {
            _fixtures = fixtures;
            _lockDaysOut = lockDaysOut;
            _stripCount = stripCount;
            foreach (var fixture in fixtures)
            {
                if (fixture.PresetStrip is { } preset)
                {
                    _strips[fixture] = preset;
                }
            }
        }

        /// <summary>Locks made since <see cref="ClearNotices"/>.</summary>
        public IReadOnlyList<Notice> Notices => _notices;

        public void ClearNotices() => _notices.Clear();

        public StripId? StripOf(Fixture fixture) => _strips.TryGetValue(fixture, out var strip) ? strip : (StripId?)null;

        public DateTime LocksOn(Fixture fixture) => fixture.Start.AddDays(-_lockDaysOut);

        public FixtureView View(Fixture fixture) => new FixtureView(fixture, StripOf(fixture), LocksOn(fixture), _locked.Contains(fixture));

        public CommandResult Assign(string fixtureId, StripId strip)
        {
            var fixture = _fixtures.FirstOrDefault(f => f.Id == fixtureId);
            if (fixture == null)
            {
                return CommandResult.Rejected($"There's no fixture {fixtureId}.");
            }
            if (strip.Number < 1 || strip.Number > _stripCount)
            {
                return CommandResult.Rejected($"{strip} isn't on this square.");
            }
            if (_locked.Contains(fixture))
            {
                return CommandResult.Rejected($"The strip for {fixtureId} is locked: its build-up has started.");
            }
            if (Clash(fixture, strip) is { } other)
            {
                return CommandResult.Rejected($"{strip} is booked for {other.Id}, and the two build-ups would overlap.");
            }

            _strips[fixture] = strip;
            return CommandResult.Ok();
        }

        /// <summary>Locks every fixture whose build-up has started by <paramref name="today"/>.</summary>
        public void LockDue(DateTime today)
        {
            foreach (var fixture in _fixtures)
            {
                if (!_locked.Contains(fixture) && LocksOn(fixture) <= today)
                {
                    Lock(fixture);
                }
            }
        }

        /// <summary>The fixture's strip, locking it now if the build-up was skipped past.</summary>
        public StripId StripFor(Fixture fixture)
        {
            if (!_locked.Contains(fixture))
            {
                Lock(fixture);
            }
            return _strips[fixture];
        }

        private void Lock(Fixture fixture)
        {
            var byDefault = !_strips.ContainsKey(fixture);
            if (byDefault)
            {
                _strips[fixture] = Default(fixture);
            }
            _locked.Add(fixture);
            _notices.Add(new StripLockedNotice(View(fixture), byDefault));
        }

        // Unused strips first, then the one whose last match before this fixture ended longest ago.
        private StripId Default(Fixture fixture)
        {
            var best = new StripId(1);
            var bestLastUsed = DateTime.MaxValue;
            for (var n = 1; n <= _stripCount; n++)
            {
                var strip = new StripId(n);
                if (Clash(fixture, strip) != null)
                {
                    continue;
                }
                var lastUsed = _strips
                    .Where(s => s.Value == strip && s.Key.End < fixture.Start)
                    .Select(s => s.Key.End)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max();
                if (lastUsed < bestLastUsed)
                {
                    best = strip;
                    bestLastUsed = lastUsed;
                }
            }
            return best;
        }

        private Fixture? Clash(Fixture fixture, StripId strip) =>
            _strips
                .Where(s => s.Key != fixture && s.Value == strip && Overlaps(fixture, s.Key))
                .Select(s => s.Key)
                .OrderBy(f => f.Start)
                .FirstOrDefault();

        private bool Overlaps(Fixture a, Fixture b) => LocksOn(a) <= b.End && LocksOn(b) <= a.End;
    }
}
