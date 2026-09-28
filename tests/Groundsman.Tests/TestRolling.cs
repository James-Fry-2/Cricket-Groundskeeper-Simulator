using Groundsman.Core.Content;

namespace Groundsman.Tests;

internal static class TestRolling
{
    public static RollerSettings Light { get; } = new RollerSettings("light", "Light roller", compactionPerHour: 0.04, wetDamagePerHour: 0.01, overuseAbove: 1.0, overuseDamagePerHour: 0);
    public static RollerSettings Medium { get; } = new RollerSettings("medium", "Medium roller", compactionPerHour: 0.08, wetDamagePerHour: 0.03, overuseAbove: 1.0, overuseDamagePerHour: 0);
    public static RollerSettings Heavy { get; } = new RollerSettings("heavy", "Heavy roller", compactionPerHour: 0.12, wetDamagePerHour: 0.08, overuseAbove: 0.8, overuseDamagePerHour: 0.05);

    public static IReadOnlyList<RollerSettings> Rollers { get; } = new[] { Light, Medium, Heavy };

    public static CompactionSettings Compaction { get; } = new CompactionSettings(
        startingCompaction: 0.55,
        startingStructureDamage: 0,
        minRollingMinutes: 5,
        maxRollingMinutes: 120,
        wetDamagePerPoint: 0.1,
        hardnessDryWeight: 0.7,
        hardnessClayReference: 30,
        hardnessClayExponent: 0.5);
}
