using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Time;

namespace Groundsman.Tests;

internal static class TestContent
{
    public static TaskSettings Tasks { get; } = new TaskSettings(waterSurfaceGain: 4);

    public static ReadingSettings Readings { get; } = new ReadingSettings(moistureProbeWidth: 8);

    public static GameContent Content { get; } = new GameContent(TestCalendar.Settings, TestClimate.Settings, TestGround.Settings, Tasks, Readings);

    public static GameSetup Setup(GameTime start, IEnumerable<DateTime>? matchDays = null, ulong seed = 1) =>
        new GameSetup(Content, start, matchDays ?? Array.Empty<DateTime>(), seed);
}
