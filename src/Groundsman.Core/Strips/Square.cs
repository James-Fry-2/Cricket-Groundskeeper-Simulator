using System;
using System.Collections.Generic;
using Groundsman.Core.Content;

namespace Groundsman.Core.Strips
{
    internal sealed class Square
    {
        private readonly StripState[] _strips;

        public Square(GroundSettings ground)
        {
            Saturation = ground.Saturation;
            _strips = new StripState[ground.Strips.Count];
            for (var i = 0; i < _strips.Length; i++)
            {
                var strip = ground.Strips[i];
                _strips[i] = new StripState(strip.Id, strip.SurfaceMoisture, strip.SubsurfaceMoisture);
            }
        }

        public double Saturation { get; }

        public IReadOnlyList<StripState> Strips => _strips;

        public bool Contains(StripId id) => id.Number >= 1 && id.Number <= _strips.Length;

        public StripState Get(StripId id)
        {
            if (!Contains(id))
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "No such strip on this square.");
            }

            // Content guarantees strips are numbered 1 to n in order.
            return _strips[id.Number - 1];
        }
    }
}
