namespace Groundsman.Core.Content
{
    public sealed class CoverSettings
    {
        public CoverSettings(int count, double evaporationFactor)
        {
            if (count < 0)
            {
                throw new ContentException($"covers.count ({count}) can't be negative.");
            }
            if (evaporationFactor < 0 || evaporationFactor > 1)
            {
                throw new ContentException($"covers.evaporationFactor ({evaporationFactor}) must be from 0 to 1.");
            }

            Count = count;
            EvaporationFactor = evaporationFactor;
        }

        /// <summary>Strip covers the ground owns.</summary>
        public int Count { get; }

        /// <summary>Evaporation under a cover as a share of an open strip's.</summary>
        public double EvaporationFactor { get; }
    }
}
