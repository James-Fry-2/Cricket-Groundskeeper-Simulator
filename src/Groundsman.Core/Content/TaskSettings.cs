namespace Groundsman.Core.Content
{
    public sealed class TaskSettings
    {
        public TaskSettings(double waterSurfaceGain)
        {
            if (waterSurfaceGain <= 0)
            {
                throw new ContentException($"tasks.waterSurfaceGain ({waterSurfaceGain}) must be above 0.");
            }

            WaterSurfaceGain = waterSurfaceGain;
        }

        /// <summary>Percentage points of surface moisture one watering adds.</summary>
        public double WaterSurfaceGain { get; }
    }
}
