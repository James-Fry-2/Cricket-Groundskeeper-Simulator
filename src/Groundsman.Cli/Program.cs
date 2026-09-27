using System.Security.Cryptography;
using Groundsman.Cli;
using Groundsman.Core;
using Groundsman.Core.Time;
using Spectre.Console;

var seed = ParseSeed(args) ?? BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));
var (content, season) = ContentLoader.Load(ContentLoader.DefaultDirectory);
var start = GameTime.OnDate(season.Start, content.Calendar.MorningHour);
var game = new Game(new GameSetup(content, start, season.MatchDays, seed));

AnsiConsole.MarkupLine($"[green]Cricket Groundsman Simulator[/], seed {seed}");
var debug = args.Contains("--debug");
if (debug)
{
    AnsiConsole.MarkupLine("[magenta]Debug mode: true values shown in magenta.[/]");
}
new GameLoop(AnsiConsole.Console, game, Console.IsInputRedirected ? Console.In : null, debug ? game.Inspect : null).Run();

static ulong? ParseSeed(string[] args)
{
    var index = Array.IndexOf(args, "--seed");
    return index >= 0 && index + 1 < args.Length && ulong.TryParse(args[index + 1], out var seed) ? seed : null;
}
