using System.Globalization;
using System.Text;
using System.Text.Json;
using Groundsman.Cli;
using Groundsman.Core;
using Groundsman.Core.Content;

namespace Groundsman.Harness;

/// <summary>
/// Reads the folders testers send back and reports the MVP pass test from their logs, then
/// Gate C with the questionnaire answers. Each save is replayed to check it. The thresholds
/// are placeholders until the first wave's data is in.
/// </summary>
public sealed class PlaytestReport
{
    /// <summary>The questionnaire answers Gate C needs, entered by hand: tester,explained_verdict,play_again.</summary>
    public const string AnswersName = "gatec.csv";

    /// <summary>Fixtures given their strip two or more fixtures out for a tester to count as planning ahead.</summary>
    public const int PlansAheadMin = 3;

    /// <summary>Key jobs a tester must have done for the readings row to be judged.</summary>
    public const int KeyJobsMin = 5;

    /// <summary>Share of key jobs with a reading of that strip first for readings to count as driving decisions.</summary>
    public const double ReadingShareMin = 0.5;

    /// <summary>Every this many plain advances in a row, with nothing done, counts as a long run.</summary>
    public const int LongRun = 5;

    /// <summary>Long runs a season may have and still have held its pace.</summary>
    public const int LongRunsMax = 3;

    private static readonly string[] KeyJobTypes = { "water", "roll", "cover" };

    private PlaytestReport(IReadOnlyList<SeasonAnalysis> seasons, IReadOnlyList<TesterResult> testers, bool hasAnswers)
    {
        Seasons = seasons;
        Testers = testers;
        HasAnswers = hasAnswers;
    }

    public IReadOnlyList<SeasonAnalysis> Seasons { get; }
    public IReadOnlyList<TesterResult> Testers { get; }
    public bool HasAnswers { get; }

    /// <summary>Most testers finished, explained a verdict and would play again; "most" is more than half.</summary>
    public bool GateCPassed =>
        HasAnswers
        && Testers.Count > 0
        && Most(t => t.Finished)
        && Most(t => t.ExplainedVerdict == true)
        && Most(t => t.PlayAgain == true);

    /// <param name="root">A folder of tester folders (T1, T2, ...), each holding season folders; a season folder on its own works too.</param>
    public static PlaytestReport Run(string root, GameContent content, IReadOnlyList<Fixture> fixtures, string contentHash)
    {
        var seasons = Directory.GetFiles(root, Telemetry.FileName, SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .OrderBy(d => d, StringComparer.Ordinal)
            .Select(d => Analyse(root, d!, content, fixtures, contentHash))
            .ToList();

        var answers = ReadAnswers(Path.Combine(root, AnswersName));
        var testers = seasons
            .GroupBy(s => s.Tester)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g =>
            {
                answers.TryGetValue(g.Key, out var answer);
                return new TesterResult(g.Key, g.Any(s => s.Finished), answer.Explained, answer.PlayAgain);
            })
            .ToList();
        return new PlaytestReport(seasons, testers, answers.Count > 0);
    }

    public string Summary()
    {
        var text = new StringBuilder();
        text.Append("Tester  Season                            Replay  Finished  Turns  Minutes  Ahead  Reads/jobs  Declined  Long runs  ff   Verdicts\n");
        foreach (var s in Seasons)
        {
            text.Append($"{s.Tester,-6}  {Short(s.Season),-32}  {(s.Replayed ? "ok" : "NO"),-6}  {Yes(s.Finished),-8}  {s.Turns,5}  {s.Minutes,7:0}  {s.FixturesPlannedAhead,5}  {s.KeyJobsAfterReading,4}/{s.KeyJobs,-5}  {s.RequestsDeclined,8}  {s.LongRuns,9}  {s.FastForwards,3}  {s.VerdictViews,8}\n");
            if (!s.Replayed)
            {
                text.Append($"        {s.ReplayNote}\n");
            }
        }

        text.Append("\nPass test, testers meeting each row:\n");
        Row("Plan strips two or more fixtures out", s => s.PlansAhead, $"{PlansAheadMin}+ fixtures");
        Row("Take readings before key jobs", s => s.ReadingsDrive, $"{ReadingShareMin:P0} of {KeyJobsMin}+ water, roll and cover jobs");
        Row("Turn down a request on purpose", s => s.TurnsDown, "1+");
        Row("Pace holds", s => s.PaceHolds, $"{LongRunsMax} or fewer runs of {LongRun}+ plain advances");
        text.Append("  Verdicts readable: from the questionnaire (question 8) and Gate C below.\n");

        text.Append($"\nGate C, {Testers.Count} testers (most means more than half):\n");
        text.Append($"  Finished the season: {Testers.Count(t => t.Finished)}\n");
        if (!HasAnswers)
        {
            text.Append($"  No questionnaire answers yet: add {AnswersName} to the folder (tester,explained_verdict,play_again, yes or no).\n");
            text.Append("Gate C NOT PASSED (waiting for answers).\n");
            return text.ToString();
        }
        text.Append($"  Explained a verdict: {Testers.Count(t => t.ExplainedVerdict == true)}\n");
        text.Append($"  Would play another season: {Testers.Count(t => t.PlayAgain == true)}\n");
        text.Append($"Gate C {(GateCPassed ? "PASSED" : "NOT PASSED")}.\n");
        return text.ToString();

        void Row(string name, Func<SeasonAnalysis, bool> test, string bar)
        {
            var meeting = Seasons.GroupBy(s => s.Tester).Count(g => g.Any(test));
            text.Append($"  {name}: {meeting} of {Testers.Count} ({bar})\n");
        }
    }

    private bool Most(Func<TesterResult, bool> test) => Testers.Count(test) * 2 > Testers.Count;

    private static SeasonAnalysis Analyse(string root, string folder, GameContent content, IReadOnlyList<Fixture> fixtures, string contentHash)
    {
        var relative = Path.GetRelativePath(root, folder).Split(Path.DirectorySeparatorChar);
        var tester = relative.Length > 1 ? relative[0] : "-";
        var events = File.ReadAllLines(Path.Combine(folder, Telemetry.FileName))
            .Where(l => l.Length > 0)
            .Select(l => JsonDocument.Parse(l).RootElement)
            .ToList();

        var (replayed, note, finished) = Replay(Path.Combine(folder, PlaytestFolder.SaveName), content, fixtures, contentHash);
        finished |= events.Any(e => Name(e) == "review");

        var commands = events.Where(e => Name(e) == "command" && e.GetProperty("accepted").GetBoolean()).ToList();
        var advances = events.Where(e => Name(e) == "advance").ToList();

        var plannedAhead = commands
            .Where(e => Type(e) == "assign" && e.TryGetProperty("fixturesAhead", out var ahead) && ahead.GetInt32() >= 2)
            .Select(e => e.GetProperty("fixture").GetString())
            .Distinct()
            .Count();

        // A key job counts as informed when the same strip was read earlier that turn or the turn before.
        var readings = commands.Where(e => Type(e) == "read").Select(e => (Turn: Turn(e), Strip: Strip(e), Order: events.IndexOf(e))).ToList();
        var keyJobs = commands.Where(e => KeyJobTypes.Contains(Type(e))).ToList();
        var informed = keyJobs.Count(job => readings.Any(r =>
            r.Strip == Strip(job) && r.Order < events.IndexOf(job) && Turn(job) - r.Turn <= 1));

        var (longest, longRuns) = PlainRuns(advances);
        var minutes = advances.Sum(a => Math.Min(a.GetProperty("seconds").GetDouble(), 600)) / 60;

        return new SeasonAnalysis(
            tester,
            Path.GetFileName(folder),
            replayed,
            note,
            finished,
            advances.Count,
            minutes,
            events.Count(e => Name(e) == "session_start"),
            plannedAhead,
            keyJobs.Count,
            informed,
            readings.Count,
            commands.Count(e => Type(e) == "answer" && e.GetProperty("accept").GetBoolean()),
            commands.Count(e => Type(e) == "answer" && !e.GetProperty("accept").GetBoolean()),
            longest,
            longRuns,
            events.Count(e => Name(e) == "fast_forward"),
            events.Count(e => Name(e) == "screen" && e.GetProperty("name").GetString() is "verdict" or "record" or "review"));
    }

    private static (bool Replayed, string Note, bool Finished) Replay(string save, GameContent content, IReadOnlyList<Fixture> fixtures, string contentHash)
    {
        if (!File.Exists(save))
        {
            return (false, "No save in the folder.", false);
        }
        try
        {
            var data = SaveFile.Read(save);
            var (game, error) = SaveFile.Restore(data, content, fixtures, contentHash);
            if (game == null)
            {
                return (false, error!, data.Finished);
            }
            return (true, $"Replayed {data.Turns} turns.", game.View.Review != null);
        }
        catch (InvalidDataException e)
        {
            return (false, e.Message, false);
        }
    }

    /// <summary>The longest run of plain advances (nothing done, not fast-forwarded), and how many long runs: a run of 12 is two.</summary>
    private static (int Longest, int LongRuns) PlainRuns(List<JsonElement> advances)
    {
        int longest = 0, runs = 0, current = 0;
        foreach (var advance in advances)
        {
            var plain = advance.GetProperty("commands").GetInt32() == 0
                && !(advance.TryGetProperty("ff", out var ff) && ff.GetBoolean());
            if (plain)
            {
                current++;
                longest = Math.Max(longest, current);
                if (current % LongRun == 0)
                {
                    runs++;
                }
            }
            else
            {
                current = 0;
            }
        }
        return (longest, runs);
    }

    private static Dictionary<string, (bool? Explained, bool? PlayAgain)> ReadAnswers(string path)
    {
        var answers = new Dictionary<string, (bool?, bool?)>(StringComparer.Ordinal);
        if (!File.Exists(path))
        {
            return answers;
        }
        foreach (var line in File.ReadAllLines(path).Skip(1).Where(l => l.Trim().Length > 0))
        {
            var cells = line.Split(',').Select(c => c.Trim().ToLowerInvariant()).ToArray();
            if (cells.Length >= 3)
            {
                answers[cells[0].ToUpperInvariant()] = (cells[1] == "yes", cells[2] == "yes");
            }
        }
        return answers;
    }

    private static string Name(JsonElement e) => e.GetProperty("event").GetString()!;
    private static string? Type(JsonElement e) => e.TryGetProperty("type", out var t) ? t.GetString() : null;
    private static int Turn(JsonElement e) => e.GetProperty("turn").GetInt32();
    private static int? Strip(JsonElement e) => e.TryGetProperty("strip", out var s) ? s.GetInt32() : null;
    private static string Yes(bool value) => value ? "yes" : "no";
    private static string Short(string season) => season.Length > 32 ? season.Substring(0, 32) : season;

    public sealed record SeasonAnalysis(
        string Tester,
        string Season,
        bool Replayed,
        string ReplayNote,
        bool Finished,
        int Turns,
        double Minutes,
        int Sessions,
        int FixturesPlannedAhead,
        int KeyJobs,
        int KeyJobsAfterReading,
        int Readings,
        int RequestsAccepted,
        int RequestsDeclined,
        int LongestPlainRun,
        int LongRuns,
        int FastForwards,
        int VerdictViews)
    {
        public bool PlansAhead => FixturesPlannedAhead >= PlansAheadMin;
        public bool ReadingsDrive => KeyJobs >= KeyJobsMin && KeyJobsAfterReading >= ReadingShareMin * KeyJobs;
        public bool TurnsDown => RequestsDeclined >= 1;
        public bool PaceHolds => LongRuns <= LongRunsMax;
    }

    public sealed record TesterResult(string Tester, bool Finished, bool? ExplainedVerdict, bool? PlayAgain);
}
