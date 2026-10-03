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

    public static string Notice(Notice notice, int requestNumber = 0) => notice switch
    {
        Groundsman.Core.Pressures.RequestNotice asked =>
            $"{Who(asked.Request.Stakeholder)} asks for {Request(asked.Request.Kind)} for {Match(asked.Request.Fixture)} on {Day(asked.Request.Fixture.Start)}. Answer by {Day(asked.Request.AnswerBy)}: yes {requestNumber} or no {requestNumber}.",
        Groundsman.Core.Pressures.SatisfactionNotice moved => Change(moved.Change),
        StripLockedNotice { ByDefault: true } locked =>
            $"No strip was chosen for {Match(locked.Fixture.Fixture)} on {Day(locked.Fixture.Fixture.Start)}, so the head groundsman has put it on strip {locked.Fixture.Strip!.Value.Number}. Its build-up starts now.",
        StripLockedNotice locked =>
            $"Strip {locked.Fixture.Strip!.Value.Number} is locked in for {Match(locked.Fixture.Fixture)} on {Day(locked.Fixture.Fixture.Start)}. Its build-up starts now.",
        _ => notice.ToString() ?? "",
    };

    public static string Who(Groundsman.Core.Pressures.Stakeholder stakeholder) => stakeholder switch
    {
        Groundsman.Core.Pressures.Stakeholder.Captain => "The captain",
        Groundsman.Core.Pressures.Stakeholder.Board => "The board",
        _ => "The referee",
    };

    public static string Request(Groundsman.Core.Pressures.RequestKind kind) => kind switch
    {
        Groundsman.Core.Pressures.RequestKind.Green => "a green seamer",
        Groundsman.Core.Pressures.RequestKind.Turning => "a pitch that turns late on",
        Groundsman.Core.Pressures.RequestKind.Pace => "pace and carry",
        Groundsman.Core.Pressures.RequestKind.Flat => "a flat batting pitch",
        _ => "a pitch that lasts into day four",
    };

    /// <summary>A satisfaction change as a sentence, with its points.</summary>
    public static string Change(Groundsman.Core.Pressures.SatisfactionChange change)
    {
        var match = $"{Match(change.Fixture)} on {Day(change.Fixture.Start)}";
        var asked = change.Request is { } kind ? Request(kind) : "";
        var why = change.Reason switch
        {
            Groundsman.Core.Pressures.SatisfactionReason.RequestDelivered => $"you delivered {asked} for {match}",
            Groundsman.Core.Pressures.SatisfactionReason.RequestNotDelivered => $"you promised {asked} for {match} and it didn't come",
            Groundsman.Core.Pressures.SatisfactionReason.RequestDeclined => $"you turned down {asked} for {match}",
            Groundsman.Core.Pressures.SatisfactionReason.RequestIgnored => $"you never answered their request for {asked} for {match}",
            Groundsman.Core.Pressures.SatisfactionReason.HomeWin => $"the county won {match}",
            Groundsman.Core.Pressures.SatisfactionReason.HomeLoss => $"the county lost {match}",
            Groundsman.Core.Pressures.SatisfactionReason.DayFourReached => $"{match} went into day four",
            Groundsman.Core.Pressures.SatisfactionReason.ShortFourDay => $"{match} was over inside three days",
            Groundsman.Core.Pressures.SatisfactionReason.NoResult => $"{match} ended without a result",
            Groundsman.Core.Pressures.SatisfactionReason.TelevisedCentre => $"{match} was televised from a centre strip",
            Groundsman.Core.Pressures.SatisfactionReason.TelevisedOffCentre => $"{match} was televised from an outer strip",
            Groundsman.Core.Pressures.SatisfactionReason.Demerits => $"{match} cost {Demerits(change.Demerits)}",
            _ => $"{match} was rated {Grade(change.Grade!.Value).ToLowerInvariant()}",
        };
        var points = Math.Round(change.Delta, MidpointRounding.AwayFromZero);
        return $"{Who(change.Stakeholder)} {(points >= 0 ? "+" : "−")}{Math.Abs(points):0}: {why}.";
    }

    /// <summary>
    /// Days a fixture's strip will have rested since its previous match there, played or
    /// planned, and that match; null when it's the strip's first this season or unassigned.
    /// </summary>
    public static (int Days, Fixture Previous)? Rest(IReadOnlyList<FixtureView> fixtures, int index)
    {
        var fixture = fixtures[index];
        if (fixture.Strip is not { } strip)
        {
            return null;
        }
        var previous = fixtures
            .Where(f => f.Strip == strip && f.Fixture.End < fixture.Fixture.Start)
            .Select(f => f.Fixture)
            .OrderBy(f => f.End)
            .LastOrDefault();
        return previous == null ? null : ((fixture.Fixture.Start - previous.End).Days - 1, previous);
    }

    public static string ShortRequest(Groundsman.Core.Pressures.RequestKind kind) => kind switch
    {
        Groundsman.Core.Pressures.RequestKind.Green => "green",
        Groundsman.Core.Pressures.RequestKind.Turning => "turning",
        Groundsman.Core.Pressures.RequestKind.Pace => "pace",
        Groundsman.Core.Pressures.RequestKind.Flat => "flat",
        _ => "4 days",
    };

    /// <summary>A fixture's requests in brief: what was asked, and where it stands.</summary>
    public static string Requests(IEnumerable<Groundsman.Core.Pressures.RequestView> requests) =>
        string.Join(", ", requests.Select(r => $"{ShortRequest(r.Kind)} {r.Status switch
        {
            Groundsman.Core.Pressures.RequestStatus.Open => "?",
            Groundsman.Core.Pressures.RequestStatus.Accepted => "yes",
            Groundsman.Core.Pressures.RequestStatus.Declined => "no",
            Groundsman.Core.Pressures.RequestStatus.Ignored => "ignored",
            Groundsman.Core.Pressures.RequestStatus.Delivered => "✓",
            Groundsman.Core.Pressures.RequestStatus.NotDelivered => "✗",
            _ => "rained off",
        }}"));

    public static string Mood(Groundsman.Core.Pressures.Mood mood) => mood switch
    {
        Groundsman.Core.Pressures.Mood.Delighted => "delighted",
        Groundsman.Core.Pressures.Mood.Content => "content",
        Groundsman.Core.Pressures.Mood.Uneasy => "uneasy",
        _ => "unhappy",
    };

    public static string Wear(Groundsman.Core.Pressures.SquareWear wear) => wear switch
    {
        Groundsman.Core.Pressures.SquareWear.Heavy => "Heavily worn",
        Groundsman.Core.Pressures.SquareWear.Worn => "Worn",
        Groundsman.Core.Pressures.SquareWear.Light => "Lightly worn",
        _ => "Fresh",
    };

    /// <summary>A season's worth of one reason, for the review.</summary>
    public static string Summary(Groundsman.Core.Pressures.ReasonSummary summary)
    {
        var asked = summary.Request is { } kind ? Request(kind) : "";
        var times = summary.Count == 1 ? "" : $" ({summary.Count} times)";
        var why = summary.Reason switch
        {
            Groundsman.Core.Pressures.SatisfactionReason.RequestDelivered => $"delivered {asked}",
            Groundsman.Core.Pressures.SatisfactionReason.RequestNotDelivered => $"promised {asked} and didn't deliver",
            Groundsman.Core.Pressures.SatisfactionReason.RequestDeclined => $"turned down {asked}",
            Groundsman.Core.Pressures.SatisfactionReason.RequestIgnored => $"never answered a request for {asked}",
            Groundsman.Core.Pressures.SatisfactionReason.HomeWin => "the county's wins",
            Groundsman.Core.Pressures.SatisfactionReason.HomeLoss => "the county's defeats",
            Groundsman.Core.Pressures.SatisfactionReason.DayFourReached => "four-day matches going into day four",
            Groundsman.Core.Pressures.SatisfactionReason.ShortFourDay => "four-day matches over inside three days",
            Groundsman.Core.Pressures.SatisfactionReason.NoResult => "matches without a result",
            Groundsman.Core.Pressures.SatisfactionReason.TelevisedCentre => "televised matches on a centre strip",
            Groundsman.Core.Pressures.SatisfactionReason.TelevisedOffCentre => "televised matches on an outer strip",
            Groundsman.Core.Pressures.SatisfactionReason.Demerits => "demerits",
            _ => "the pitch ratings",
        };
        var points = Math.Round(summary.Total, MidpointRounding.AwayFromZero);
        return $"{(points >= 0 ? "+" : "−")}{Math.Abs(points):0}: {why}{times}";
    }

    public static string Satisfaction(IReadOnlyList<Groundsman.Core.Pressures.StakeholderView> stakeholders) =>
        "Satisfaction: " + string.Join(", ", stakeholders.Select(s => $"{s.Stakeholder} {s.Satisfaction:0}")) + ".";

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
