using Groundsman.Core.Content;

namespace Groundsman.Tests;

internal static class TestWear
{
    public static WearSettings Settings { get; } = new WearSettings(
        resistanceCompactionWeight: 0.5,
        resistanceClayWeight: 0.3,
        resistanceClayReference: 30,
        resistanceRootWeight: 0.2,
        resistanceWetLoss: 0.5,
        footholesPerOver: 0.004,
        heavyFootedExtra: 1.0,
        roughPerOver: 0.003,
        leftArmExtra: 0.5,
        surfaceWearPerOver: 0.002,
        dryDustExtra: 0.5,
        coverLossPerOver: 0.05,
        wetPlay: 2.0,
        crackStartDryness: 0.6,
        cracksPerHour: 0.01,
        crackDamageExtra: 2.0,
        cracksClosePerHour: 0.02,
        recoveryPerDay: 0.02,
        repairedRecoveryPerDay: 0.08,
        repairFills: 0.6,
        cleanShare: 0.1,
        fillShare: 0.4,
        neighbourShare: 0.25,
        lastingShare: 0.2,
        lastingResistanceLoss: 0.5,
        endsLossPerWear: 4,
        endsEstablishDays: 18,
        endsUnrepairedDays: 45,
        endsGerminateShare: 0.3,
        endsResistanceLoss: 0.4);
}
