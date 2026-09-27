namespace Groundsman.Core.Time
{
    /// <summary>How finely the player's turns split a day, from coarsest to finest.</summary>
    public enum DayPace
    {
        OffSeason = 0,
        InSeason = 1,
        FinalPrep = 2,
        MatchDay = 3,
    }
}
