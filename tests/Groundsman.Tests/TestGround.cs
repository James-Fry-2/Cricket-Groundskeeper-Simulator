using Groundsman.Core.Content;
using Groundsman.Core.Strips;

namespace Groundsman.Tests;

internal static class TestGround
{
    /// <summary>
    /// Fixed test values, independent of the shipped content. All strips are the standard test
    /// loam; strip n starts at 22 + n % 5 surface, 4 more below.
    /// </summary>
    public static GroundSettings Settings { get; } = new GroundSettings(
        "Test Ground",
        new[] { "North End", "South End" },
        Enumerable.Range(1, 12)
            .Select(n => new StripSettings(new StripId(n), TestLoams.Standard.Id, 22 + n % 5, 26 + n % 5))
            .ToArray());
}
