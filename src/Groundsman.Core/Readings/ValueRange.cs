using System;

namespace Groundsman.Core.Readings
{
    public readonly struct ValueRange : IEquatable<ValueRange>
    {
        public ValueRange(double low, double high)
        {
            if (high < low)
            {
                throw new ArgumentException($"High ({high}) is below low ({low}).", nameof(high));
            }

            Low = low;
            High = high;
        }

        public double Low { get; }
        public double High { get; }
        public double Width => High - Low;

        public bool Contains(double value) => value >= Low && value <= High;

        public bool Equals(ValueRange other) => Low.Equals(other.Low) && High.Equals(other.High);

        public override bool Equals(object? obj) => obj is ValueRange other && Equals(other);

        public override int GetHashCode() => (Low.GetHashCode() * 397) ^ High.GetHashCode();

        public override string ToString() => $"{Low:0.#} to {High:0.#}";
    }
}
