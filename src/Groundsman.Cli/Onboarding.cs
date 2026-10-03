using System.Text.Json;

namespace Groundsman.Cli;

/// <summary>The words that teach the game: the introduction and the first-time tips.</summary>
public static class Onboarding
{
    /// <summary>One line a paragraph, so the terminal wraps it to fit.</summary>
    public const string Intro =
        "You are head groundsman at Kestrel Lane, home of Kestrelshire County Cricket Club. Between April and September the club plays 18 home matches on your square of 12 strips: four-day matches, one-day matches and T20s. Your job is the pitches.\n" +
        "\n" +
        "Three people judge you:\n" +
        "- The match referee rates every pitch. Uneven, dangerous or dead pitches cost demerits.\n" +
        "- The captain wants pitches that suit the team, and will ask for them.\n" +
        "- The board wants four-day matches to last, televised matches on the centre strips, and no demerits.\n" +
        "\n" +
        "You never see a strip's true state. Readings give ranges, and they can be wrong: f feels a strip (quick), r probes its surface, d takes a soil core from below.\n" +
        "\n" +
        "To start: x shows the fixtures, p puts a fixture on a strip, Enter moves time on, and ff skips ahead to the next thing that needs you. h lists everything.";

    /// <summary>Tips by the moment that prompts them, each shown once.</summary>
    public static readonly IReadOnlyDictionary<string, string> Tips = new Dictionary<string, string>
    {
        ["start"] = "Tip: every fixture needs a strip, chosen before its build-up starts ten days out. x lists them, p <#> <strip> chooses. Using a strip wears it for weeks, so spread them out, and keep the centre strips (c) for televised matches.",
        ["request"] = "Tip: answer requests with yes <#> or no <#>. A promise you don't keep costs more than saying no. Green means grass left on and a damp surface; turning, a dry surface that wears; pace, a hard, well-rolled strip; flat, a true pitch with little movement.",
        ["lock"] = "Tip: a build-up has started, ten days to get the strip right. Water deeply early (w), roll when it feels damp (f then l), mow down towards 7 mm over the days (m), and cover against rain near the end (c).",
        ["rain"] = "Tip: c <strip> puts a cover on and u takes it off. A covered strip takes no rain but dries slowly, so take covers off when the rain has gone.",
        ["match"] = "Tip: matches play session by session, and the commentary says how the pitch is behaving and why. clean <strip> tidies the footholes at a break; in a four-day match, fill <strip> repairs them at close of play.",
        ["verdict"] = "Tip: the referee rates every pitch, and an unsatisfactory one costs a demerit; five in five years and the ground loses the right to host. Repair the ends of a used strip (e <strip>) and give it weeks to grow back before using it again.",
    };
}

/// <summary>Which tips the player has seen, kept across seasons when there's a file to keep them in.</summary>
public sealed class TipBook
{
    private readonly string? _path;
    private readonly HashSet<string> _seen;
    private bool _off;

    public TipBook()
        : this(null, new HashSet<string>(), off: false)
    {
    }

    private TipBook(string? path, HashSet<string> seen, bool off)
    {
        _path = path;
        _seen = seen;
        _off = off;
    }

    public static TipBook Load(string path)
    {
        if (!File.Exists(path))
        {
            return new TipBook(path, new HashSet<string>(), off: false);
        }
        try
        {
            var saved = JsonSerializer.Deserialize<Saved>(File.ReadAllText(path)) ?? new Saved();
            return new TipBook(path, new HashSet<string>(saved.Seen), saved.Off);
        }
        catch (JsonException)
        {
            return new TipBook(path, new HashSet<string>(), off: false);
        }
    }

    /// <summary>True if the tip should show now; it then counts as seen.</summary>
    public bool Show(string id)
    {
        if (_off || !_seen.Add(id))
        {
            return false;
        }
        Save();
        return true;
    }

    public void TurnOff()
    {
        _off = true;
        Save();
    }

    private void Save()
    {
        if (_path != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path))!);
            File.WriteAllText(_path, JsonSerializer.Serialize(new Saved { Seen = _seen.OrderBy(s => s, StringComparer.Ordinal).ToList(), Off = _off }));
        }
    }

    private sealed class Saved
    {
        public List<string> Seen { get; set; } = new List<string>();
        public bool Off { get; set; }
    }
}
