using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Content;
using Groundsman.Core.Time;

namespace Groundsman.Cli;

/// <summary>
/// The game, recording every accepted command with its turn so the season can be saved and
/// replayed. With a path, it saves itself after every change.
/// </summary>
public sealed class RecordingGame : IGame
{
    private readonly string? _autosavePath;
    private DateTime _turnStarted;
    private int _commandsThisTurn;

    public RecordingGame(Game inner, SaveData data, string? autosavePath = null, Telemetry? telemetry = null)
    {
        Inner = inner;
        Data = data;
        _autosavePath = autosavePath;
        Telemetry = telemetry;
        _turnStarted = telemetry?.Now ?? default;
    }

    public static RecordingGame Start(GameContent content, IReadOnlyList<Fixture> fixtures, GameTime start, ulong seed, string contentHash, string? autosavePath = null, string? gameVersion = null, Telemetry? telemetry = null)
    {
        var data = new SaveData
        {
            ContentHash = contentHash,
            GameVersion = gameVersion,
            Seed = seed,
            StartDate = start.Date,
            StartHour = start.Hour,
        };
        var game = new RecordingGame(new Game(new GameSetup(content, start, fixtures, seed)), data, autosavePath, telemetry);
        game.Autosave();
        return game;
    }

    public Game Inner { get; }
    public SaveData Data { get; private set; }
    public string? AutosavePath => _autosavePath;

    /// <summary>Set while the player fast-forwards, so the log can tell skipped turns from played ones.</summary>
    public bool FastForwarding { get; set; }

    /// <summary>The playtest log, if this season keeps one.</summary>
    public Telemetry? Telemetry { get; }

    public void Log(string name, IReadOnlyDictionary<string, object?>? fields = null) =>
        Telemetry?.Log(name, Data.Turns, Inner.View.Now, fields);

    public GameView View => Inner.View;

    public CommandResult Submit(IGameCommand command)
    {
        var details = Details(command);
        var result = Inner.Submit(command);
        if (result.Accepted)
        {
            Data.Commands.Add(CommandCodec.Encode(command, Data.Turns));
            _commandsThisTurn++;
            Autosave();
        }
        if (Telemetry != null)
        {
            details["accepted"] = result.Accepted;
            if (!result.Accepted)
            {
                details["reason"] = result.Reason;
            }
            Log("command", details);
        }
        return result;
    }

    public AdvanceResult Advance()
    {
        var wasOver = Inner.View.Review != null;
        var result = Inner.Advance();
        if (Telemetry != null)
        {
            var now = Telemetry.Now;
            Log("advance", new Dictionary<string, object?>
            {
                ["seconds"] = Math.Round((now - _turnStarted).TotalSeconds, 1),
                ["commands"] = _commandsThisTurn,
                ["hours"] = result.HoursRun,
                ["ff"] = FastForwarding,
                ["from"] = result.From.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) + $" {result.From.Hour:00}:00",
            });
            _turnStarted = now;
        }
        _commandsThisTurn = 0;
        Data = Data with { Turns = Data.Turns + 1, Finished = Inner.View.Review != null };
        if (!wasOver && Inner.View.Review is { } review)
        {
            LogReview(review);
        }
        Autosave();
        return result;
    }

    /// <summary>What a command was, for the log: the saved form, plus what only makes sense at the time.</summary>
    private Dictionary<string, object?> Details(IGameCommand command)
    {
        var saved = CommandCodec.Encode(command, Data.Turns);
        var details = new Dictionary<string, object?> { ["type"] = saved.Type };
        if (saved.Strip != null) details["strip"] = saved.Strip;
        if (saved.By != null) details["by"] = saved.By;
        if (saved.Tool != null) details["tool"] = saved.Tool;
        if (saved.HeightMm != null) details["heightMm"] = saved.HeightMm;
        if (saved.Roller != null) details["roller"] = saved.Roller;
        if (saved.Minutes != null) details["minutes"] = saved.Minutes;

        var view = Inner.View;
        if (command is AssignStrip assign)
        {
            details["fixture"] = assign.FixtureId;
            var upcoming = view.Fixtures.Where(f => f.Fixture.End >= view.Now.Date).ToList();
            details["fixturesAhead"] = upcoming.FindIndex(f => f.Id == assign.FixtureId);
        }
        if (command is AnswerRequest answer)
        {
            var request = view.Requests.FirstOrDefault(r => r.Id == answer.RequestId);
            details["request"] = answer.RequestId;
            details["accept"] = answer.Accept;
            details["stakeholder"] = request?.Stakeholder.ToString();
            details["kind"] = request?.Kind.ToString();
        }
        return details;
    }

    private void LogReview(Groundsman.Core.Pressures.SeasonReview review) => Log("review", new Dictionary<string, object?>
    {
        ["satisfaction"] = review.Stakeholders.ToDictionary(s => s.Stakeholder.ToString(), s => Math.Round(s.Satisfaction, 1)),
        ["veryGood"] = review.VeryGood,
        ["satisfactory"] = review.Satisfactory,
        ["unsatisfactory"] = review.Unsatisfactory,
        ["unfit"] = review.Unfit,
        ["demerits"] = review.Demerits,
        ["wins"] = review.Wins,
        ["losses"] = review.Losses,
    });

    private void Autosave()
    {
        if (_autosavePath != null)
        {
            SaveFile.Write(_autosavePath, Data);
        }
    }
}
