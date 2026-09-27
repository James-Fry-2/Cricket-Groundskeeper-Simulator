using Groundsman.Core.Strips;

namespace Groundsman.Core.Content
{
    public sealed class StripSettings
    {
        public StripSettings(StripId id, string loamId, double surfaceMoisture, double subsurfaceMoisture)
        {
            Id = id;
            LoamId = loamId;
            SurfaceMoisture = surfaceMoisture;
            SubsurfaceMoisture = subsurfaceMoisture;
        }

        public StripId Id { get; }
        public string LoamId { get; }
        public double SurfaceMoisture { get; }
        public double SubsurfaceMoisture { get; }
    }
}
