using System.Globalization;
using System.Text.Json;
using Groundsman.Core.Time;

namespace Groundsman.Cli;

/// <summary>
/// The playtest log: one JSON object a line, beside the season's save. Each line names its
/// event and carries the turn, the game time and the wall-clock time, so the pass test can be
/// read from it. Wall-clock time belongs here in the Cli, never in the core.
/// </summary>
public sealed class Telemetry
{
    public const string FileName = "telemetry.jsonl";

    private readonly string _path;
    private readonly Func<DateTime> _clock;

    /// <param name="clock">UTC wall-clock time; tests supply their own.</param>
    public Telemetry(string path, Func<DateTime>? clock = null)
    {
        _path = path;
        _clock = clock ?? (() => DateTime.UtcNow);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
    }

    public DateTime Now => _clock();

    public void Log(string name, int turn, GameTime game, IReadOnlyDictionary<string, object?>? fields = null)
    {
        var line = new Dictionary<string, object?>
        {
            ["event"] = name,
            ["turn"] = turn,
            ["game"] = game.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + $" {game.Hour:00}:00",
            ["wall"] = _clock().ToString("o", CultureInfo.InvariantCulture),
        };
        if (fields != null)
        {
            foreach (var field in fields)
            {
                line[field.Key] = field.Value;
            }
        }
        File.AppendAllText(_path, JsonSerializer.Serialize(line) + "\n");
    }
}
