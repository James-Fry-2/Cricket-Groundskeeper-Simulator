namespace Groundsman.Core.Simulation
{
    /// <summary>
    /// The fixed order systems run in each hour. The order is part of the rules: changing it
    /// changes every replay.
    /// </summary>
    public enum TickStep
    {
        Weather = 0,
        Covers = 1,
        Moisture = 2,
        Grass = 3,
        Tasks = 4,
        Match = 5,
        WearAndRecovery = 6,
    }
}
