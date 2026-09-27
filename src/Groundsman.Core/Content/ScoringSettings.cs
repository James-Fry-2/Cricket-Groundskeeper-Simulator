namespace Groundsman.Core.Content
{
    public enum MatchMorningMiss
    {
        None = 0,
        SubsurfaceDry = 1,
        SubsurfaceWet = 2,
        SurfaceWet = 3,
    }

    /// <summary>
    /// Phase 2's stand-in for a pitch rating: moist at depth and dry on top on a match's first
    /// morning, as the research describes. Phase 3 replaces it with the referee's rating.
    /// </summary>
    public sealed class ScoringSettings
    {
        public ScoringSettings(double subsurfaceMin, double subsurfaceMax, double surfaceMax)
        {
            if (subsurfaceMin < 0 || subsurfaceMax <= subsurfaceMin)
            {
                throw new ContentException($"scoring.matchMorning.subsurfaceMax ({subsurfaceMax}) must be above subsurfaceMin ({subsurfaceMin}), which can't be negative.");
            }
            if (surfaceMax <= 0)
            {
                throw new ContentException($"scoring.matchMorning.surfaceMax ({surfaceMax}) must be above 0.");
            }

            SubsurfaceMin = subsurfaceMin;
            SubsurfaceMax = subsurfaceMax;
            SurfaceMax = surfaceMax;
        }

        public double SubsurfaceMin { get; }
        public double SubsurfaceMax { get; }
        public double SurfaceMax { get; }

        public bool IsOnTarget(double surface, double subsurface) => Judge(surface, subsurface) == MatchMorningMiss.None;

        /// <summary>The first way the strip misses, checking depth before the surface.</summary>
        public MatchMorningMiss Judge(double surface, double subsurface)
        {
            if (subsurface < SubsurfaceMin)
            {
                return MatchMorningMiss.SubsurfaceDry;
            }
            if (subsurface > SubsurfaceMax)
            {
                return MatchMorningMiss.SubsurfaceWet;
            }
            return surface >= SurfaceMax ? MatchMorningMiss.SurfaceWet : MatchMorningMiss.None;
        }
    }
}
