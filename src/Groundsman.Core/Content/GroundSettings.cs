using System;
using System.Collections.Generic;
using System.Linq;
using Groundsman.Core.Strips;

namespace Groundsman.Core.Content
{
    public sealed class GroundSettings
    {
        /// <param name="centreStrips">The strips in the middle of the square; none when not given.</param>
        public GroundSettings(string name, IReadOnlyList<string> ends, IReadOnlyList<StripSettings> strips, IReadOnlyList<int>? centreStrips = null)
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

            var centre = centreStrips ?? Array.Empty<int>();
            if (centreStrips != null && centre.Count == 0)
            {
                throw new ContentException("ground.centreStrips needs at least one strip.");
            }
            foreach (var number in centre)
            {
                if (number < 1 || number > strips.Count)
                {
                    throw new ContentException($"ground.centreStrips ({number}) must be a strip number from 1 to {strips.Count}.");
                }
            }

            Name = name;
            _centre = new HashSet<int>(centre);
            Ends = new List<string>(ends).AsReadOnly();
            Strips = new List<StripSettings>(strips).AsReadOnly();
        }

        public string Name { get; }

        /// <summary>The two ends of the ground, which commentary names.</summary>
        public IReadOnlyList<string> Ends { get; }

        public IReadOnlyList<StripSettings> Strips { get; }

        public bool IsCentre(StripId strip) => _centre.Contains(strip.Number);

        /// <summary>Strips between this one and the nearest centre strip, plus one; 0 for a centre strip.</summary>
        public int FromCentre(StripId strip) => _centre.Count == 0 ? 0 : _centre.Min(c => Math.Abs(c - strip.Number));

        private readonly HashSet<int> _centre;

        private static void CheckMoisture(int index, string field, double value)
        {
            if (value < 0 || value > 100)
            {
                throw new ContentException($"ground.strips[{index}].{field} ({value}) must be from 0 to 100.");
            }
        }
    }
}
