using Groundsman.Core.Content;

namespace Groundsman.Core.Strips
{
    /// <summary>
    /// True state of one strip. Internal so front ends can't read it: they see readings only.
    /// </summary>
    internal sealed class StripState
    {
        public StripState(StripId id, LoamSettings loam, double surfaceMoisture, double subsurfaceMoisture, GrassSettings? grass = null, CompactionSettings? compaction = null)
        {
            Compaction = compaction?.StartingCompaction ?? 0;
            StructureDamage = compaction?.StartingStructureDamage ?? 0;
            Id = id;
            Loam = loam;
            GrassCover = grass?.StartingCover ?? 0;
            GrassHeightMm = grass?.StartingHeightMm ?? 0;
            RootDepthMm = grass?.StartingRootDepthMm ?? 0;
            SurfaceMoisture = surfaceMoisture;
            SubsurfaceMoisture = subsurfaceMoisture;
        }

        public StripId Id { get; }

        public LoamSettings Loam { get; }

        /// <summary>Volumetric water content of the surface layer, in %.</summary>
        public double SurfaceMoisture { get; set; }

        /// <summary>Volumetric water content of the subsurface layer, in %.</summary>
        public double SubsurfaceMoisture { get; set; }

        /// <summary>Share of the surface covered by grass, in %.</summary>
        public double GrassCover { get; set; }

        public double GrassHeightMm { get; set; }
        public double RootDepthMm { get; set; }

        /// <summary>0 to 1: how tightly rolling has packed the soil.</summary>
        public double Compaction { get; set; }

        /// <summary>0 to 1: harm from rolling too wet or overusing the heavy roller. Hidden, and felt later as uneven bounce and cracking.</summary>
        public double StructureDamage { get; set; }

        /// <summary>0 to 1: depth of the bowlers' footholes at the ends.</summary>
        public double Footholes { get; set; }

        /// <summary>0 to 1: follow-through rough where spinners can use it.</summary>
        public double Rough { get; set; }

        /// <summary>0 to 1: batters' marks, running and crumbling.</summary>
        public double SurfaceWear { get; set; }

        /// <summary>0 to 1: cracks opened by clay drying.</summary>
        public double Cracks { get; set; }

        /// <summary>The ends have been filled and seeded since the strip was last played on.</summary>
        public bool EndsRepaired { get; set; }
    }
}
