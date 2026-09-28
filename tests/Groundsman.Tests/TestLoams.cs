using Groundsman.Core.Content;

namespace Groundsman.Tests;

internal static class TestLoams
{
    public static LoamSettings Standard { get; } = new LoamSettings(
        "standard", "Standard test loam", clayPercent: 30,
        saturation: 40, fieldCapacity: 30, airDry: 6,
        surfaceDrainageRate: 0.08, subsurfaceDrainageRate: 0.02, capillaryRate: 0.01,
        crackingTendency: 0.5,
        rollingWindowMin: 18, rollingWindowMax: 26);

    public static LoamSettings Heavy { get; } = new LoamSettings(
        "heavy", "Heavy test loam", clayPercent: 40,
        saturation: 44, fieldCapacity: 34, airDry: 8,
        surfaceDrainageRate: 0.04, subsurfaceDrainageRate: 0.01, capillaryRate: 0.008,
        crackingTendency: 0.8,
        rollingWindowMin: 20, rollingWindowMax: 29);

    public static IReadOnlyList<LoamSettings> All { get; } = new[] { Standard, Heavy };
}
