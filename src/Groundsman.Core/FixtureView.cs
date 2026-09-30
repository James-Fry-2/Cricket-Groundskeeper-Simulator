using System;
using Groundsman.Core.Strips;

namespace Groundsman.Core
{
    /// <summary>A fixture with the strip assigned to it.</summary>
    public sealed class FixtureView
    {
        public FixtureView(Fixture fixture, StripId? strip, DateTime locksOn, bool locked)
        {
            Fixture = fixture;
            Strip = strip;
            LocksOn = locksOn;
            Locked = locked;
        }

        public Fixture Fixture { get; }
        public string Id => Fixture.Id;

        /// <summary>Null until the player assigns one, or the lock assigns a default.</summary>
        public StripId? Strip { get; }

        /// <summary>The day the build-up starts, after which the strip can't be changed.</summary>
        public DateTime LocksOn { get; }

        public bool Locked { get; }
    }
}
