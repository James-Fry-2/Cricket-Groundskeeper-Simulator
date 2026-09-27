namespace Groundsman.Core.Content
{
    public sealed class TaskSettings
    {
        public TaskSettings(double waterMm)
        {
            if (waterMm <= 0)
            {
                throw new ContentException($"tasks.waterMm ({waterMm}) must be above 0.");
            }

            WaterMm = waterMm;
        }

        /// <summary>Water one watering puts on a strip, in mm.</summary>
        public double WaterMm { get; }
    }
}
