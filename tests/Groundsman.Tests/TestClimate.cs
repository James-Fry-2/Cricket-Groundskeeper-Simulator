using Groundsman.Core.Content;

namespace Groundsman.Tests;

internal static class TestClimate
{
    /// <summary>The same plain month all year, independent of the shipped content.</summary>
    public static ClimateSettings Settings { get; } = new ClimateSettings(
        rainDayThresholdMm: 1,
        wetDayPersistence: 0.3,
        rainSpellMinHours: 2,
        rainSpellMaxHours: 6,
        temperatureAnomalySd: 2,
        temperatureAnomalyPersistence: 0.7,
        windVariability: 0.3,
        wetDaySunshineFactor: 0.4,
        cloudVariability: 1.2,
        cloudPersistence: 0.5,
        sunshineRangeEffect: 1.2,
        rainCooling: 1.5,
        wetDayWindFactor: 1.3,
        Enumerable.Range(1, 12)
            .Select(m => new MonthClimate(m, meanTemperature: 12, dailyRange: 8, rainDays: 10, rainTotalMm: 50, sunshineHours: 150, meanWindKph: 15, daylightHours: 14, sunWarmth: 2))
            .ToArray());
}
