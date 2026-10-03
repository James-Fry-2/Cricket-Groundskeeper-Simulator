using Groundsman.Core.Content;
using Groundsman.Core.Pressures;

namespace Groundsman.Tests;

internal static class TestStakeholders
{
    /// <summary>Fixed test values, independent of the shipped content.</summary>
    public static StakeholderSettings Settings { get; } = With();

    public static StakeholderSettings With(double captainChance = 0.6, double boardChance = 0.3) => new StakeholderSettings(
        startingSatisfaction: 50,
        requestDaysBeforeLock: 5,
        captainRequestChance: captainChance,
        captainCharacters: new Dictionary<string, IReadOnlyDictionary<RequestKind, double>>
        {
            ["fourDay"] = new Dictionary<RequestKind, double> { [RequestKind.Green] = 2, [RequestKind.Turning] = 1, [RequestKind.Pace] = 1 },
            ["oneDay"] = new Dictionary<RequestKind, double> { [RequestKind.Pace] = 1, [RequestKind.Flat] = 1 },
            ["t20"] = new Dictionary<RequestKind, double> { [RequestKind.Flat] = 2, [RequestKind.Pace] = 1 },
        },
        boardRequestChance: boardChance,
        captainAnswers: new AnswerEffects(delivered: 12, notDelivered: -15, declined: -4, ignored: -6),
        boardAnswers: new AnswerEffects(delivered: 8, notDelivered: -12, declined: -3, ignored: -5),
        homeWin: 3,
        homeLoss: -3,
        dayFourReached: 4,
        shortFourDay: -6,
        noResult: -4,
        televisedCentre: 3,
        televisedOffCentre: -6,
        perDemerit: -10,
        veryGood: 6,
        satisfactory: 2,
        unsatisfactory: -8,
        unfit: -20,
        greenSeamDayOne: 5,
        turningSpinLastDay: 4,
        paceCarry: 5.5,
        trueConsistency: 8,
        flatMovementBelow: 3,
        contentFrom: 55,
        delightedFrom: 75,
        uneasyFrom: 40,
        wearLightFrom: 0.01,
        wearWornFrom: 0.03,
        wearHeavyFrom: 0.06);
}
