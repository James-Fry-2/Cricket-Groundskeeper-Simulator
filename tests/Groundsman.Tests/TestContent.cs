using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Time;

namespace Groundsman.Tests;

internal static class TestContent
{
    public static TaskSettings Tasks { get; } = new TaskSettings(waterSurfaceGain: 4);

    public static GameContent Content { get; } = new GameContent(TestCalendar.Settings, TestGround.Settings, Tasks);

    public static GameSetup Setup(GameTime start, params DateTime[] matchDays) => new GameSetup(Content, start, matchDays);
}
