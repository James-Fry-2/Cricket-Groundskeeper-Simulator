using System.Collections.Generic;

namespace Groundsman.Core.Content
{
    public sealed class GroundSettings
    {
        public GroundSettings(string name, IReadOnlyList<string> ends, IReadOnlyList<StripSettings> strips)
        {
            if (ends.Count != 2 || string.IsNullOrWhiteSpace(ends[0]) || string.IsNullOrWhiteSpace(ends[1]))
            {
                throw new ContentException("ground.ends needs the names of both ends.");
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ContentException("ground.name can't be blank.");
            }
            if (strips.Count == 0)
            {
                throw new ContentException("ground.strips needs at least one strip.");
            }
            for (var i = 0; i < strips.Count; i++)
            {
                var strip = strips[i];
                if (strip.Id.Number != i + 1)
                {
                    throw new ContentException($"ground.strips[{i}].number ({strip.Id.Number}) must be {i + 1}: strips are numbered 1 to n in order.");
                }
                if (string.IsNullOrWhiteSpace(strip.LoamId))
                {
                    throw new ContentException($"ground.strips[{i}].loam can't be blank.");
                }
                CheckMoisture(i, "surfaceMoisture", strip.SurfaceMoisture);
                CheckMoisture(i, "subsurfaceMoisture", strip.SubsurfaceMoisture);
            }

            Name = name;
            Ends = new List<string>(ends).AsReadOnly();
            Strips = new List<StripSettings>(strips).AsReadOnly();
        }

        public string Name { get; }

        /// <summary>The two ends of the ground, which commentary names.</summary>
        public IReadOnlyList<string> Ends { get; }

        public IReadOnlyList<StripSettings> Strips { get; }

        private static void CheckMoisture(int index, string field, double value)
        {
            if (value < 0 || value > 100)
            {
                throw new ContentException($"ground.strips[{index}].{field} ({value}) must be from 0 to 100.");
            }
        }
    }
}
