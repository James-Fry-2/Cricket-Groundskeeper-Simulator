namespace Groundsman.Core.Content
{
    public sealed class MoistureSettings
    {
        public MoistureSettings(
            double surfaceDepthMm,
            double subsurfaceDepthMm,
            double evaporationPerDegreeMm,
            double evaporationWindFactorPerKph,
            double evaporationPerSunshineHourMm,
            double rootUptakeShare,
            double rootUptakeCurve)
        {
            if (rootUptakeCurve < 1)
            {
                throw new ContentException($"moisture.rootUptakeCurve ({rootUptakeCurve}) must be at least 1.");
            }
            if (rootUptakeShare < 0 || rootUptakeShare > 1)
            {
                throw new ContentException($"moisture.rootUptakeShare ({rootUptakeShare}) must be from 0 to 1.");
            }
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
            RootUptakeShare = rootUptakeShare;
            RootUptakeCurve = rootUptakeCurve;
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

        /// <summary>
        /// Share of the hour's evaporation demand that grass roots draw from the subsurface,
        /// on top of what the surface gives up. This is how a strip dries below field capacity
        /// at depth in the final days before a match.
        /// </summary>
        public double RootUptakeShare { get; }

        /// <summary>
        /// How sharply root uptake falls as the subsurface dries: 1 falls in a straight line
        /// towards air-dry; higher values keep uptake strong near field capacity and weak once
        /// the soil is dry, as roots find it harder to draw water from drier soil.
        /// </summary>
        public double RootUptakeCurve { get; }

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
