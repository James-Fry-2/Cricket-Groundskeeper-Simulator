using System.Security.Cryptography;
using Groundsman.Cli;
using Groundsman.Core;
using Groundsman.Core.Time;
using Spectre.Console;

var (content, season) = ContentLoader.Load(ContentLoader.DefaultDirectory);
var contentHash = ContentLoader.Hash(ContentLoader.DefaultDirectory);
var start = GameTime.OnDate(season.Start, content.Calendar.MorningHour);
var folder = new PlaytestFolder(Option("--data") ?? PlaytestFolder.DefaultRoot);
var input = Console.IsInputRedirected ? Console.In : null;
var debug = args.Contains("--debug");

RecordingGame NewSeason(ulong seed)
{
    var path = Path.Combine(folder.NewSeason(DateTime.Now, seed), PlaytestFolder.SaveName);
    AnsiConsole.MarkupLine($"[green]A new season[/], seed {seed}. It saves itself to [grey]{Markup.Escape(path)}[/].");
    return RecordingGame.Start(content, season.Fixtures, start, seed, contentHash, path);
}

AnsiConsole.MarkupLine("[green]Cricket Groundsman Simulator[/]");
if (debug)
{
    AnsiConsole.MarkupLine("[magenta]Debug mode: true values shown in magenta.[/]");
}

RecordingGame? game = null;
var seedOption = ParseSeed(Option("--seed"));
if (seedOption == null && folder.LatestUnfinished() is { } saved)
{
    var data = SaveFile.Read(saved);
    AnsiConsole.MarkupLine($"You have a season in progress (turn {data.Turns}). Resume it? [bold]yes[/] or [bold]no[/]");
    var answer = input != null ? input.ReadLine() : AnsiConsole.Prompt(new TextPrompt<string>("[grey]>[/]").AllowEmpty());
    if (answer?.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase) == true)
    {
        var (restored, error) = SaveFile.Restore(data, content, season.Fixtures, contentHash, saved);
        if (restored == null)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(error!)}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]Resumed.[/] It's {Format.Time(restored.View.Now)}.");
            game = restored;
        }
    }
}
game ??= NewSeason(seedOption ?? RandomSeed());

new GameLoop(
    AnsiConsole.Console,
    game,
    input,
    debug ? game.Inner.Inspect : null,
    () => NewSeason(RandomSeed())).Run();

static ulong RandomSeed() => BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));

static ulong? ParseSeed(string? text) => ulong.TryParse(text, out var seed) ? seed : null;

string? Option(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
