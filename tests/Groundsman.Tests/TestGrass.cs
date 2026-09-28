using Groundsman.Core.Content;

namespace Groundsman.Tests;

internal static class TestGrass
{
    public static GrassSettings Settings { get; } = new GrassSettings(
        startingCover: 85,
        startingHeightMm: 15,
        startingRootDepthMm: 60,
        minTemperature: 5,
        optimumTemperature: 18,
        maxTemperature: 30,
        heightPerDayMm: 1.2,
        coverPerDay: 1.0,
        maxCover: 95,
        rootsPerDayMm: 1.0,
        maxRootDepthMm: 100,
        droughtWaterBelow: 0.15,
        droughtCoverLossPerDay: 0.5,
        minMowHeightMm: 3,
        maxMowHeightMm: 50,
        scalpShare: 0.34,
        scalpCoverLossPerMm: 2.0);
}
