using System;

namespace Groundsman.Core.Strips
{
    public readonly struct StripId : IEquatable<StripId>, IComparable<StripId>
    {
        public StripId(int number)
        {
            Number = number;
        }

        public int Number { get; }

        public int CompareTo(StripId other) => Number.CompareTo(other.Number);

        public bool Equals(StripId other) => Number == other.Number;

        public override bool Equals(object? obj) => obj is StripId other && Equals(other);

        public override int GetHashCode() => Number;

        public override string ToString() => $"Strip {Number}";

        public static bool operator ==(StripId left, StripId right) => left.Equals(right);
        public static bool operator !=(StripId left, StripId right) => !left.Equals(right);
    }
}
