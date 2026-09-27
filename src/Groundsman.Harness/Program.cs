using Groundsman.Cli;
using Groundsman.Harness;

const string usage = """
    Usage:
      weather [--seed N] [--years N] [--out FILE]   Daily weather CSV and a monthly check against the normals
      trace [--seed N] [--end yyyy-MM-dd] [--out FILE]   Truth at every decision point of an unplayed season
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
