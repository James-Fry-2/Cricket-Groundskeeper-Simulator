using Groundsman.Core.Content;

namespace Groundsman.Tests;

internal static class TestTeams
{
    public static TeamSettings Home { get; } = new TeamSettings("home", "Test County", batting: 60, bowling: 60, new AttackProfile(seam: 0.7, leftArm: 0.2, heavyFooted: 0.5));

    public static TeamSettings Opponent { get; } = new TeamSettings("away", "Test Visitors", batting: 60, bowling: 60, new AttackProfile(seam: 0.6, leftArm: 0.3, heavyFooted: 0.4));

    public static IReadOnlyList<TeamSettings> All { get; } = new[] { Home, Opponent };
}
