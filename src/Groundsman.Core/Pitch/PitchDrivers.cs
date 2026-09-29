namespace Groundsman.Core.Pitch
{
    /// <summary>What's behind the pitch characteristics: each driver's share of its effect.</summary>
    internal sealed class PitchDrivers
    {
        public PitchDrivers(
            double grassSeam,
            double dampSeam,
            double structureDamage,
            double loose,
            double cracks,
            double footholes,
            double surfaceWear,
            double drySpin,
            double roughSpin,
            double crackSpin,
            double wearSpin,
            double cushion,
            double wetness)
        {
            GrassSeam = grassSeam;
            DampSeam = dampSeam;
            StructureDamage = structureDamage;
            Loose = loose;
            Cracks = cracks;
            Footholes = footholes;
            SurfaceWear = surfaceWear;
            DrySpin = drySpin;
            RoughSpin = roughSpin;
            CrackSpin = crackSpin;
            WearSpin = wearSpin;
            Cushion = cushion;
            Wetness = wetness;
        }

        public double GrassSeam { get; }
        public double DampSeam { get; }

        // Consistency lost to each cause.
        public double StructureDamage { get; }
        public double Loose { get; }
        public double Cracks { get; }
        public double Footholes { get; }
        public double SurfaceWear { get; }

        // Spin from each cause.
        public double DrySpin { get; }
        public double RoughSpin { get; }
        public double CrackSpin { get; }
        public double WearSpin { get; }

        /// <summary>Share of pace and bounce long grass is taking off.</summary>
        public double Cushion { get; }

        /// <summary>How wet the surface is, 0 at air-dry to 1 at field capacity.</summary>
        public double Wetness { get; }
    }
}
