using Groundsman.Core.Strips;

namespace Groundsman.Core.Inspection
{
    public sealed class StripTruth
    {
        public StripTruth(StripId id, double surfaceMoisture, double subsurfaceMoisture)
        {
            Id = id;
            SurfaceMoisture = surfaceMoisture;
            SubsurfaceMoisture = subsurfaceMoisture;
        }

        public StripId Id { get; }
        public double SurfaceMoisture { get; }
        public double SubsurfaceMoisture { get; }
    }
}
