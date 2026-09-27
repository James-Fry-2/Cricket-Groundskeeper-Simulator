using System.Collections.Generic;

namespace Groundsman.Core.Content
{
    public sealed class GroundSettings
    {
        public GroundSettings(string name, double saturation, IReadOnlyList<StripSettings> strips)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ContentException("ground.name can't be blank.");
            }
            if (saturation <= 0 || saturation > 100)
            {
                throw new ContentException($"ground.saturation ({saturation}) must be above 0 and at most 100.");
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
                CheckMoisture(i, "surfaceMoisture", strip.SurfaceMoisture, saturation);
                CheckMoisture(i, "subsurfaceMoisture", strip.SubsurfaceMoisture, saturation);
            }

            Name = name;
            Saturation = saturation;
            Strips = new List<StripSettings>(strips).AsReadOnly();
        }

        public string Name { get; }

        /// <summary>Moisture % at which a layer holds no more water.</summary>
        public double Saturation { get; }

        public IReadOnlyList<StripSettings> Strips { get; }

        private static void CheckMoisture(int index, string field, double value, double saturation)
        {
            if (value < 0 || value > saturation)
            {
                throw new ContentException($"ground.strips[{index}].{field} ({value}) must be from 0 to saturation ({saturation}).");
            }
        }
    }
}
