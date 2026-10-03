using System.Globalization;

namespace Groundsman.Cli;

/// <summary>
/// Where seasons live: one folder each, holding the save (and in later tasks the telemetry and
/// notes), so a tester can zip a season's folder and send it back.
/// </summary>
public sealed class PlaytestFolder
{
    public const string SaveName = "save.json";

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
