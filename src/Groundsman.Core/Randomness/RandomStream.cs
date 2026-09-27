namespace Groundsman.Core.Randomness
{
    /// <summary>
    /// One independent stream per system. Values are fixed ids that feed seed derivation:
    /// never renumber or reuse one, or every stored replay and save changes.
    /// </summary>
    public enum RandomStream
    {
        Weather = 1,
        Forecast = 2,
        Readings = 3,
        Match = 4,
        Events = 5,
    }
}
