namespace Groundsman.Core.Strips
{
    /// <summary>
    /// True state of one strip. Internal so front ends can't read it: they see readings only.
    /// </summary>
    internal sealed class StripState
    {
        public StripState(StripId id, double surfaceMoisture, double subsurfaceMoisture)
        {
            Id = id;
            SurfaceMoisture = surfaceMoisture;
            SubsurfaceMoisture = subsurfaceMoisture;
        }

        public StripId Id { get; }

        /// <summary>Volumetric water content of the surface layer, in %.</summary>
        public double SurfaceMoisture { get; set; }

        /// <summary>Volumetric water content of the subsurface layer, in %.</summary>
        public double SubsurfaceMoisture { get; set; }
    }
}
