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

    /// <summary>Days since a date, short for the strip table: 0d is today.</summary>
    public static string Ago(DateTime date, GameTime now) => $"{(now.Date - date.Date).Days}d";

    public static string EndsState(Groundsman.Core.Strips.EndsState state) => state switch
    {
        Groundsman.Core.Strips.EndsState.Bare => "bare",
        Groundsman.Core.Strips.EndsState.Seeded => "seeded",
        Groundsman.Core.Strips.EndsState.Thin => "thin",
        _ => "established",
    };

    /// <summary>The last look at the ends, and when they were last repaired.</summary>
    public static string Ends(StripView strip, GameTime now)
    {
        var parts = new List<string>();
        if (strip.Ends is { } look)
        {
            parts.Add($"{EndsState(look.State)} {Ago(look.TakenAt.Date, now)}");
        }
        if (strip.LastRepaired is { } repaired)
        {
            parts.Add($"rep {Ago(repaired.Date, now)}");
        }
        return string.Join(", ", parts);
    }

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
        if (strip.MowingQueued && strip.LastMown != null)
        {
            orders.Add($"[blue]mow {strip.LastMown.HeightMm:0.#}[/]");
        }
        if (strip.RollingQueued)
        {
            orders.Add("[blue]roll[/]");
        }
        if (strip.RepairQueued)
        {
            orders.Add("[blue]repair[/]");
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

    public static string Innings(Groundsman.Core.Match.InningsView innings)
    {
        var score = innings.Wickets == 10
            ? $"{innings.Runs} all out"
            : $"{innings.Runs}-{innings.Wickets}{(innings.Declared ? "d" : "")}";
        return $"{innings.Batting} {score} ({innings.Overs.ToString("0.#", CultureInfo.InvariantCulture)} ov)";
    }

    /// <summary>
    /// The innings to show. One begun at close of play with no overs faced is left off, as on
    /// a real scoreboard.
    /// </summary>
    public static IEnumerable<Groundsman.Core.Match.InningsView> Started(Groundsman.Core.Match.MatchView match) =>
        match.Innings.Where((innings, i) => i == 0 || innings.Overs > 0);

    public static string Grade(Groundsman.Core.Match.PitchGrade grade) => grade switch
    {
        Groundsman.Core.Match.PitchGrade.VeryGood => "Very good",
        Groundsman.Core.Match.PitchGrade.Satisfactory => "Satisfactory",
        Groundsman.Core.Match.PitchGrade.Unsatisfactory => "Unsatisfactory",
        _ => "Unfit",
    };

    public static string Notice(Notice notice) => notice switch
    {
        StripLockedNotice { ByDefault: true } locked =>
            $"No strip was chosen for {Match(locked.Fixture.Fixture)} on {Day(locked.Fixture.Fixture.Start)}, so the head groundsman has put it on strip {locked.Fixture.Strip!.Value.Number}. Its build-up starts now.",
        StripLockedNotice locked =>
            $"Strip {locked.Fixture.Strip!.Value.Number} is locked in for {Match(locked.Fixture.Fixture)} on {Day(locked.Fixture.Fixture.Start)}. Its build-up starts now.",
        _ => notice.ToString() ?? "",
    };

    public static string Match(Fixture fixture) => $"{fixture.Format.Name} v {fixture.Opponent.Name}";

    public static string Demerits(int count) => $"{count} demerit{(count == 1 ? "" : "s")}";

    /// <summary>What Law 9 allows on the match strip this turn.</summary>
    public static string Interval(Groundsman.Core.Match.IntervalView interval)
    {
        var strip = interval.Strip.Number;
        var jobs = new List<string>();
        if (interval.CanClean)
        {
            jobs.Add($"clean the footholes (clean {strip})");
        }
        if (interval.CanFill)
        {
            jobs.Add($"fill them for the night (fill {strip})");
        }
        return jobs.Count == 0
            ? $"{interval.Name}."
            : $"{interval.Name}. On strip {strip} you can {string.Join(" or ", jobs)}.";
    }

    public static string FirstName(string name)
    {
        var space = name.IndexOf(' ');
        return space > 0 ? name.Substring(0, space) : name;
    }

    public static string NextMatch(FixtureView? next, GameTime now)
    {
        if (next == null)
        {
            return "none scheduled";
        }

        var fixture = next.Fixture;
        var match = $"{fixture.Format.Name} v {fixture.Opponent.Name}";
        var strip = next.Strip is { } assigned ? $"on strip {assigned.Number}" : "no strip chosen yet";
        if (fixture.Start <= now.Date)
        {
            var day = (now.Date - fixture.Start).Days + 1;
            return fixture.Days == 1 ? $"{match}, today {strip}" : $"{match}, day {day} of {fixture.Days} {strip}";
        }

        var days = (fixture.Start - now.Date).Days;
        var until = $"(in {days} day{(days == 1 ? "" : "s")})";
        return next.Strip == null
            ? $"{match}, {Day(fixture.Start)} {until}, {strip}"
            : $"{match}, {Day(fixture.Start)} {strip} {until}";
    }
}
