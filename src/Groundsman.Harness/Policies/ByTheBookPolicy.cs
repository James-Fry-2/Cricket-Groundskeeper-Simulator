using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Staff;

namespace Groundsman.Harness.Policies;

/// <summary>
/// The research's preparation loop (docs/research.md section 3), from readings and the
/// forecast only. The probe reads the surface, so moisture at depth comes from following the
/// schedule: water deeply four to five days out, then taper and let the surface dry, covering
/// against rain in the final days.
/// </summary>
public sealed class ByTheBookPolicy : IPolicy
{
    public const int PrepStartsDaysOut = 10;
    public const int DeepWaterDaysOut = 5;
    public const int FinalDays = 3;

    /// <summary>Chance of rain at which watering is put off, and heavy rain is covered against early on.</summary>
    public const double RainLikely = 0.6;

    /// <summary>Chance of rain at which the strip is covered in the final days.</summary>
    public const double CoverAtChance = 0.4;

    /// <summary>A forecast top rain amount, mm, worth covering against before the final days.</summary>
    public const double HeavyRainMm = 8;

    /// <summary>Reading midpoint, %, above which the strip is already wet enough not to water.</summary>
    public const double ReadsWet = 28;

    /// <summary>Reading midpoint, %, below which a second deep watering goes on four days out.</summary>
    public const double ReadsDryish = 22;

    /// <summary>Reading midpoint, %, below which a light top-up goes on three days out.</summary>
    public const double ReadsParched = 14;

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
        if (daysOut >= 1 && target.SurfaceMoisture?.TakenAt.Date != view.Now.Date)
        {
            game.Submit(new TakeReading(fixture.Strip, Deputy));
        }

        view = game.View;
        target = view.Strips[fixture.Strip.Number - 1];
        var reading = target.SurfaceMoistureNow;
        var midpoint = reading == null ? (double?)null : (reading.Value.Low + reading.Value.High) / 2;
        var today = view.Forecast[0];

        if (!target.WateringQueued && today.ChanceOfRain < RainLikely && ShouldWater(daysOut, midpoint))
        {
            game.Submit(new WaterStrip(fixture.Strip));
        }

        bool wantCover;
        if (daysOut <= FinalDays)
        {
            var chance = today.ChanceOfRain;
            if (view.Now.Hour >= 12 && view.Forecast.Count > 1)
            {
                chance = Math.Max(chance, view.Forecast[1].ChanceOfRain);
            }
            wantCover = chance >= CoverAtChance;
        }
        else
        {
            wantCover = today.ChanceOfRain >= RainLikely && today.Rain.High >= HeavyRainMm;
        }

        if (wantCover && !target.Covered && target.CoverOrder == CoverOrder.None)
        {
            game.Submit(new CoverStrip(fixture.Strip));
        }
        else if (!wantCover && target.Covered && target.CoverOrder == CoverOrder.None)
        {
            game.Submit(new UncoverStrip(fixture.Strip));
        }
    }

    private static bool ShouldWater(int daysOut, double? midpoint) => daysOut switch
    {
        DeepWaterDaysOut => midpoint == null || midpoint <= ReadsWet,
        DeepWaterDaysOut - 1 => midpoint <= ReadsDryish,
        FinalDays => midpoint <= ReadsParched,
        _ => false,
    };
}
