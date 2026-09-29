using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;
using Newtonsoft.Json;

namespace Groundsman.Tests.Replays;

public class ReplayTests
{
    [Fact]
    public void A_scripted_season_start_replays_exactly()
    {
        Snapshot.Match("replay-season-start", Run());
    }

    [Fact]
    public void Running_twice_gives_identical_output()
    {
        Assert.Equal(Run(), Run());
    }

    private static string Run()
    {
        var matchDays = new[] { new DateTime(2027, 4, 16), new DateTime(2027, 4, 17) };
        var game = new Game(TestContent.Setup(new GameTime(2027, 3, 25, 7), matchDays, seed: 2026));
        var turns = new List<object>();

        for (var turn = 0; turn < 80; turn++)
        {
            if (turn % 5 == 0)
            {
                game.Submit(new WaterStrip(new StripId(turn % 12 + 1)));
            }
            if (turn % 3 == 0)
            {
                game.Submit(new TakeReading(new StripId(turn % 7 + 1)));
            }
            if (turn % 4 == 0)
            {
                game.Submit(new MowStrip(new StripId(turn % 5 + 1), 8));
            }
            if (turn % 2 == 0)
            {
                game.Submit(new RollStrip(new StripId(turn % 6 + 1), turn % 3 == 0 ? "heavy" : "light", 30));
            }

            game.Advance();
            turns.Add(Record(game));
        }

        return JsonConvert.SerializeObject(turns, Formatting.Indented);
    }

    // Values are rounded so the snapshot catches rule changes, not last-digit differences
    // between maths libraries.
    private static object Record(Game game)
    {
        var view = game.View;
        var truth = game.Inspect();

        return new
        {
            time = view.Now.ToString(),
            rain24h = Round(view.Weather!.RainLast24HoursMm),
            temperature = Round(view.Weather.Temperature),
            wind = Round(view.Weather.WindKph),
            surface = string.Join(" ", truth.Strips.Select(s => Round(s.SurfaceMoisture))),
            subsurface = string.Join(" ", truth.Strips.Select(s => Round(s.SubsurfaceMoisture))),
            grass = string.Join(" ", truth.Strips.Select(s => $"{Round(s.GrassHeightMm)}mm/{Round(s.GrassCover)}%")),
            compaction = string.Join(" ", truth.Strips.Select(s => $"{Round(s.Compaction)}/{Round(s.StructureDamage)}")),
            wear = string.Join(" ", truth.Strips.Select(s => $"{Round(s.Footholes)}/{Round(s.Rough)}/{Round(s.SurfaceWear)}/{Round(s.Cracks)}")),
            pitch = truth.Strips.Select(s => $"pace {Round(s.Pitch.Pace)} bounce {Round(s.Pitch.Bounce)} true {Round(s.Pitch.Consistency)} carry {Round(s.Pitch.Carry)} seam {Round(s.Pitch.Seam)} spin {Round(s.Pitch.Spin)} cracks {Round(s.Pitch.Cracking)}").ToArray(),
            forecast = view.Forecast
                .Select(d => $"{d.Date:MM-dd} rain {Round(d.Rain.Low)} to {Round(d.Rain.High)} chance {Round(d.ChanceOfRain)}, high {Round(d.MaxTemperature.Low)} to {Round(d.MaxTemperature.High)}")
                .ToArray(),
            readings = view.Strips
                .Where(s => s.SurfaceMoisture != null)
                .Select(s => $"{s.Id.Number}: {Round(s.SurfaceMoisture!.Range.Low)} to {Round(s.SurfaceMoisture.Range.High)} at {s.SurfaceMoisture.TakenAt}, now {Round(s.SurfaceMoistureNow!.Value.Low)} to {Round(s.SurfaceMoistureNow.Value.High)}")
                .ToArray(),
        };
    }

    private static string Round(double value) => Math.Round(value, 6).ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
}
