using System.Text;
using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Time;

namespace Groundsman.Harness;

/// <summary>
/// Plays a season with no commands and records the truth at every decision point, for tuning
/// weather and moisture by eye.
/// </summary>
public static class SeasonTrace
{
    public static string Run(GameContent content, SeasonSettings season, ulong seed, DateTime end)
    {
        var start = GameTime.OnDate(season.Start, content.Calendar.MorningHour);
        var game = new Game(new GameSetup(content, start, season.Fixtures, seed));
        var stop = GameTime.OnDate(end, content.Calendar.MorningHour);

        var csv = new StringBuilder("time,rain_24h_mm,temperature");
        foreach (var strip in content.Ground.Strips)
        {
            csv.Append($",s{strip.Id.Number}_surface,s{strip.Id.Number}_subsurface,s{strip.Id.Number}_grass_mm,s{strip.Id.Number}_cover,s{strip.Id.Number}_hardness");
        }
        csv.Append('\n');

        while (true)
        {
            var view = game.View;
            var truth = game.Inspect();
            csv.Append(view.Now.ToString());
            csv.Append(',').Append(view.Weather == null ? "" : Csv.Number(view.Weather.RainLast24HoursMm));
            csv.Append(',').Append(view.Weather == null ? "" : Csv.Number(view.Weather.Temperature));
            foreach (var strip in truth.Strips)
            {
                csv.Append(',').Append(Csv.Number(strip.SurfaceMoisture));
                csv.Append(',').Append(Csv.Number(strip.SubsurfaceMoisture));
                csv.Append(',').Append(Csv.Number(strip.GrassHeightMm));
                csv.Append(',').Append(Csv.Number(strip.GrassCover));
                csv.Append(',').Append(Csv.Number(strip.Hardness));
            }
            csv.Append('\n');

            if (view.Now >= stop)
            {
                return csv.ToString();
            }
            game.Advance();
        }
    }
}
