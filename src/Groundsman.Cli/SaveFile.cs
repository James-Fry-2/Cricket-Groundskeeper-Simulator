using System.Text.Json;
using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Time;

namespace Groundsman.Cli;

/// <summary>
/// A season as a save holds it: the seed it started from and every command the player gave,
/// each with its turn. The game is deterministic, so replaying them rebuilds it exactly.
/// </summary>
public sealed record SaveData
{
    public int SchemaVersion { get; init; } = SaveFile.SchemaVersion;

    /// <summary>The content the season was played with; a save only replays on the same content.</summary>
    public string ContentHash { get; init; } = "";

    public string? GameVersion { get; init; }
    public ulong Seed { get; init; }
    public DateTime StartDate { get; init; }
    public int StartHour { get; init; }

    /// <summary>Turns played: how many times time has advanced.</summary>
    public int Turns { get; init; }

    public List<SavedCommand> Commands { get; init; } = new List<SavedCommand>();

    /// <summary>The season review has been reached.</summary>
    public bool Finished { get; init; }
}

public static class SaveFile
{
    public const int SchemaVersion = 1;

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { WriteIndented = true };

    /// <summary>Writes beside the target then moves it into place, so a crash never leaves half a save.</summary>
    public static void Write(string path, SaveData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, Options));
        File.Move(temp, path, overwrite: true);
    }

    public static SaveData Read(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<SaveData>(File.ReadAllText(path), Options)
                ?? throw new InvalidDataException($"{path} is empty.");
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"{path} isn't a save: {e.Message}", e);
        }
    }

    /// <summary>Rebuilds the season by replaying its commands; the error says why when it can't.</summary>
    public static (RecordingGame? Game, string? Error) Restore(SaveData data, GameContent content, IReadOnlyList<Fixture> fixtures, string contentHash, string? autosavePath = null)
    {
        if (data.SchemaVersion != SchemaVersion)
        {
            return (null, $"This save is version {data.SchemaVersion}; this build reads version {SchemaVersion}.");
        }
        if (data.ContentHash != contentHash)
        {
            return (null, "This save was made with different game content, so it can't be replayed. Start a new season.");
        }

        var game = new Game(new GameSetup(content, new GameTime(data.StartDate.Year, data.StartDate.Month, data.StartDate.Day, data.StartHour), fixtures, data.Seed));
        var byTurn = data.Commands.ToLookup(c => c.Turn);
        for (var turn = 0; turn <= data.Turns; turn++)
        {
            foreach (var saved in byTurn[turn])
            {
                var result = game.Submit(CommandCodec.Decode(saved));
                if (!result.Accepted)
                {
                    return (null, $"The save didn't replay: on turn {turn} the {saved.Type} command was refused ({result.Reason}).");
                }
            }
            if (turn < data.Turns)
            {
                game.Advance();
            }
        }
        return (new RecordingGame(game, data, autosavePath), null);
    }
}
