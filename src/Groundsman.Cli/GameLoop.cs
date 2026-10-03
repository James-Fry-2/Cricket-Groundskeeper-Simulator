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
    private readonly TextReader? _pipedInput;
    private readonly Func<IGame>? _nextSeason;
    private IGame _game;
    private Func<TruthSnapshot>? _inspect;
    private Groundsman.Core.Fixture? _commentaryFixture;
    private int _commentaryShown;
    private Groundsman.Core.Fixture? _verdictShown;
    private bool _reviewShown;

    /// <param name="pipedInput">
    /// Plain line source for when stdin is redirected, since Spectre's prompts refuse to read it.
    /// </param>
    /// <param name="inspect">Debug mode: shows true values beside readings when given.</param>
    /// <param name="nextSeason">Starts a new season once this one is over; without it, the season can't be replayed.</param>
    public GameLoop(IAnsiConsole console, IGame game, TextReader? pipedInput = null, Func<TruthSnapshot>? inspect = null, Func<IGame>? nextSeason = null)
    {
        _console = console;
        _game = game;
        _pipedInput = pipedInput;
        _inspect = inspect;
        _nextSeason = nextSeason;
        MarkShown();
    }

    // A resumed season has already shown its commentary, verdicts and review.
    private void MarkShown()
    {
        var view = _game.View;
        _commentaryFixture = view.LatestMatch?.Fixture;
        _commentaryShown = view.LatestMatch?.Commentary.Count ?? 0;
        _verdictShown = view.LatestMatch?.Rating != null ? view.LatestMatch.Fixture : null;
        _reviewShown = view.Review != null;
    }

    public void Run()
    {
        Log("session_start", ("resumed", (_game as RecordingGame)?.Data.Turns > 0));
        ShowNotices(_game.View);
        ShowStatus(withScoreboard: true);
        Log("screen", ("name", "status"));
        while (true)
        {
            var text = ReadLine();
            if (text == null)
            {
                Log("session_end");
                return;
            }

            switch (InputParser.Parse(text))
            {
                case QuitInput:
                    Log("session_end");
                    return;
                case AdvanceInput:
                    Advance();
                    break;
                case HelpInput:
                    ShowHelp();
                    Log("screen", ("name", "help"));
                    break;
                case StatusInput:
                    ShowStatus(withScoreboard: true);
                    Log("screen", ("name", "status"));
                    break;
                case RecordInput:
                    ShowRecord();
                    Log("screen", ("name", "record"));
                    break;
                case FixturesInput:
                    ShowFixtures();
                    Log("screen", ("name", "fixtures"));
                    break;
                case AssignInput assign:
                    Assign(assign);
                    break;
                case AnswerInput answer:
                    Answer(answer);
                    break;
                case NewSeasonInput:
                    NewSeason();
                    break;
                case SaveInput:
                    SaveCopy();
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
                    Log("invalid", ("text", text.Trim().Length > 40 ? text.Trim().Substring(0, 40) : text.Trim()));
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
        if (_game.View.Review != null)
        {
            _console.MarkupLine("[yellow]The season is over. Type new for another season, or q to quit. s, v and x still show this one.[/]");
            return;
        }

        var result = _game.Advance();
        _console.MarkupLine($"[grey]Advanced {result.HoursRun} hours to {Format.Time(result.To)}[/]");
        var view = _game.View;
        ShowNotices(view);
        if (view.Interval != null || HasNewCommentary(view))
        {
            ShowScoreboard(view);
            ShowNewCommentary(view);
        }
        ShowVerdictOnce(view);
        if (view.Review is { } review && !_reviewShown)
        {
            _reviewShown = true;
            ShowReview(review);
            Log("screen", ("name", "review"));
            _console.MarkupLine("[yellow]The season is over. Type new for another season, or q to quit.[/]");
            return;
        }
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
            ? $"{strip} feels [bold]{Markup.Escape(reading.Word)}[/]. The ends look {Format.EndsState(view.Ends!.State)}."
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
            .AddColumn(new TableColumn("Surface").NoWrap())
            .AddColumn(new TableColumn("Below").NoWrap())
            .AddColumn(new TableColumn("Cut").NoWrap())
            .AddColumn("Rolled")
            .AddColumn("Played")
            .AddColumn("Ends")
            .AddColumn("Status");
        if (truth != null)
        {
            table.AddColumn("[magenta]Truth (surface / below)[/]");
        }

        // Tight padding keeps the table inside an 80-column terminal.
        foreach (var column in table.Columns)
        {
            column.Padding = new Padding(0, 0, 1, 0);
        }

        for (var i = 0; i < view.Strips.Count; i++)
        {
            var strip = view.Strips[i];
            var reading = strip.SurfaceMoisture;
            var cells = new List<string>
            {
                strip.Centre ? $"{strip.Id.Number}c" : strip.Id.Number.ToString(),
                reading == null ? "[grey]–[/]" : Markup.Escape($"{Format.Reading(reading, strip.SurfaceMoistureNow!.Value)} {Format.Ago(reading.TakenAt.Date, view.Now)} {Format.FirstName(view.Staff.Single(s => s.Id == reading.TakenBy).Name)}"),
                strip.SubsurfaceMoisture == null ? "" : Markup.Escape($"{Format.Percent(strip.SubsurfaceMoistureNow!.Value)} {Format.Ago(strip.SubsurfaceMoisture.TakenAt.Date, view.Now)}"),
                strip.LastMown == null ? "" : $"{strip.LastMown.HeightMm:0.#}mm {Format.Ago(strip.LastMown.OrderedAt.Date, view.Now)}",
                strip.LastRolled == null ? "" : $"{Markup.Escape(strip.LastRolled.RollerId)} {strip.LastRolled.Minutes:0}m {Format.Ago(strip.LastRolled.OrderedAt.Date, view.Now)}",
                strip.LastPlayed is { } played && played.End <= view.Now.Date ? Format.Ago(played.End, view.Now) : "",
                Markup.Escape(Format.Ends(strip, view.Now)),
                string.Join(", ", new[] { strip.Covered ? "covered" : "", Format.Orders(strip) }.Where(p => p.Length > 0)),
            };
            if (truth != null)
            {
                var t = truth.Strips[i];
                cells.Add($"[magenta]{t.SurfaceMoisture:0.0}% / {t.SubsurfaceMoisture:0.0}%[/]");
            }
            table.AddRow(cells.ToArray());
        }

        _console.Write(table);
        _console.MarkupLine("[grey]c: a centre strip, wanted for televised matches. Ages in days: 0d is today. rep: ends repaired.[/]");
        ShowForecast(view);
        _console.MarkupLine($"Covers free: {view.CoversFree} of {view.CoversOwned}.");
        if (view.DemeritsActive > 0)
        {
            _console.MarkupLine($"[{(view.Banned ? "red" : "yellow")}]Demerits in the last five years: {view.DemeritsActive}{(view.Banned ? ". The ground has lost the right to host." : ".")}[/]");
        }
        _console.MarkupLine(Markup.Escape(Format.Hours(view.Staff)));
        _console.MarkupLine(Markup.Escape(Format.Satisfaction(view.Stakeholders)));
        var open = view.Requests.Select((r, i) => (Request: r, Number: i + 1)).Where(r => r.Request.Status == Groundsman.Core.Pressures.RequestStatus.Open).ToList();
        foreach (var (request, number) in open)
        {
            _console.MarkupLine($"[yellow]Waiting for your answer by {Format.Day(request.AnswerBy)}: {Markup.Escape(Format.Who(request.Stakeholder))} wants {Markup.Escape(Format.Request(request.Kind))} for {Markup.Escape(Format.Match(request.Fixture))}. yes {number} or no {number}.[/]");
        }
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
        _console.MarkupLine($"[bold]{Markup.Escape(fixture.Format.Name)} v {Markup.Escape(fixture.Opponent.Name)}{day}, strip {match.Strip.Number}[/]");
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
        Log("screen", ("name", "verdict"));

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

        var title = $"The referee's verdict: {Markup.Escape(match.Fixture.Format.Name)} v {Markup.Escape(match.Fixture.Opponent.Name)}, strip {match.Strip.Number}";
        _console.Write(new Panel(new Markup(string.Join("\n", lines))).Header(title).Border(BoxBorder.Rounded));
    }

    private void ShowNotices(GameView view)
    {
        foreach (var notice in view.Notices)
        {
            var number = notice is Groundsman.Core.Pressures.RequestNotice asked
                ? view.Requests.ToList().FindIndex(r => r.Id == asked.Request.Id) + 1
                : 0;
            _console.MarkupLine($"[yellow]{Markup.Escape(Format.Notice(notice, number))}[/]");
        }
    }

    private void Log(string name, params (string Key, object? Value)[] fields) =>
        (_game as RecordingGame)?.Log(name, fields.ToDictionary(f => f.Key, f => f.Value));

    private void SaveCopy()
    {
        if (_game is not RecordingGame { AutosavePath: { } path } recording)
        {
            _console.MarkupLine("[red]This season isn't being saved.[/]");
            return;
        }
        var copy = Path.Combine(Path.GetDirectoryName(path)!, $"save-turn-{recording.Data.Turns}.json");
        SaveFile.Write(copy, recording.Data);
        _console.MarkupLine($"Saved a copy to {Markup.Escape(copy)}. The season also saves itself after every turn.");
    }

    private void NewSeason()
    {
        if (_game.View.Review == null)
        {
            _console.MarkupLine("[red]The season isn't over yet.[/]");
            return;
        }
        if (_nextSeason == null)
        {
            _console.MarkupLine("[red]No new season is available here.[/]");
            return;
        }

        Log("session_end");
        _game = _nextSeason();
        Log("session_start", ("resumed", false));
        if (_inspect != null)
        {
            _inspect = _game is RecordingGame recording ? recording.Inner.Inspect : _game is Game game ? game.Inspect : _inspect;
        }
        MarkShown();
        _console.MarkupLine("[green]A new season begins.[/]");
        ShowNotices(_game.View);
        ShowStatus(withScoreboard: true);
    }

    private void ShowReview(Groundsman.Core.Pressures.SeasonReview review)
    {
        var lines = new List<string>();
        foreach (var stakeholder in review.Stakeholders)
        {
            lines.Add($"[bold]{Markup.Escape(Format.Who(stakeholder.Stakeholder))}[/] is {Format.Mood(stakeholder.Mood)} ({stakeholder.Satisfaction:0} of 100).");
            foreach (var reason in stakeholder.TopReasons)
            {
                lines.Add("  " + Markup.Escape(Format.Summary(reason)));
            }
        }
        lines.Add("");
        lines.Add($"Pitches: very good {review.VeryGood}, satisfactory {review.Satisfactory}, unsatisfactory {review.Unsatisfactory}, unfit {review.Unfit}.");
        lines.Add($"{Format.Demerits(review.Demerits)} in the last five years{(review.Banned ? ": the ground has lost the right to host." : ".")}");
        lines.Add($"The county won {review.Wins}, lost {review.Losses}, drew {review.Draws}, no result {review.NoResults}.");
        lines.Add("");
        lines.Add("The square at the end of the season:");
        foreach (var group in review.Square.GroupBy(s => s.Wear).OrderByDescending(g => g.Key))
        {
            var strips = string.Join(", ", group.Select(s => $"{s.Strip.Number} ({s.Matches} {(s.Matches == 1 ? "match" : "matches")})"));
            lines.Add($"  {Format.Wear(group.Key)}: {strips}");
        }

        _console.Write(new Panel(new Markup(string.Join("\n", lines))).Header($"Season review: {Markup.Escape(_game.View.GroundName)}").Border(BoxBorder.Double));
    }

    private void Answer(AnswerInput answer)
    {
        var requests = _game.View.Requests;
        if (answer.Request < 1 || answer.Request > requests.Count)
        {
            _console.MarkupLine($"[red]There's no request {answer.Request}.[/]");
            return;
        }
        var request = requests[answer.Request - 1];
        var who = Format.Who(request.Stakeholder);
        Order(
            new AnswerRequest(request.Id, answer.Accept),
            Markup.Escape(answer.Accept
                ? $"{who} will have {Format.Request(request.Kind)} for {Format.Match(request.Fixture)}. Now you have to deliver it."
                : $"You've told {who.ToLowerInvariant()} no to {Format.Request(request.Kind)} for {Format.Match(request.Fixture)}."));
        if (!answer.Accept && _game.View.Notices.LastOrDefault() is Groundsman.Core.Pressures.SatisfactionNotice declined)
        {
            _console.MarkupLine($"[yellow]{Markup.Escape(Format.Change(declined.Change))}[/]");
        }
    }

    private void Assign(AssignInput assign)
    {
        var fixtures = _game.View.Fixtures;
        if (assign.Fixture < 1 || assign.Fixture > fixtures.Count)
        {
            _console.MarkupLine($"[red]There's no fixture {assign.Fixture}. Type x for the list.[/]");
            return;
        }
        var fixture = fixtures[assign.Fixture - 1];
        if (!Report(_game.Submit(new AssignStrip(fixture.Id, assign.Strip))))
        {
            return;
        }
        var rest = Format.Rest(_game.View.Fixtures, assign.Fixture - 1) is { } r
            ? $", {r.Days} days after {Format.Match(r.Previous)} there"
            : ", its first match this season";
        _console.MarkupLine(Markup.Escape($"{Format.Match(fixture.Fixture)} on {Format.Day(fixture.Fixture.Start)} will be played on strip {assign.Strip.Number}{rest}."));
    }

    private void ShowFixtures()
    {
        var view = _game.View;
        var table = new Table().Border(TableBorder.Simple).Title("Fixtures")
            .AddColumn("#")
            .AddColumn(new TableColumn("Date").NoWrap())
            .AddColumn(new TableColumn("Match").NoWrap())
            .AddColumn("TV")
            .AddColumn("Strip")
            .AddColumn("Rest")
            .AddColumn("Requests")
            .AddColumn(new TableColumn("Status").NoWrap());
        foreach (var column in table.Columns)
        {
            column.Padding = new Padding(0, 0, 1, 0);
        }

        for (var i = 0; i < view.Fixtures.Count; i++)
        {
            var fixture = view.Fixtures[i];
            var played = view.Matches.FirstOrDefault(m => m.Fixture == fixture.Fixture && m.Finished);
            var grey = played != null ? "grey" : "default";
            var status = played?.Rating is { } rating ? $"[{GradeColour(rating.Grade)}]{Format.Grade(rating.Grade).ToLowerInvariant()}[/]"
                : played != null ? "[grey]not rated[/]"
                : fixture.Locked ? "locked"
                : "locks " + fixture.LocksOn.ToString("d MMM", System.Globalization.CultureInfo.InvariantCulture);
            table.AddRow(
                $"[{grey}]{i + 1}[/]",
                $"[{grey}]{fixture.Fixture.Start.ToString("d MMM", System.Globalization.CultureInfo.InvariantCulture)}[/]",
                $"[{grey}]{Markup.Escape(Format.Match(fixture.Fixture))}[/]",
                fixture.Fixture.Televised ? "TV" : "",
                fixture.Strip is { } strip ? (_game.View.Strips[strip.Number - 1].Centre ? $"{strip.Number}c" : strip.Number.ToString()) : "[yellow]none[/]",
                Format.Rest(view.Fixtures, i) is { } rest ? $"{rest.Days}d" : "",
                Markup.Escape(Format.Requests(view.Requests.Where(r => r.Fixture == fixture.Fixture))),
                status);
        }
        _console.Write(table);
        _console.MarkupLine("[grey]p <#> <strip> puts a fixture on a strip until it locks, 10 days out. Rest: days since the strip's last match. c: centre.[/]");
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
                match.Strip.Number.ToString(),
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
            .AddColumn("What it does");
        void Section(string title) => table.AddRow($"[bold]{title}[/]", "");

        Section("Readings");
        table.AddRow("r <strip>", "Moisture probe reading of the surface; r all reads every strip")
            .AddRow("f <strip>", "Feel a strip: quick, dry, damp or wet (can be wrong), and a look at the ends")
            .AddRow("d <strip>", "Soil core: slow, reads moisture below the surface");
        Section("Strip work");
        table.AddRow("w <strip>", "Water a strip (done when time advances)")
            .AddRow("m <strip> <mm>", "Mow to a height; more than a third off at once scalps it")
            .AddRow("l <strip> <roller> <min>", $"Roll ({string.Join(", ", _game.View.Rollers.Select(r => r.Id))}): only moist, never wet")
            .AddRow("e <strip>", "Repair the ends after a match: fill and seed them")
            .AddRow("c <strip> / u <strip>", "Put a cover on, or take it off")
            .AddRow("", "Every strip job takes a name, e.g. w 3 sam. You do it if none is given.");
        Section("Match days");
        table.AddRow("clean <strip>", "Clean and dry the footholes before play or at a break")
            .AddRow("fill <strip>", "Fill the footholes at close of play, in matches over one day");
        Section("Planning");
        table.AddRow("x", "The fixture list: strips, rest, requests, locks and ratings")
            .AddRow("p <#> <strip>", "Put fixture # on a strip; change it until its build-up starts")
            .AddRow("yes <#> / no <#>", "Accept or turn down request #. A broken promise costs more than no")
            .AddRow("v", "The season record: every match, its result and rating");
        Section("Game");
        table.AddRow("s", "Show the ground again")
            .AddRow("Enter or a", "Advance to the next decision point")
            .AddRow("new", "Once the season is over, start another")
            .AddRow("save", "Save a copy now; the season also saves itself after every turn")
            .AddRow("q", "Quit");

        _console.Write(table);
    }
}
