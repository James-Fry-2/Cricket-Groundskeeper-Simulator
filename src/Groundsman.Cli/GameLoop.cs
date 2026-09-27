using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Strips;
using Spectre.Console;

namespace Groundsman.Cli;

public sealed class GameLoop
{
    private readonly IAnsiConsole _console;
    private readonly IGame _game;
    private readonly TextReader? _pipedInput;

    /// <param name="pipedInput">
    /// Plain line source for when stdin is redirected, since Spectre's prompts refuse to read it.
    /// </param>
    public GameLoop(IAnsiConsole console, IGame game, TextReader? pipedInput = null)
    {
        _console = console;
        _game = game;
        _pipedInput = pipedInput;
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
                case ReadInput { Strip: null }:
                    foreach (var strip in _game.View.Strips)
                    {
                        Read(strip.Id);
                    }
                    break;
                case ReadInput { Strip: { } strip }:
                    Read(strip);
                    break;
                case WaterInput water:
                    Water(water.Strip);
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

    private void Read(StripId strip)
    {
        var result = _game.Submit(new TakeReading(strip));
        if (!Report(result))
        {
            return;
        }

        var reading = _game.View.Strips.Single(s => s.Id == strip).SurfaceMoisture!;
        _console.MarkupLine($"{strip} reads [bold]{Format.Percent(reading.Range)}[/] surface moisture.");
    }

    private void Water(StripId strip)
    {
        if (Report(_game.Submit(new WaterStrip(strip))))
        {
            _console.MarkupLine($"{strip} is down for watering.");
        }
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
        _console.MarkupLine($"{Format.Pace(view.Pace)}. Next match: {Format.NextMatch(view.NextMatchDay, view.Now)}.");

        var table = new Table().Border(TableBorder.Simple)
            .AddColumn("Strip")
            .AddColumn("Surface moisture")
            .AddColumn("Read")
            .AddColumn("Orders");

        foreach (var strip in view.Strips)
        {
            var reading = strip.SurfaceMoisture;
            table.AddRow(
                strip.Id.Number.ToString(),
                reading == null ? "[grey]no reading[/]" : Format.Percent(reading.Range),
                reading == null ? "" : Format.Age(reading.TakenAt, view.Now),
                strip.WateringQueued ? "[blue]water[/]" : "");
        }

        _console.Write(table);
        _console.MarkupLine("[grey]Enter to advance, h for help.[/]");
    }

    private void ShowHelp()
    {
        var table = new Table().Border(TableBorder.None).HideHeaders()
            .AddColumn("Command")
            .AddColumn("What it does")
            .AddRow("r <strip>", "Take a moisture probe reading of a strip")
            .AddRow("r all", "Read every strip")
            .AddRow("w <strip>", "Water a strip (done when time advances)")
            .AddRow("s", "Show the ground again")
            .AddRow("Enter or a", "Advance to the next decision point")
            .AddRow("q", "Quit");

        _console.Write(table);
    }
}
