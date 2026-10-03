using Groundsman.Core;
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

    public RecordingGame(Game inner, SaveData data, string? autosavePath = null)
    {
        Inner = inner;
        Data = data;
        _autosavePath = autosavePath;
    }

    public static RecordingGame Start(GameContent content, IReadOnlyList<Fixture> fixtures, GameTime start, ulong seed, string contentHash, string? autosavePath = null, string? gameVersion = null)
    {
        var data = new SaveData
        {
            ContentHash = contentHash,
            GameVersion = gameVersion,
            Seed = seed,
            StartDate = start.Date,
            StartHour = start.Hour,
        };
        var game = new RecordingGame(new Game(new GameSetup(content, start, fixtures, seed)), data, autosavePath);
        game.Autosave();
        return game;
    }

    public Game Inner { get; }
    public SaveData Data { get; private set; }
    public string? AutosavePath => _autosavePath;

    public GameView View => Inner.View;

    public CommandResult Submit(IGameCommand command)
    {
        var result = Inner.Submit(command);
        if (result.Accepted)
        {
            Data.Commands.Add(CommandCodec.Encode(command, Data.Turns));
            Autosave();
        }
        return result;
    }

    public AdvanceResult Advance()
    {
        var result = Inner.Advance();
        Data = Data with { Turns = Data.Turns + 1, Finished = Inner.View.Review != null };
        Autosave();
        return result;
    }

    private void Autosave()
    {
        if (_autosavePath != null)
        {
            SaveFile.Write(_autosavePath, Data);
        }
    }
}
