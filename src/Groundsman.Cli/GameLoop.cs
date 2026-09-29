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
    private Groundsman.Core.Fixture? _commentaryFixture;
    private int _commentaryShown;
    private Groundsman.Core.Fixture? _verdictShown;

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
        ShowStatus(withScoreboard: true);
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
                    ShowStatus(withScoreboard: true);
                    break;
                case RecordInput:
                    ShowRecord();
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
                case CleanInput clean:
                    Order(new CleanFootholes(clean.Strip, clean.By), $"{clean.Strip}'s footholes will be cleaned and dried.");
                    break;
                case FillInput fill:
                    Order(new FillFootholes(fill.Strip, fill.By), $"{fill.Strip}'s footholes will be filled overnight.");
                    break;
                case RepairInput repair:
                    Order(new RepairEnds(repair.Strip, repair.By), $"{repair.Strip} is down for end repairs.");
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
        var view = _game.View;
        if (view.Interval != null || HasNewCommentary(view))
        {
            ShowScoreboard(view);
            ShowNewCommentary(view);
        }
        ShowVerdictOnce(view);
        ShowStatus(withScoreboard: false);
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

    private void ShowStatus(bool withScoreboard)
    {
        var view = _game.View;

        _console.WriteLine();
        _console.Write(new Rule($"[green]{Markup.Escape(view.GroundName)}[/]  {Format.Time(view.Now)}").LeftJustified());
        _console.MarkupLine($"{Format.Pace(view.Pace)}. Next match: {Format.NextMatch(view.NextFixture, view.Now)}.");
        if (withScoreboard && view.Interval != null)
        {
            ShowScoreboard(view);
        }
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
            .AddColumn("Ends")
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
                strip.LastRepaired is { } repaired ? $"repaired {Format.Age(repaired, view.Now)}" : "",
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
        if (view.DemeritsActive > 0)
        {
            _console.MarkupLine($"[{(view.Banned ? "red" : "yellow")}]Demerits in the last five years: {view.DemeritsActive}{(view.Banned ? ". The ground has lost the right to host." : ".")}[/]");
        }
        _console.MarkupLine(Markup.Escape(Format.Hours(view.Staff)));
        if (view.Interval is { } interval)
        {
            _console.MarkupLine($"[bold]{Markup.Escape(Format.Interval(interval))}[/]");
        }
        _console.MarkupLine("[grey]Enter to advance, h for help.[/]");
    }

    private bool HasNewCommentary(GameView view) =>
        view.LatestMatch is { } match && (match.Fixture != _commentaryFixture || match.Commentary.Count > _commentaryShown);

    /// <summary>Commentary added since the last turn, each line once.</summary>
    private void ShowNewCommentary(GameView view)
    {
        var match = view.LatestMatch;
        if (match == null)
        {
            return;
        }
        if (match.Fixture != _commentaryFixture)
        {
            _commentaryFixture = match.Fixture;
            _commentaryShown = 0;
        }

        foreach (var line in match.Commentary.Skip(_commentaryShown))
        {
            _console.MarkupLine($"  [italic]{line.Hour.Hour:00}:00[/] {Markup.Escape(line.Text)}");
        }
        _commentaryShown = match.Commentary.Count;
    }

    private void ShowScoreboard(GameView view)
    {
        var match = view.LatestMatch;
        if (match == null || match.Innings.Count == 0)
        {
            return;
        }

        var fixture = match.Fixture;
        var day = fixture.Days > 1 && !match.Finished ? $", day {(view.Now.Date - fixture.Start).Days + 1} of {fixture.Days}" : "";
        _console.MarkupLine($"[bold]{Markup.Escape(fixture.Format.Name)} v {Markup.Escape(fixture.Opponent.Name)}{day}, strip {fixture.Strip.Number}[/]");
        foreach (var innings in Format.Started(match))
        {
            _console.MarkupLine("  " + Markup.Escape(Format.Innings(innings)));
        }
        if (match.Result != null)
        {
            _console.MarkupLine($"  [bold]{Markup.Escape(match.Result.Text)}[/]");
        }
    }

    /// <summary>The referee's verdict, the first time a turn finds it.</summary>
    private void ShowVerdictOnce(GameView view)
    {
        var match = view.LatestMatch;
        if (match?.Rating is not { } rating || match.Fixture == _verdictShown)
        {
            return;
        }
        _verdictShown = match.Fixture;

        var colour = GradeColour(rating.Grade);
        var lines = new List<string> { $"[bold {colour}]{Format.Grade(rating.Grade)}[/]" };
        lines.AddRange(rating.Reasons.Select(r => "  " + Markup.Escape(r)));
        lines.Add("");
        lines.AddRange(Format.Started(match).Select(i => Markup.Escape(Format.Innings(i))));
        if (match.Result != null)
        {
            lines.Add(Markup.Escape(match.Result.Text));
        }
        lines.Add("");
        var demerits = rating.Demerits == 0 ? "No demerits" : $"[{colour}]{Format.Demerits(rating.Demerits)}[/]";
        lines.Add($"{demerits} for this match. {Format.Demerits(view.DemeritsActive)} in the last five years.");

        var title = $"The referee's verdict: {Markup.Escape(match.Fixture.Format.Name)} v {Markup.Escape(match.Fixture.Opponent.Name)}, strip {match.Fixture.Strip.Number}";
        _console.Write(new Panel(new Markup(string.Join("\n", lines))).Header(title).Border(BoxBorder.Rounded));
    }

    private void ShowRecord()
    {
        var finished = _game.View.Matches.Where(m => m.Finished).ToList();
        if (finished.Count == 0)
        {
            _console.MarkupLine("No matches played yet.");
            return;
        }

        var table = new Table().Border(TableBorder.Simple).Title("Season record")
            .AddColumn(new TableColumn("Date").NoWrap())
            .AddColumn(new TableColumn("Match").NoWrap())
            .AddColumn("Strip")
            .AddColumn("Result")
            .AddColumn(new TableColumn("Rating").NoWrap());
        foreach (var match in finished)
        {
            var rating = match.Rating;
            table.AddRow(
                match.Fixture.Start.ToString("d MMM", System.Globalization.CultureInfo.InvariantCulture),
                Markup.Escape($"{match.Fixture.Format.Name} v {match.Fixture.Opponent.Name}"),
                match.Fixture.Strip.Number.ToString(),
                Markup.Escape(match.Result?.Text ?? ""),
                rating == null ? "[grey]not rated[/]" : $"[{GradeColour(rating.Grade)}]{Format.Grade(rating.Grade)}{(rating.Demerits > 0 ? $" ({rating.Demerits})" : "")}[/]");
        }
        _console.Write(table);
        _console.MarkupLine($"{Format.Demerits(_game.View.DemeritsActive)} in the last five years.");
    }

    private static string GradeColour(Groundsman.Core.Match.PitchGrade grade) =>
        grade <= Groundsman.Core.Match.PitchGrade.Satisfactory ? "green" : "red";

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
            .AddRow("e <strip>", "Repair the ends after a match: fill and seed footholes and rough")
            .AddRow("clean <strip>", "During a match: clean and dry the footholes before play or at a break")
            .AddRow("fill <strip>", "During a match over one day: fill the footholes at close of play")
            .AddRow("c <strip>", "Put a cover on a strip: keeps rain off, slows drying")
            .AddRow("u <strip>", "Take a strip's cover off")
            .AddRow("", "Every strip job takes a name, e.g. w 3 sam. You do it if none is given.")
            .AddRow("s", "Show the ground again")
            .AddRow("v", "The season record: every match, its result and the referee's rating")
            .AddRow("Enter or a", "Advance to the next decision point")
            .AddRow("q", "Quit");

        _console.Write(table);
    }
}
