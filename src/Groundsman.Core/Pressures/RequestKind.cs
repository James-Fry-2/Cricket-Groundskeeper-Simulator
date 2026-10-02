namespace Groundsman.Core.Pressures
{
    /// <summary>What a stakeholder asks of a fixture's pitch.</summary>
    public enum RequestKind
    {
        /// <summary>Grass and moisture for the seamers on day one.</summary>
        Green = 1,

        /// <summary>A dry surface that turns as the match goes on.</summary>
        Turning = 2,

        /// <summary>True bounce and carry for the quicks.</summary>
        Pace = 3,

        /// <summary>Little movement, true bounce: a batting pitch.</summary>
        Flat = 4,

        /// <summary>The board's: a four-day match still going on day four.</summary>
        LastsFourDays = 5,
    }
}
