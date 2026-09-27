namespace Groundsman.Core.Content
{
    public sealed class ReadingSettings
    {
        public ReadingSettings(double moistureProbeWidth)
        {
            if (moistureProbeWidth <= 0)
            {
                throw new ContentException($"readings.moistureProbeWidth ({moistureProbeWidth}) must be above 0.");
            }

            MoistureProbeWidth = moistureProbeWidth;
        }

        /// <summary>Width in percentage points of a moisture probe reading's range.</summary>
        public double MoistureProbeWidth { get; }
    }
}
