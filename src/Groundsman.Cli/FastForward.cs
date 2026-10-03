using Groundsman.Core;

namespace Groundsman.Cli;

/// <summary>
/// When fast-forward stops: anything that needs the player. Judged on the view alone, so it
/// can't stop for something the player couldn't know.
/// </summary>
public static class FastForward
{
    /// <summary>Chance of rain, today or tomorrow, worth stopping for while a strip is in its build-up.</summary>
    public const double RainChance = 0.6;

    /// <summary>A safety net: never more than this many turns at once.</summary>
    public const int MaxTurns = 60;

    /// <summary>Why the player is needed now, or null if the turn can be skipped.</summary>
    /// <param name="rainWarnedOn">The day it last stopped for rain: it warns once a day, so a player who chooses not to cover isn't stopped every turn.</param>
    public static string? StopReason(GameView view, DateTime? rainWarnedOn = null)
    {
        if (view.Review != null)
        {
            return "the season is over";
        }
        if (view.Notices.Count > 0)
        {
            return "there's news";
        }
        // A match is worth stopping for when it starts, and at close of play when the footholes
        // can be filled; the commentary from sessions skipped in between still prints.
        if (view.Interval is { } interval && view.NextFixture is { } playing)
        {
            if (interval.CanFill)
            {
                return "it's close of play, when the footholes can be filled";
            }
            if (view.Now.Date == playing.Fixture.Start && interval.Name == "Before play")
            {
                return "the match starts today";
            }
        }

        var today = view.Now.Date;
        if (view.Fixtures.FirstOrDefault(f => !f.Locked && f.Strip == null && (f.LocksOn - today).Days <= 1) is { } unassigned)
        {
            return $"{Format.Match(unassigned.Fixture)} has no strip and its build-up starts {(unassigned.LocksOn <= today ? "today" : "tomorrow")}";
        }

        var preparing = view.Fixtures
            .Where(f => f.Locked && f.Strip != null && f.Fixture.Start > today)
            .Select(f => view.Strips[f.Strip!.Value.Number - 1])
            .Where(s => !s.Covered && s.CoverOrder == CoverOrder.None);
        if (rainWarnedOn != today && preparing.Any() && view.Forecast.Take(2).Any(d => d.ChanceOfRain >= RainChance))
        {
            return "rain is likely and a strip in its build-up is uncovered";
        }
        return null;
    }
}
