using System.Globalization;

namespace Groundsman.Cli;

/// <summary>
/// Where seasons live: one folder each, holding the save (and in later tasks the telemetry and
/// notes), so a tester can zip a season's folder and send it back.
/// </summary>
public sealed class PlaytestFolder
{
    public const string SaveName = "save.json";
    public const string ReadmeName = "README.txt";

    private const string Readme = """
        Cricket Groundsman playtest

        Each season you play gets a folder here, named season-<date>-<seed>. It holds:
        - save.json: the season so far, so you can stop and carry on. It saves itself after every turn.
        - telemetry.jsonl: a log of what you did and when: the commands you gave, the readings you
          took, how you answered requests, the screens you looked at, and how long each turn took.
          It holds nothing about you or your computer.
        - notes.txt, if you want to write anything down as you play.

        Nothing is sent anywhere. When you've finished a season, zip its folder and send it back with
        the questionnaire.
        """;

    public PlaytestFolder(string root)
    {
        Root = root;
    }

    /// <summary>Documents/Cricket Groundsman, where testers can find it.</summary>
    public static string DefaultRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Cricket Groundsman");

    public string Root { get; }

    /// <summary>A new folder for a season started at <paramref name="now"/> (wall clock).</summary>
    public string NewSeason(DateTime now, ulong seed)
    {
        var folder = Path.Combine(Root, $"season-{now.ToString("yyyy-MM-dd-HHmm", CultureInfo.InvariantCulture)}-{seed}");
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>Writes the README explaining what's recorded; true the first time, when the player should be told.</summary>
    public bool EnsureReadme()
    {
        var path = Path.Combine(Root, ReadmeName);
        if (File.Exists(path))
        {
            return false;
        }
        Directory.CreateDirectory(Root);
        File.WriteAllText(path, Readme.Replace("\n", Environment.NewLine) + Environment.NewLine);
        return true;
    }

    /// <summary>The save of the most recently played season not yet finished, if any.</summary>
    public string? LatestUnfinished()
    {
        if (!Directory.Exists(Root))
        {
            return null;
        }
        return Directory.GetDirectories(Root, "season-*")
            .Select(d => Path.Combine(d, SaveName))
            .Where(File.Exists)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ThenByDescending(p => p, StringComparer.Ordinal)
            .FirstOrDefault(p =>
            {
                try
                {
                    return !SaveFile.Read(p).Finished;
                }
                catch (InvalidDataException)
                {
                    return false;
                }
            });
    }
}
