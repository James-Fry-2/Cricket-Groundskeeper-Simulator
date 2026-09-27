using Groundsman.Core.Content;

namespace Groundsman.Tests;

internal static class TestForecast
{
    public static ForecastSettings Settings { get; } = new ForecastSettings(
        days: 7,
        rangeSpreads: 1.5,
        rainErrorSd: 1.5,
        rainGrowthPerDay: 0.4,
        maxTemperatureErrorSd: 1.0,
        maxTemperatureGrowthPerDay: 0.3);
}
