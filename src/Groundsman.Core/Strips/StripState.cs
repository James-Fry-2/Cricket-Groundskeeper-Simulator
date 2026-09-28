using Groundsman.Core.Content;

namespace Groundsman.Core.Strips
{
    /// <summary>
    /// True state of one strip. Internal so front ends can't read it: they see readings only.
    /// </summary>
    internal sealed class StripState
    {
        public StripState(StripId id, LoamSettings loam, double surfaceMoisture, double subsurfaceMoisture, GrassSettings? grass = null)
        {
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
    }
}
