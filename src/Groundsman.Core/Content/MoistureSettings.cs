namespace Groundsman.Core.Content
{
    public sealed class MoistureSettings
    {
        public MoistureSettings(
            double surfaceDepthMm,
            double subsurfaceDepthMm,
            double evaporationPerDegreeMm,
            double evaporationWindFactorPerKph,
            double evaporationPerSunshineHourMm)
        {
            CheckPositive("surfaceDepthMm", surfaceDepthMm);
            CheckPositive("subsurfaceDepthMm", subsurfaceDepthMm);
            CheckNotNegative("evaporation.perDegreeMm", evaporationPerDegreeMm);
            CheckNotNegative("evaporation.windFactorPerKph", evaporationWindFactorPerKph);
            CheckNotNegative("evaporation.perSunshineHourMm", evaporationPerSunshineHourMm);

            SurfaceDepthMm = surfaceDepthMm;
            SubsurfaceDepthMm = subsurfaceDepthMm;
            EvaporationPerDegreeMm = evaporationPerDegreeMm;
            EvaporationWindFactorPerKph = evaporationWindFactorPerKph;
            EvaporationPerSunshineHourMm = evaporationPerSunshineHourMm;
        }

        /// <summary>Depth of the surface layer, the part that dries first and that the ball meets.</summary>
        public double SurfaceDepthMm { get; }

        /// <summary>Depth of the layer below, down to about 100mm, where preparation wants moisture held.</summary>
        public double SubsurfaceDepthMm { get; }

        /// <summary>Potential evaporation per hour per °C above zero.</summary>
        public double EvaporationPerDegreeMm { get; }

        /// <summary>The temperature term is multiplied by 1 plus this for each km/h of wind.</summary>
        public double EvaporationWindFactorPerKph { get; }

        /// <summary>Potential evaporation added per hour of bright sunshine.</summary>
        public double EvaporationPerSunshineHourMm { get; }

        private static void CheckPositive(string field, double value)
        {
            if (value <= 0)
            {
                throw new ContentException($"moisture.{field} ({value}) must be above 0.");
            }
        }

        private static void CheckNotNegative(string field, double value)
        {
            if (value < 0)
            {
                throw new ContentException($"moisture.{field} ({value}) can't be negative.");
            }
        }
    }
}
