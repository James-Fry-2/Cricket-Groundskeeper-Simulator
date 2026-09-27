using Groundsman.Core.Strips;

namespace Groundsman.Core.Content
{
    public sealed class StripSettings
    {
        public StripSettings(StripId id, double surfaceMoisture, double subsurfaceMoisture)
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
