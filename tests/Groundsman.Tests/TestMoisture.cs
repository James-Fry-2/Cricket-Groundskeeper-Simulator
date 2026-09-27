using Groundsman.Core.Content;

namespace Groundsman.Tests;

internal static class TestMoisture
{
    public static MoistureSettings Settings { get; } = new MoistureSettings(
        surfaceDepthMm: 25,
        subsurfaceDepthMm: 75,
        evaporationPerDegreeMm: 0.004,
        evaporationWindFactorPerKph: 0.02,
        evaporationPerSunshineHourMm: 0.2);
}
