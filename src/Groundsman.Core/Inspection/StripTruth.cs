using Groundsman.Core.Strips;

namespace Groundsman.Core.Inspection
{
    public sealed class StripTruth
    {
        public StripTruth(StripId id, double surfaceMoisture, double subsurfaceMoisture, double grassCover, double grassHeightMm, double rootDepthMm, double compaction, double structureDamage, double hardness)
        {
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
    }
}
