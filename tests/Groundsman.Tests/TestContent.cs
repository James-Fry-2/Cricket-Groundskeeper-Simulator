using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Time;

namespace Groundsman.Tests;

internal static class TestContent
{
    /// <summary>Surface moisture points one watering adds, given the test surface depth.</summary>
    public static double WaterGain => Tasks.WaterMm / TestMoisture.Settings.SurfaceDepthMm * 100;

    public static TaskSettings Tasks { get; } = new TaskSettings(waterMm: 1);

    public static CoverSettings Covers { get; } = new CoverSettings(count: 4, evaporationFactor: 0.3);

    public static ReadingSettings Readings { get; } = new ReadingSettings(moistureProbeWidth: 8);

    public static GameContent Content { get; } = new GameContent(TestCalendar.Settings, TestClimate.Settings, TestGround.Settings, TestLoams.All, TestMoisture.Settings, Covers, Tasks, Readings);

    public static GameSetup Setup(GameTime start, IEnumerable<DateTime>? matchDays = null, ulong seed = 1) =>
        new GameSetup(Content, start, matchDays ?? Array.Empty<DateTime>(), seed);
}
