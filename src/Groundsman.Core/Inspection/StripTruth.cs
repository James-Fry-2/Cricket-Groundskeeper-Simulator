using Groundsman.Core.Pitch;
using Groundsman.Core.Strips;

namespace Groundsman.Core.Inspection
{
    public sealed class StripTruth
    {
        public StripTruth(StripId id, double surfaceMoisture, double subsurfaceMoisture, double grassCover, double grassHeightMm, double rootDepthMm, double compaction, double structureDamage, double hardness, PitchCharacteristics pitch, double footholes, double rough, double surfaceWear, double cracks, double lastingWear, double endsEstablishment)
        {
            Footholes = footholes;
            Rough = rough;
            SurfaceWear = surfaceWear;
            Cracks = cracks;
            LastingWear = lastingWear;
            EndsEstablishment = endsEstablishment;
            Pitch = pitch;
            Compaction = compaction;
            StructureDamage = structureDamage;
            Hardness = hardness;
            GrassCover = grassCover;
            GrassHeightMm = grassHeightMm;
            RootDepthMm = rootDepthMm;
            Id = id;
            SurfaceMoisture = surfaceMoisture;
            SubsurfaceMoisture = subsurfaceMoisture;
        }

        public StripId Id { get; }
        public double SurfaceMoisture { get; }
        public double SubsurfaceMoisture { get; }
        public double GrassCover { get; }
        public double GrassHeightMm { get; }
        public double RootDepthMm { get; }
        public double Compaction { get; }
        public double StructureDamage { get; }
        public double Hardness { get; }
        public double Footholes { get; }
        public double Rough { get; }
        public double SurfaceWear { get; }
        public double Cracks { get; }
        public double LastingWear { get; }
        public double EndsEstablishment { get; }

        /// <summary>How the strip would play now.</summary>
        public PitchCharacteristics Pitch { get; }
    }
}
