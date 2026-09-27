using System;
using System.Globalization;

namespace Groundsman.Core.Time
{
    /// <summary>
    /// The start of one simulated hour. Never built from the wall clock.
    /// </summary>
    public readonly struct GameTime : IEquatable<GameTime>, IComparable<GameTime>
    {
        private readonly DateTime _value;

        public GameTime(int year, int month, int day, int hour)
            : this(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified).AddHours(CheckHour(hour)))
        {
        }

        private GameTime(DateTime value)
        {
            _value = value;
        }

        public DateTime Date => _value.Date;
        public int Hour => _value.Hour;

        public static GameTime OnDate(DateTime date, int hour) => new GameTime(date.Year, date.Month, date.Day, hour);

        public GameTime AddHours(int hours) => new GameTime(_value.AddHours(hours));

        public GameTime AddDays(int days) => new GameTime(_value.AddDays(days));

        public GameTime AtHour(int hour) => OnDate(_value, hour);

        public int HoursUntil(GameTime other) => (int)(other._value - _value).TotalHours;

        public int CompareTo(GameTime other) => _value.CompareTo(other._value);

        public bool Equals(GameTime other) => _value == other._value;

        public override bool Equals(object? obj) => obj is GameTime other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();

        public override string ToString() => _value.ToString("yyyy-MM-dd HH:00", CultureInfo.InvariantCulture);

        public static bool operator ==(GameTime left, GameTime right) => left.Equals(right);
        public static bool operator !=(GameTime left, GameTime right) => !left.Equals(right);
        public static bool operator <(GameTime left, GameTime right) => left._value < right._value;
        public static bool operator >(GameTime left, GameTime right) => left._value > right._value;
        public static bool operator <=(GameTime left, GameTime right) => left._value <= right._value;
        public static bool operator >=(GameTime left, GameTime right) => left._value >= right._value;

        private static int CheckHour(int hour)
        {
            if (hour < 0 || hour > 23)
            {
                throw new ArgumentOutOfRangeException(nameof(hour), hour, "Must be 0 to 23.");
            }
            return hour;
        }
    }
}
