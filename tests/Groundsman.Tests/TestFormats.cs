using Groundsman.Core.Content;

namespace Groundsman.Tests;

internal static class TestFormats
{
    public static FormatSettings FourDay { get; } = new FormatSettings(
        "fourDay", "Four-day", days: 4, inningsPerSide: 2, oversPerInnings: null, oversPerDay: 96, runsPerOver: 3.2, wicketsPerOver: 0.07,
        new[] { new PlaySession(11, 13), new PlaySession(14, 16), new PlaySession(16, 18) },
        new[] { 8, 13, 16, 18 });

    /// <summary>Shares the four-day match-day hours so date-only tests keep their turn times.</summary>
    public static FormatSettings OneDay { get; } = new FormatSettings(
        "oneDay", "One-day", days: 1, inningsPerSide: 1, oversPerInnings: 50, oversPerDay: null, runsPerOver: 5.2, wicketsPerOver: 0.14,
        new[] { new PlaySession(11, 14), new PlaySession(15, 18) },
        new[] { 8, 13, 16, 18 });

    public static FormatSettings T20 { get; } = new FormatSettings(
        "t20", "T20", days: 1, inningsPerSide: 1, oversPerInnings: 20, oversPerDay: null, runsPerOver: 8.0, wicketsPerOver: 0.3,
        new[] { new PlaySession(18, 20), new PlaySession(20, 22) },
        new[] { 8, 18, 20, 22 });

    public static IReadOnlyList<FormatSettings> All { get; } = new[] { FourDay, OneDay, T20 };
}
