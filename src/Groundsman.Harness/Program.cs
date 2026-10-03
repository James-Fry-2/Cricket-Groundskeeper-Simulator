using Groundsman.Cli;
using Groundsman.Harness;

const string usage = """
    Usage:
      weather [--seed N] [--years N] [--out FILE]   Daily weather CSV and a monthly check against the normals
      trace [--seed N] [--end yyyy-MM-dd] [--out FILE]   Truth at every decision point of an unplayed season
      pitch [--seasons N] [--out FILE]   How an untouched strip plays each morning, averaged by month
      gate [--seasons N] [--seed FIRST] [--lead 0.25] [--out FILE]   Gate A: by the book against neglect and random play
      gateb [--seasons N] [--seed FIRST] [--lead 10] [--tolerance 3] [--out FILE]   Gate B: greedy against planned strip rotation
      playtest FOLDER [--content DIR]   The pass test and Gate C from testers' returned folders (DIR: the build's content, if not this one's)
      reuse [--seasons N]   A strip reused after gaps of 11 to 42 days, and a third use, against a fresh strip
      check [--seasons N] [--seed FIRST] [--satisfactory 0.85] [--very-good 0.1] [--neglect 0.8] [--out FILE]   Check 3: the referee's ratings, planned play against neglect
    """;

if (args.Length == 0)
{
    Console.WriteLine(usage);
    return 1;
}

var (content, season) = ContentLoader.Load(ContentLoader.DefaultDirectory);
var seed = ulong.Parse(Option("--seed") ?? "1");

switch (args[0])
{
    case "weather":
    {
        var years = int.Parse(Option("--years") ?? "50");
        var report = WeatherReport.Run(content.Climate, seed, years);
        var path = Write(Option("--out") ?? "harness-output/weather.csv", report.DailyCsv);
        Console.WriteLine($"{years} years of weather from seed {seed}, averaged by month:");
        Console.WriteLine(report.SummaryTable());
        Console.WriteLine($"Daily weather written to {path}");
        return 0;
    }
    case "trace":
    {
        var end = DateTime.Parse(Option("--end") ?? $"{season.Start.Year}-09-30");
        var path = Write(Option("--out") ?? "harness-output/trace.csv", SeasonTrace.Run(content, season, seed, end));
        Console.WriteLine($"Season trace from seed {seed} written to {path}");
        return 0;
    }
    case "pitch":
    {
        var seasons = int.Parse(Option("--seasons") ?? "50");
        var report = PitchReport.Run(content, season, seasons, new DateTime(season.Start.Year, 9, 30));
        var path = Write(Option("--out") ?? "harness-output/pitch.csv", report.Csv);
        Console.WriteLine($"An untouched strip each morning over {seasons} seasons, averaged by month (0 to 10):");
        Console.WriteLine(report.SummaryTable());
        Console.WriteLine($"Daily values written to {path}");
        return 0;
    }
    case "gate":
    {
        var seasons = int.Parse(Option("--seasons") ?? "1000");
        var lead = double.Parse(Option("--lead") ?? "0.25", System.Globalization.CultureInfo.InvariantCulture);
        var scoring = ContentLoader.LoadScoring(ContentLoader.DefaultDirectory);
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var report = GateReport.Run(content, season, scoring, seasons, lead, seed);
        var path = Write(Option("--out") ?? "harness-output/gate.csv", report.Csv);
        Console.WriteLine($"{seasons} seasons (seeds {seed} to {seed + (ulong)seasons - 1}) of {season.Fixtures.Count} fixtures per policy, in {timer.Elapsed.TotalSeconds:0} s.");
        Console.WriteLine($"On target: subsurface {scoring.SubsurfaceMin}–{scoring.SubsurfaceMax}%, surface under {scoring.SurfaceMax}%, on each fixture's first morning.");
        Console.WriteLine();
        Console.WriteLine(report.SummaryTable());
        Console.WriteLine($"Per-match results written to {path}");
        return report.Passed ? 0 : 2;
    }
    case "gateb":
    {
        var seasons = int.Parse(Option("--seasons") ?? "1000");
        var lead = double.Parse(Option("--lead") ?? "10", System.Globalization.CultureInfo.InvariantCulture);
        var tolerance = double.Parse(Option("--tolerance") ?? "3", System.Globalization.CultureInfo.InvariantCulture);
        var scoring = ContentLoader.LoadScoring(ContentLoader.DefaultDirectory);
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var report = GateBReport.Run(content, season, scoring, seasons, lead, tolerance, seed);
        var path = Write(Option("--out") ?? "harness-output/gateb.csv", report.Csv);
        Console.WriteLine($"{seasons} seasons (seeds {seed} to {seed + (ulong)seasons - 1}) per policy, in {timer.Elapsed.TotalSeconds:0} s.");
        Console.WriteLine();
        Console.WriteLine(report.SummaryTable());
        Console.WriteLine($"Per-match results written to {path}");
        return report.Passed ? 0 : 2;
    }
    case "playtest":
    {
        if (args.Length < 2)
        {
            Console.WriteLine(usage);
            return 1;
        }
        var contentDir = Option("--content") ?? ContentLoader.DefaultDirectory;
        var (buildContent, buildSeason) = ContentLoader.Load(contentDir);
        var report = PlaytestReport.Run(args[1], buildContent, buildSeason.Fixtures, ContentLoader.Hash(contentDir));
        Console.WriteLine(report.Summary());
        return report.GateCPassed ? 0 : 2;
    }
    case "reuse":
    {
        var seasons = int.Parse(Option("--seasons") ?? "200");
        var report = ReuseReport.Run(content, season.Start, new DateTime(season.Start.Year, 6, 1), seasons);
        Console.WriteLine($"Four-day matches prepared by the book, {seasons} seasons each, first match 1 June; true pitch averaged over the last match:");
        Console.WriteLine(report.SummaryTable());
        return 0;
    }
    case "check":
    {
        var seasons = int.Parse(Option("--seasons") ?? "1000");
        var satisfactory = double.Parse(Option("--satisfactory") ?? "0.85", System.Globalization.CultureInfo.InvariantCulture);
        var veryGood = double.Parse(Option("--very-good") ?? "0.1", System.Globalization.CultureInfo.InvariantCulture);
        var neglect = double.Parse(Option("--neglect") ?? "0.8", System.Globalization.CultureInfo.InvariantCulture);
        var scoring = ContentLoader.LoadScoring(ContentLoader.DefaultDirectory);
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var report = CheckReport.From(PolicyRuns.Run(content, season, scoring, seasons, seed, CheckReport.Makers), content.Rating, satisfactory, veryGood, neglect);
        var path = Write(Option("--out") ?? "harness-output/check.csv", report.Csv);
        Console.WriteLine($"{seasons} seasons (seeds {seed} to {seed + (ulong)seasons - 1}) of {season.Fixtures.Count} fixtures per policy, in {timer.Elapsed.TotalSeconds:0} s.");
        Console.WriteLine();
        Console.WriteLine(report.SummaryTable());
        Console.WriteLine($"Per-match results written to {path}");
        return report.Passed ? 0 : 2;
    }
    default:
        Console.WriteLine(usage);
        return 1;
}

string? Option(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static string Write(string path, string text)
{
    var full = Path.GetFullPath(path);
    Directory.CreateDirectory(Path.GetDirectoryName(full)!);
    File.WriteAllText(full, text);
    return full;
}
