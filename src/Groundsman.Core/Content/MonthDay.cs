using System;
using System.Globalization;

namespace Groundsman.Core.Content
{
    /// <summary>
    /// A date that recurs every year, such as the start of the season.
    /// </summary>
    public readonly struct MonthDay : IEquatable<MonthDay>, IComparable<MonthDay>
    {
        public MonthDay(int month, int day)
        {
            // 29 February doesn't exist every year, so a recurring date can't use it.
            if (month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(2001, month))
            {
                throw new ArgumentOutOfRangeException(nameof(day), $"{month:00}-{day:00} is not a date that occurs every year.");
            }

            Month = month;
            Day = day;
        }

        public int Month { get; }
        public int Day { get; }

        public DateTime In(int year) => new DateTime(year, Month, Day);

        public static bool TryParse(string? text, out MonthDay value)
        {
            value = default;
            if (!DateTime.TryParseExact(text, "MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return false;
            }
            if (parsed.Month == 2 && parsed.Day == 29)
            {
                return false;
            }
            value = new MonthDay(parsed.Month, parsed.Day);
            return true;
        }

        public int CompareTo(MonthDay other) => Month != other.Month ? Month.CompareTo(other.Month) : Day.CompareTo(other.Day);

        public bool Equals(MonthDay other) => Month == other.Month && Day == other.Day;

        public override bool Equals(object? obj) => obj is MonthDay other && Equals(other);

        public override int GetHashCode() => Month * 32 + Day;

        public override string ToString() => $"{Month:00}-{Day:00}";
    }
}
