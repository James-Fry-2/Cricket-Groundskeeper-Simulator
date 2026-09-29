using Groundsman.Core.Content;

namespace Groundsman.Tests;

internal static class TestMatch
{
    public static MatchSettings Settings { get; } = new MatchSettings(
        strengthScale: 0.8,
        seamWickets: 1.2,
        spinWickets: 1.2,
        unevenWickets: 2.0,
        deadWickets: 0.4,
        carryRuns: 0.4,
        unevenRuns: 0.3,
        deadRuns: 0.3,
        runsSpread: 0.35,
        declarationLead: 250,
        declarationFromDay: 3,
        rainRestartLoss: 0.5,
        tossBowlFirstSeamAbove: 6);
}
