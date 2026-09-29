using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests;

internal static class TestContent
{
    /// <summary>Surface moisture points one watering adds, given the test surface depth.</summary>
    public static double WaterGain => Tasks.WaterMm / TestMoisture.Settings.SurfaceDepthMm * 100;

    public static TaskSettings Tasks { get; } = new TaskSettings(waterMm: 1);

    public static CoverSettings Covers { get; } = new CoverSettings(count: 4, evaporationFactor: 0.3);

    public static ReadingSettings Readings { get; } = new ReadingSettings(
        moistureProbeWidth: 8,
        moistureProbeMissRate: 0.08,
        soilCoreWidth: 6,
        soilCoreMissRate: 0.05,
        feelJudgementSd: 2.5,
        new[] { new FeelBand("dry", 0, 16), new FeelBand("damp", 16, 26), new FeelBand("wet", 26, 50) },
        widenPerDay: 1.0,
        widenPerMmWater: 0.5);

    public static GameContent Content { get; } = new GameContent(TestCalendar.Settings, TestClimate.Settings, TestGround.Settings, TestLoams.All, TestMoisture.Settings, Covers, Tasks, TestStaff.Settings, Readings, TestForecast.Settings, TestFormats.All, TestTeams.All, TestTeams.Home.Id, TestGrass.Settings, TestRolling.Rollers, TestRolling.Compaction, TestPitch.Settings, TestWear.Settings, TestMatch.Settings, TestCommentary.Settings);

    /// <summary>A game whose match days are one-day fixtures on strip 1, for tests that only care about dates.</summary>
    public static GameSetup Setup(GameTime start, IEnumerable<DateTime>? matchDays = null, ulong seed = 1, ReadingSettings? readings = null) =>
        new GameSetup(
            readings == null ? Content : WithReadings(readings),
            start,
            (matchDays ?? Array.Empty<DateTime>()).Select(d => new Fixture(d, TestFormats.OneDay, new StripId(1), TestTeams.Opponent)).ToArray(),
            seed);

    /// <summary>Test readings with nothing left to chance except where the true value sits.</summary>
    public static ReadingSettings ExactReadings { get; } = new ReadingSettings(
        moistureProbeWidth: 8,
        moistureProbeMissRate: 0,
        soilCoreWidth: 6,
        soilCoreMissRate: 0,
        feelJudgementSd: 0,
        Readings.FeelBands,
        widenPerDay: Readings.WidenPerDay,
        widenPerMmWater: Readings.WidenPerMmWater);

    private static GameContent WithReadings(ReadingSettings readings) => new GameContent(
        TestCalendar.Settings, TestClimate.Settings, TestGround.Settings, TestLoams.All, TestMoisture.Settings, Covers, Tasks, TestStaff.Settings, readings, TestForecast.Settings, TestFormats.All, TestTeams.All, TestTeams.Home.Id, TestGrass.Settings, TestRolling.Rollers, TestRolling.Compaction, TestPitch.Settings, TestWear.Settings, TestMatch.Settings, TestCommentary.Settings);
}
