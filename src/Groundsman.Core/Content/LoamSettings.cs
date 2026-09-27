namespace Groundsman.Core.Content
{
    /// <summary>
    /// A cricket loam's soil water behaviour. Moisture values are volumetric %.
    /// </summary>
    public sealed class LoamSettings
    {
        public LoamSettings(
            string id,
            string name,
            double clayPercent,
            double saturation,
            double fieldCapacity,
            double airDry,
            double surfaceDrainageRate,
            double subsurfaceDrainageRate,
            double capillaryRate,
            double crackingTendency)
        {
            var path = $"loams[{id}]";
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ContentException("loams: every loam needs an id.");
            }
            if (clayPercent < 0 || clayPercent > 100)
            {
                throw new ContentException($"{path}.clayPercent ({clayPercent}) must be from 0 to 100.");
            }
            if (saturation <= 0 || saturation > 100)
            {
                throw new ContentException($"{path}.saturation ({saturation}) must be above 0 and at most 100.");
            }
            if (fieldCapacity <= 0 || fieldCapacity >= saturation)
            {
                throw new ContentException($"{path}.fieldCapacity ({fieldCapacity}) must be above 0 and below saturation ({saturation}).");
            }
            if (airDry < 0 || airDry >= fieldCapacity)
            {
                throw new ContentException($"{path}.airDry ({airDry}) must be from 0 to below fieldCapacity ({fieldCapacity}).");
            }
            CheckRate(path, "surfaceDrainageRate", surfaceDrainageRate);
            CheckRate(path, "subsurfaceDrainageRate", subsurfaceDrainageRate);
            CheckRate(path, "capillaryRate", capillaryRate);
            CheckRate(path, "crackingTendency", crackingTendency);

            Id = id;
            Name = name;
            ClayPercent = clayPercent;
            Saturation = saturation;
            FieldCapacity = fieldCapacity;
            AirDry = airDry;
            SurfaceDrainageRate = surfaceDrainageRate;
            SubsurfaceDrainageRate = subsurfaceDrainageRate;
            CapillaryRate = capillaryRate;
            CrackingTendency = crackingTendency;
        }

        public string Id { get; }
        public string Name { get; }
        public double ClayPercent { get; }

        /// <summary>Moisture at which a layer holds no more water.</summary>
        public double Saturation { get; }

        /// <summary>Moisture a layer drains down to under gravity, about a day after saturation.</summary>
        public double FieldCapacity { get; }

        /// <summary>Moisture at which the surface stops giving up water to evaporation.</summary>
        public double AirDry { get; }

        /// <summary>Share of the surface's water above field capacity that drains down each hour.</summary>
        public double SurfaceDrainageRate { get; }

        /// <summary>Share of the subsurface's water above field capacity lost downwards each hour.</summary>
        public double SubsurfaceDrainageRate { get; }

        /// <summary>Share of the moisture difference drawn up each hour when the surface is drier.</summary>
        public double CapillaryRate { get; }

        /// <summary>0 to 1. Stored for phase 3, when drying cracks strips.</summary>
        public double CrackingTendency { get; }

        private static void CheckRate(string path, string field, double value)
        {
            if (value < 0 || value > 1)
            {
                throw new ContentException($"{path}.{field} ({value}) must be from 0 to 1.");
            }
        }
    }
}
