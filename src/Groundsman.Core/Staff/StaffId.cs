using System;

namespace Groundsman.Core.Staff
{
    public readonly struct StaffId : IEquatable<StaffId>
    {
        public StaffId(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Value { get; }

        public bool Equals(StaffId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is StaffId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value;

        public static bool operator ==(StaffId left, StaffId right) => left.Equals(right);
        public static bool operator !=(StaffId left, StaffId right) => !left.Equals(right);
    }
}
