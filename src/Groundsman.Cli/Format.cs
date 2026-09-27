using System.Globalization;
using Groundsman.Core;
using Groundsman.Core.Readings;
using Groundsman.Core.Time;

namespace Groundsman.Cli;

public static class Format
{
    // Round outwards so rounding for display never puts the true value outside the range shown.
    public static string Percent(ValueRange range) => $"{Math.Floor(range.Low):0}–{Math.Ceiling(range.High):0}%";

    /// <summary>A feel reading's word, or a probe reading's range as it stands now.</summary>
    public static string Reading(Reading reading, ValueRange now) =>
        reading.Word ?? Percent(now);

    public static string Rain(ValueRange range) =>
        range.High <= 0 ? "dry" : $"{Math.Floor(range.Low):0}–{Math.Ceiling(range.High):0} mm";

    public static string Chance(double chance) => $"{Math.Round(chance * 10, MidpointRounding.AwayFromZero) * 10:0}%";

    public static string Temperature(ValueRange range) => $"{Math.Floor(range.Low):0}–{Math.Ceiling(range.High):0}°C";

    public static string Age(GameTime takenAt, GameTime now) => (now.Date - takenAt.Date).Days switch
    {
        0 => "today",
        1 => "yesterday",
        var days => $"{days} days ago",
    };

    public static string Time(GameTime time) =>
        time.Date.ToString("ddd d MMM yyyy", CultureInfo.InvariantCulture) + $", {time.Hour:00}:00";

    public static string Day(DateTime date) => date.ToString("ddd d MMM", CultureInfo.InvariantCulture);

    public static string Pace(DayPace pace) => pace switch
    {
        DayPace.OffSeason => "Off-season",
        DayPace.InSeason => "In season",
        DayPace.FinalPrep => "Final prep",
        DayPace.MatchDay => "Match day",
        _ => pace.ToString(),
    };

    public static string Weather(WeatherObservation weather)
    {
        var text = $"Rain in the last 24 hours: {weather.RainLast24HoursMm:0.0} mm. Now {weather.Temperature:0}°C, wind {weather.WindKph:0} km/h.";
        return weather.YesterdayLow is { } low && weather.YesterdayHigh is { } high
            ? text + $" Yesterday {low:0}–{high:0}°C."
            : text;
    }

    public static string Orders(StripView strip)
    {
        var orders = new List<string>();
        if (strip.WateringQueued)
        {
            orders.Add("[blue]water[/]");
        }
        if (strip.CoverOrder == CoverOrder.Cover)
        {
            orders.Add("[blue]cover on[/]");
        }
        if (strip.CoverOrder == CoverOrder.Uncover)
        {
            orders.Add("[blue]cover off[/]");
        }
        return string.Join(", ", orders);
    }

    public static string Hours(IReadOnlyList<StaffView> staff) =>
        "Hours left today: " + string.Join(", ", staff.Select(s => $"{s.Name} {s.HoursLeft:0.##}/{s.HoursPerDay:0.##}")) + ".";

    public static string NextMatch(Fixture? fixture, GameTime now)
    {
        if (fixture == null)
        {
            return "none scheduled";
        }

        var strip = $"strip {fixture.Strip.Number}";
        if (fixture.Start <= now.Date)
        {
            var day = (now.Date - fixture.Start).Days + 1;
            return fixture.Days == 1 ? $"today on {strip}" : $"day {day} of {fixture.Days} on {strip}";
        }

        var days = (fixture.Start - now.Date).Days;
        return $"{Day(fixture.Start)} on {strip} (in {days} day{(days == 1 ? "" : "s")})";
    }
}
