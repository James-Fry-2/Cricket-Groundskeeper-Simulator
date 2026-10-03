using Groundsman.Core.Content;

namespace Groundsman.Tests;

internal static class TestPitch
{
    public static PitchSettings Settings { get; } = new PitchSettings(
        grassReferenceMm: 10,
        grassCushion: 0.4,
        lastingDeadening: 3,
        bounceDepthBase: 0.6,
        bounceClayReference: 30,
        bounceClayExponent: 0.5,
        consistencyDamageWeight: 0.6,
        consistencyLooseBelow: 0.65,
        consistencyLooseWeight: 1.0,
        consistencyCrackWeight: 0.4,
        consistencyFootholeWeight: 0.4,
        consistencySurfaceWearWeight: 0.2,
        consistencyLastingWeight: 1.0,
        consistencyEndsWeight: 0.15,
        seamBase: 0.3,
        seamWetWeight: 0.7,
        spinDryWeight: 0.6,
        spinCrackWeight: 0.3,
        spinGrassWeight: 0.4,
        spinRoughWeight: 0.5,
        spinSurfaceWearWeight: 0.4);
}
