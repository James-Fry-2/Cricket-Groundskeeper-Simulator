using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Readings;
using Groundsman.Core.Staff;

namespace Groundsman.Harness.Policies;

/// <summary>
/// The research's preparation loop (docs/research.md section 3), from readings and the
/// forecast only: aim for moisture at depth with a dry surface, measured as professionals do
/// with a soil core. Water into a dry profile while rain is unlikely, protect a profile that's
/// there from rain, let a wet one dry in the open, and keep rain off in the final days.
/// </summary>
public sealed class ByTheBookPolicy : IPolicy
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

    private static readonly StaffId Deputy = new StaffId("sam");

    public string Name => "by the book";

    public void PlayTurn(IGame game)
    {
        var view = game.View;
        var fixture = view.NextFixture;

        foreach (var strip in view.Strips)
        {
            if (strip.Covered && strip.CoverOrder == CoverOrder.None && strip.Id != fixture?.Strip)
            {
                game.Submit(new UncoverStrip(strip.Id));
            }
        }

        if (fixture == null)
        {
            return;
        }

        var daysOut = (fixture.Start - view.Now.Date).Days;
        if (daysOut > PrepStartsDaysOut)
        {
            return;
        }

        var target = view.Strips[fixture.Strip.Number - 1];
        var today = view.Now.Date;
        if (daysOut >= 1 && daysOut <= CoringStartsDaysOut && target.SubsurfaceMoisture?.TakenAt.Date != today)
        {
            game.Submit(new TakeReading(fixture.Strip, Deputy, ReadingSource.SoilCore));
        }
        if (daysOut >= 1 && daysOut <= FinalDays && target.SurfaceMoisture?.TakenAt.Date != today)
        {
            game.Submit(new TakeReading(fixture.Strip, Deputy));
        }

        view = game.View;
        target = view.Strips[fixture.Strip.Number - 1];
        var depth = target.SubsurfaceMoistureNow is { } core ? (core.Low + core.High) / 2 : (double?)null;
        var forecast = view.Forecast[0];

        var waterBelow = daysOut >= FinalDays ? WaterBelowDepth : daysOut == FinalDays - 1 ? LateWaterBelowDepth : double.NegativeInfinity;
        if (!target.WateringQueued && depth < waterBelow && forecast.ChanceOfRain < RainLikely)
        {
            game.Submit(new WaterStrip(fixture.Strip));
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
            game.Submit(new CoverStrip(fixture.Strip));
        }
        else if (!wantCover && target.Covered && target.CoverOrder == CoverOrder.None)
        {
            game.Submit(new UncoverStrip(fixture.Strip));
        }
    }
}
