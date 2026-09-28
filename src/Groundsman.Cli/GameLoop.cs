using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Inspection;
using Groundsman.Core.Readings;
using Groundsman.Core.Staff;
using Groundsman.Core.Strips;
using Spectre.Console;

namespace Groundsman.Cli;

public sealed class GameLoop
{
    private readonly IAnsiConsole _console;
    private readonly IGame _game;
    private readonly TextReader? _pipedInput;
    private readonly Func<TruthSnapshot>? _inspect;

    /// <param name="pipedInput">
    /// Plain line source for when stdin is redirected, since Spectre's prompts refuse to read it.
    /// </param>
    /// <param name="inspect">Debug mode: shows true values beside readings when given.</param>
    public GameLoop(IAnsiConsole console, IGame game, TextReader? pipedInput = null, Func<TruthSnapshot>? inspect = null)
    {
        _console = console;
        _game = game;
        _pipedInput = pipedInput;
        _inspect = inspect;
    }

    public void Run()
    {
        ShowStatus();
        while (true)
        {
            var text = ReadLine();
            if (text == null)
            {
                return;
            }

            switch (InputParser.Parse(text))
            {
                case QuitInput:
                    return;
                case AdvanceInput:
                    Advance();
                    break;
                case HelpInput:
                    ShowHelp();
                    break;
                case StatusInput:
                    ShowStatus();
                    break;
                case ReadInput { Strip: null } all:
                    var now = _game.View.Now;
                    var core = all.Tool == ReadingSource.SoilCore;
                    foreach (var strip in _game.View.Strips.Where(s => (core ? s.SubsurfaceMoisture : s.SurfaceMoisture)?.TakenAt != now))
                    {
                        if (!Read(strip.Id, all.By, all.Tool))
                        {
                            break;
                        }
                    }
                    break;
                case ReadInput { Strip: { } strip } read:
                    Read(strip, read.By, read.Tool);
                    break;
                case MowInput { Strip: null } mowAll:
                    foreach (var strip in _game.View.Strips.Where(s => !s.MowingQueued))
                    {
                        if (!Order(new MowStrip(strip.Id, mowAll.HeightMm, mowAll.By), $"{strip.Id} is down for mowing to {mowAll.HeightMm:0.#} mm."))
                        {
                            break;
                        }
                    }
                    break;
                case MowInput { Strip: { } mowStrip } mow:
                    Order(new MowStrip(mowStrip, mow.HeightMm, mow.By), $"{mowStrip} is down for mowing to {mow.HeightMm:0.#} mm.");
                    break;
                case RollInput { Strip: null } rollAll:
                    foreach (var strip in _game.View.Strips.Where(s => !s.RollingQueued))
                    {
                        if (!Order(new RollStrip(strip.Id, rollAll.RollerId, rollAll.Minutes, rollAll.By), RollConfirmation(strip.Id, rollAll)))
                        {
                            break;
                        }
                    }
                    break;
                case RollInput { Strip: { } rollStrip } roll:
                    Order(new RollStrip(rollStrip, roll.RollerId, roll.Minutes, roll.By), RollConfirmation(rollStrip, roll));
                    break;
                case WaterInput water:
                    Order(new WaterStrip(water.Strip, water.By), $"{water.Strip} is down for watering.");
                    break;
                case CoverInput cover:
                    Order(new CoverStrip(cover.Strip, cover.By), $"{cover.Strip} is down for covering.");
                    break;
                case UncoverInput uncover:
                    Order(new UncoverStrip(uncover.Strip, uncover.By), $"{uncover.Strip} is down for uncovering.");
                    break;
                case InvalidInput invalid:
                    _console.MarkupLine($"[yellow]{Markup.Escape(invalid.Message)}[/]");
                    break;
            }
        }
    }

    private string? ReadLine()
    {
        if (_pipedInput == null)
        {
            return _console.Prompt(new TextPrompt<string>("[grey]>[/]").AllowEmpty());
        }

        var line = _pipedInput.ReadLine();
        if (line != null)
        {
            _console.MarkupLine($"[grey]>[/] {Markup.Escape(line)}");
        }
        return line;
    }

    private void Advance()
    {
        var result = _game.Advance();
        _console.MarkupLine($"[grey]Advanced {result.HoursRun} hours to {Format.Time(result.To)}[/]");
        ShowStatus();
    }

    private bool Read(StripId strip, StaffId? by, ReadingSource tool)
    {
        if (!Report(_game.Submit(new TakeReading(strip, by, tool))))
        {
            return false;
        }

        var view = _game.View.Strips.Single(s => s.Id == strip);
        if (tool == ReadingSource.SoilCore)
        {
            _console.MarkupLine($"{strip} cores [bold]{Format.Percent(view.SubsurfaceMoisture!.Range)}[/] below the surface.");
            return true;
        }

        var reading = view.SurfaceMoisture!;
        _console.MarkupLine(reading.Word != null
            ? $"{strip} feels [bold]{Markup.Escape(reading.Word)}[/]."
            : $"{strip} reads [bold]{Format.Percent(reading.Range)}[/] surface moisture.");
        return true;
    }

    private string RollConfirmation(StripId strip, RollInput roll)
    {
        var name = _game.View.Rollers.FirstOrDefault(r => r.Id == roll.RollerId)?.Name ?? roll.RollerId;
        return $"{strip} is down for {roll.Minutes:0} minutes with the {name}.";
    }

    private bool Order(IGameCommand command, string confirmation)
    {
        if (!Report(_game.Submit(command)))
        {
            return false;
        }
        _console.MarkupLine(confirmation);
        return true;
    }

    private bool Report(CommandResult result)
    {
        if (!result.Accepted)
        {
            _console.MarkupLine($"[red]{Markup.Escape(result.Reason ?? "Not possible.")}[/]");
        }
        return result.Accepted;
    }

    private void ShowStatus()
    {
        var view = _game.View;

        _console.WriteLine();
        _console.Write(new Rule($"[green]{Markup.Escape(view.GroundName)}[/]  {Format.Time(view.Now)}").LeftJustified());
        _console.MarkupLine($"{Format.Pace(view.Pace)}. Next match: {Format.NextMatch(view.NextFixture, view.Now)}.");
        if (view.Weather is { } weather)
        {
            _console.MarkupLine(Format.Weather(weather));
        }

        var truth = _inspect?.Invoke();
        var table = new Table().Border(TableBorder.Simple)
            .AddColumn("Strip")
            .AddColumn("Surface")
            .AddColumn("Read")
            .AddColumn("Below")
            .AddColumn("Cored")
            .AddColumn("Cut")
            .AddColumn("Rolled")
            .AddColumn("Cover")
            .AddColumn("Orders");
        if (truth != null)
        {
            table.AddColumn("[magenta]Truth (surface / below)[/]");
        }

        for (var i = 0; i < view.Strips.Count; i++)
        {
            var strip = view.Strips[i];
            var reading = strip.SurfaceMoisture;
            var cells = new List<string>
            {
                strip.Id.Number.ToString(),
                reading == null ? "[grey]–[/]" : Markup.Escape(Format.Reading(reading, strip.SurfaceMoistureNow!.Value)),
                reading == null ? "" : $"{Format.Age(reading.TakenAt, view.Now)}, {Markup.Escape(Format.FirstName(view.Staff.Single(s => s.Id == reading.TakenBy).Name))}",
                strip.SubsurfaceMoisture == null ? "" : Markup.Escape(Format.Percent(strip.SubsurfaceMoistureNow!.Value)),
                strip.SubsurfaceMoisture == null ? "" : Format.Age(strip.SubsurfaceMoisture.TakenAt, view.Now),
                strip.LastMown == null ? "" : $"{strip.LastMown.HeightMm:0.#}mm {Format.Age(strip.LastMown.OrderedAt, view.Now)}",
                strip.LastRolled == null ? "" : $"{Markup.Escape(strip.LastRolled.RollerId)} {strip.LastRolled.Minutes:0}m {Format.Age(strip.LastRolled.OrderedAt, view.Now)}",
                strip.Covered ? "covered" : "",
                Format.Orders(strip),
            };
            if (truth != null)
            {
                var t = truth.Strips[i];
                cells.Add($"[magenta]{t.SurfaceMoisture:0.0}% / {t.SubsurfaceMoisture:0.0}%[/]");
            }
            table.AddRow(cells.ToArray());
        }

        _console.Write(table);
        ShowForecast(view);
        _console.MarkupLine($"Covers free: {view.CoversFree} of {view.CoversOwned}.");
        _console.MarkupLine(Markup.Escape(Format.Hours(view.Staff)));
        _console.MarkupLine("[grey]Enter to advance, h for help.[/]");
    }

    private void ShowForecast(GameView view)
    {
        var table = new Table().Border(TableBorder.Simple).Title("Forecast").AddColumn("");
        foreach (var day in view.Forecast)
        {
            table.AddColumn(day.Date == view.Now.Date ? "Today" : day.Date.ToString("ddd d", System.Globalization.CultureInfo.InvariantCulture));
        }
        table.AddRow(new[] { "Chance" }.Concat(view.Forecast.Select(d => Format.Chance(d.ChanceOfRain))).ToArray());
        table.AddRow(new[] { "Rain" }.Concat(view.Forecast.Select(d => Format.Rain(d.Rain))).ToArray());
        table.AddRow(new[] { "High" }.Concat(view.Forecast.Select(d => Format.Temperature(d.MaxTemperature))).ToArray());
        _console.Write(table);
    }

    private void ShowHelp()
    {
        var table = new Table().Border(TableBorder.None).HideHeaders()
            .AddColumn("Command")
            .AddColumn("What it does")
            .AddRow("r <strip> [[name]]", "Take a moisture probe reading of a strip; add a name to send someone else")
            .AddRow("r all", "Read every strip")
            .AddRow("f <strip>", "Feel a strip: quick, gives dry, damp or wet, can be wrong")
            .AddRow("d <strip>", "Take a soil core: slow, reads moisture below the surface")
            .AddRow("w <strip>", "Water a strip (done when time advances)")
            .AddRow("m <strip> <mm>", "Mow a strip to a height; more than a third off at once scalps it")
            .AddRow("l <strip> <roller> <min>", $"Roll a strip ({string.Join(", ", _game.View.Rollers.Select(r => r.Id))}): only moist, never wet")
            .AddRow("c <strip>", "Put a cover on a strip: keeps rain off, slows drying")
            .AddRow("u <strip>", "Take a strip's cover off")
            .AddRow("", "Every strip job takes a name, e.g. w 3 sam. You do it if none is given.")
            .AddRow("s", "Show the ground again")
            .AddRow("Enter or a", "Advance to the next decision point")
            .AddRow("q", "Quit");

        _console.Write(table);
    }
}
