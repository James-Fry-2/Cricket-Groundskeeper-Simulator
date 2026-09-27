using System.Globalization;
using Groundsman.Core;
using Groundsman.Core.Readings;
using Groundsman.Core.Time;

namespace Groundsman.Cli;

public static class Format
{
    // Round outwards so rounding for display never puts the true value outside the range shown.
    public static string Percent(ValueRange range) => $"{Math.Floor(range.Low):0}–{Math.Ceiling(range.High):0}%";

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

    public static string NextMatch(DateTime? matchDay, GameTime now)
    {
        if (matchDay is not { } day)
        {
            return "none scheduled";
        }

        var days = (day - now.Date).Days;
        return days == 0 ? "today" : $"{Day(day)} (in {days} day{(days == 1 ? "" : "s")})";
    }
}
