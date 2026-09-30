namespace Groundsman.Core
{
    /// <summary>Something the player should hear about this turn.</summary>
    public abstract class Notice
    {
    }

    /// <summary>A fixture's strip locked as its build-up began.</summary>
    public sealed class StripLockedNotice : Notice
    {
        public StripLockedNotice(FixtureView fixture, bool byDefault)
        {
            Fixture = fixture;
            ByDefault = byDefault;
        }

        public FixtureView Fixture { get; }

        /// <summary>No strip had been assigned, so the head groundsman chose one.</summary>
        public bool ByDefault { get; }
    }
}
