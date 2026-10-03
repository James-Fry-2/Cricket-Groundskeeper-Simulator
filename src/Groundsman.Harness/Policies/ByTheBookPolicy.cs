using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Readings;
using Groundsman.Core.Staff;
using Groundsman.Core.Strips;

namespace Groundsman.Harness.Policies;

/// <summary>
/// The research's preparation loop (docs/research.md section 3), from readings and the
/// forecast only. Every fixture in the next ten days gets its build-up:
/// - Moisture at depth with a dry surface, measured as professionals do with a soil core.
///   Water into a dry profile while rain is unlikely, protect a profile that's there from rain,
///   let a wet one dry in the open, and keep rain off in the final days.
/// - Mow every other day, stepping the height down towards 6–8mm without cutting more than
///   a third off at once.
/// - Roll daily when the surface feels moist but not wet, working up from the light roller to
///   the heavy one, then a light roll to finish. Never on a day it's watered.
/// After each match the ends are repaired.
/// </summary>
public class ByTheBookPolicy : IPolicy
{
    public const int PrepStartsDaysOut = 10;
    public const int CoringStartsDaysOut = 7;
    public const int FinalDays = 3;

    /// <summary>Core midpoint, %, below which the strip is watered from a week out to three days out.</summary>
    public const double WaterBelowDepth = 25;

    /// <summary>Core midpoint, %, below which the strip still gets water two days out.</summary>
    public const double LateWaterBelowDepth = 21;

    /// <summary>Core midpoint, %, from which the profile is worth protecting from rain.</summary>
    public const double ProtectFromDepth = 26;

    /// <summary>Chance of rain at which watering is put off, and a dry strip is covered only against heavy rain.</summary>
    public const double RainLikely = 0.6;

    /// <summary>Chance of rain at which a strip worth protecting, or in its final days, is covered.</summary>
    public const double CoverAtChance = 0.4;

    /// <summary>A forecast top rain amount, mm, worth covering against even a dry strip.</summary>
    public const double HeavyRainMm = 15;

    /// <summary>The height to reach the day before the match, mm, within the research's 6–8mm.</summary>
    public const double FinalHeightMm = 7;

    /// <summary>How much higher the target is for each day further out, mm.</summary>
    public const double HeightStepPerDayMm = 0.4;

    public const int MowEveryDays = 2;

    /// <summary>Most of the height taken off in one cut, safely under the third that scalps.</summary>
    public const double MaxCutShare = 0.3;

    /// <summary>What a groundsman expects grass to grow in season, mm a day, to judge its height since the last cut.</summary>
    public const double ExpectedGrowthPerDayMm = 0.8;

    /// <summary>The height assumed for a strip not cut this season, mm.</summary>
    public const double UncutHeightMm = 25;

    /// <summary>The tallest a groundsman would guess unmown grass stands, mm: it lies over rather than growing on.</summary>
    public const double TallestGuessMm = 40;

    /// <summary>Height the rest of the square is kept at in season, mm, mown weekly.</summary>
    public const double SquareHeightMm = 15;

    public const int SquareMowEveryDays = 7;

    /// <summary>
    /// Days out from which a surface too dry to roll is watered so the roller can compact it
    /// next day. Later than this, the water would still be in the surface on match morning.
    /// </summary>
    public const int WetToRollFromDaysOut = 5;

    /// <summary>A probe midpoint, %, from which the surface is moist enough to roll.</summary>
    public const double RollFromSurface = 18;

    /// <summary>A probe midpoint, %, above which the surface is too wet to roll.</summary>
    public const double RollToSurface = 25;

    private static readonly StaffId Deputy = new StaffId("sam");

    private readonly IReadOnlyDictionary<string, StripId>? _plan;

    /// <param name="plan">Strips by fixture id, assigned before each lock; without one, the assignments stand.</param>
    public ByTheBookPolicy(IReadOnlyDictionary<string, StripId>? plan = null)
    {
        _plan = plan;
    }

    public virtual string Name => "by the book";

    /// <summary>Roller and minutes by days out: light, medium, heavy last, then a light finish.</summary>
    public static (string Roller, double Minutes) RollingFor(int daysOut) => daysOut switch
    {
        >= 8 => ("light", 45),
        >= 5 => ("medium", 45),
        >= 3 => ("heavy", 40),
        _ => ("light", 20),
    };

    public static double TargetHeightMm(int daysOut) => FinalHeightMm + HeightStepPerDayMm * (daysOut - 1);

    public void PlayTurn(IGame game)
    {
        ChooseStrips(game);
        AnswerRequests(game);

        var view = game.View;
        var today = view.Now.Date;
        var preparing = view.Fixtures
            .Where(f => f.Strip != null && (f.Fixture.Start - today).Days is >= 1 and <= PrepStartsDaysOut)
            .ToList();
        var playing = view.Fixtures.FirstOrDefault(f => f.Fixture.Start <= today && today <= f.Fixture.End);

        foreach (var strip in view.Strips)
        {
            if (strip.Covered && strip.CoverOrder == CoverOrder.None && strip.Id != playing?.Strip && preparing.All(f => f.Strip != strip.Id))
            {
                game.Submit(new UncoverStrip(strip.Id));
            }
        }

        RepairAfterMatches(game);
        foreach (var fixture in preparing)
        {
            Prepare(game, fixture);
        }
        KeepSquareMown(game, preparing.Select(f => f.Strip).Append(playing?.Strip).ToList());
    }

    /// <summary>The research's in-season routine: the whole square mown about weekly, so no strip runs to long grass between uses.</summary>
    private static void KeepSquareMown(IGame game, IReadOnlyList<StripId?> busy)
    {
        var view = game.View;
        foreach (var strip in view.Strips.Where(s => !busy.Contains(s.Id) && !s.MowingQueued))
        {
            var daysSince = strip.LastMown == null ? int.MaxValue : (view.Now.Date - strip.LastMown.OrderedAt.Date).Days;
            if (daysSince < SquareMowEveryDays)
            {
                continue;
            }
            var estimate = EstimateHeight(strip, view.Now.Date);
            var height = Math.Round(Math.Max(SquareHeightMm, estimate * (1 - MaxCutShare)), 1);
            if (height < estimate)
            {
                Do(game, by => new MowStrip(strip.Id, height, by));
            }
        }
    }

    private static double EstimateHeight(StripView strip, DateTime today) =>
        strip.LastMown == null
            ? UncutHeightMm
            : Math.Min(TallestGuessMm, strip.LastMown.HeightMm + ExpectedGrowthPerDayMm * (today - strip.LastMown.OrderedAt.Date).Days);

    /// <summary>Assigns strips before their locks: by default, following the plan if there is one.</summary>
    protected virtual void ChooseStrips(IGame game)
    {
        if (_plan != null)
        {
            Follow(game, _plan);
        }
    }

    /// <summary>Answers open requests; by default it leaves them, so they're ignored at the lock.</summary>
    protected virtual void AnswerRequests(IGame game)
    {
    }

    protected static void Follow(IGame game, IReadOnlyDictionary<string, StripId> plan)
    {
        foreach (var fixture in game.View.Fixtures)
        {
            if (!fixture.Locked && plan.TryGetValue(fixture.Id, out var strip) && fixture.Strip != strip)
            {
                game.Submit(new AssignStrip(fixture.Id, strip));
            }
        }
    }

    private static void RepairAfterMatches(IGame game)
    {
        var view = game.View;
        var today = view.Now.Date;
        foreach (var match in view.Matches.Where(m => m.Finished && m.Fixture.End < today))
        {
            var strip = view.Strips[match.Strip.Number - 1];
            var inUse = view.Fixtures.Any(f => f.Strip == strip.Id && f.Fixture.Start <= today && today <= f.Fixture.End);
            if (!inUse && !strip.RepairQueued && !(strip.LastRepaired?.Date > match.Fixture.End))
            {
                Do(game, by => new RepairEnds(strip.Id, by));
            }
        }
    }

    private static void Prepare(IGame game, FixtureView fixture)
    {
        var view = game.View;
        var today = view.Now.Date;
        var daysOut = (fixture.Fixture.Start - today).Days;
        var stripId = fixture.Strip!.Value;
        var index = stripId.Number - 1;
        var target = view.Strips[index];

        if (daysOut <= CoringStartsDaysOut && target.SubsurfaceMoisture?.TakenAt.Date != today)
        {
            game.Submit(new TakeReading(stripId, Deputy, ReadingSource.SoilCore));
        }
        if (daysOut <= FinalDays && target.SurfaceMoisture?.TakenAt.Date != today)
        {
            game.Submit(new TakeReading(stripId, Deputy));
        }

        view = game.View;
        target = view.Strips[index];
        var depth = target.SubsurfaceMoistureNow is { } core ? (core.Low + core.High) / 2 : (double?)null;
        var forecast = view.Forecast[0];

        var waterBelow = daysOut >= FinalDays ? WaterBelowDepth : daysOut == FinalDays - 1 ? LateWaterBelowDepth : double.NegativeInfinity;
        if (!target.WateringQueued && depth < waterBelow && forecast.ChanceOfRain < RainLikely)
        {
            Do(game, by => new WaterStrip(stripId, by));
        }

        var chance = forecast.ChanceOfRain;
        if (view.Now.Hour >= 12 && view.Forecast.Count > 1)
        {
            chance = Math.Max(chance, view.Forecast[1].ChanceOfRain);
        }
        var worthProtecting = daysOut <= FinalDays || depth >= ProtectFromDepth;
        var wantCover = worthProtecting
            ? chance >= CoverAtChance
            : chance >= RainLikely && forecast.Rain.High >= HeavyRainMm;

        if (wantCover && !target.Covered && target.CoverOrder == CoverOrder.None)
        {
            Do(game, by => new CoverStrip(stripId, by));
        }
        else if (!wantCover && target.Covered && target.CoverOrder == CoverOrder.None)
        {
            Do(game, by => new UncoverStrip(stripId, by));
        }

        Mow(game, stripId, daysOut);
        Roll(game, stripId, daysOut, forecast.ChanceOfRain);
    }

    private static void Mow(IGame game, StripId stripId, int daysOut)
    {
        var view = game.View;
        var strip = view.Strips[stripId.Number - 1];
        var lastCut = strip.LastMown;
        var daysSince = lastCut == null ? int.MaxValue : (view.Now.Date - lastCut.OrderedAt.Date).Days;
        if (strip.MowingQueued || daysSince == 0 || (daysSince < MowEveryDays && daysOut > 1))
        {
            return;
        }

        var estimate = EstimateHeight(strip, view.Now.Date);
        var height = Math.Round(Math.Max(TargetHeightMm(daysOut), estimate * (1 - MaxCutShare)), 1);
        if (height < estimate)
        {
            Do(game, by => new MowStrip(stripId, height, by));
        }
    }

    private static void Roll(IGame game, StripId stripId, int daysOut, double chanceOfRain)
    {
        var view = game.View;
        var strip = view.Strips[stripId.Number - 1];
        if (strip.RollingQueued || strip.WateringQueued || strip.LastRolled?.OrderedAt.Date == view.Now.Date)
        {
            return;
        }

        if (strip.SurfaceMoisture?.TakenAt.Date != view.Now.Date)
        {
            game.Submit(new TakeReading(stripId, Deputy, ReadingSource.Feel));
            strip = game.View.Strips[stripId.Number - 1];
        }

        var reading = strip.SurfaceMoisture;
        if (reading == null || reading.TakenAt.Date != view.Now.Date)
        {
            return;
        }
        var rollable = reading.Word != null
            ? reading.Word == "damp"
            : (reading.Range.Low + reading.Range.High) / 2 is >= RollFromSurface and <= RollToSurface;
        if (rollable)
        {
            var (roller, minutes) = RollingFor(daysOut);
            Do(game, by => new RollStrip(stripId, roller, minutes, by));
        }
        else if (daysOut >= WetToRollFromDaysOut && reading.Word == "dry" && chanceOfRain < RainLikely)
        {
            Do(game, by => new WaterStrip(stripId, by));
        }
    }

    /// <summary>Gives a job to whoever has the hours, the player first.</summary>
    private static void Do(IGame game, Func<StaffId, IGameCommand> job)
    {
        foreach (var person in game.View.Staff)
        {
            if (game.Submit(job(person.Id)).Accepted)
            {
                return;
            }
        }
    }
}
