using Groundsman.Core.Content;

namespace Groundsman.Cli;

public static class ContentLoader
{
    public static string DefaultDirectory => Path.Combine(AppContext.BaseDirectory, "content");

    public static (GameContent Content, SeasonSettings Season) Load(string directory)
    {
        string Read(string file) => File.ReadAllText(Path.Combine(directory, file));

        var formats = ContentParser.ParseFormats(Read("formats.json"));
        var teams = ContentParser.ParseTeams(Read("teams.json"));
        var content = new GameContent(
            ContentParser.ParseCalendar(Read("calendar.json")),
            ContentParser.ParseClimate(Read("climate.json")),
            ContentParser.ParseGround(Read("ground.json")),
            ContentParser.ParseLoams(Read("loams.json")),
            ContentParser.ParseMoisture(Read("moisture.json")),
            ContentParser.ParseCovers(Read("covers.json")),
            ContentParser.ParseTasks(Read("tasks.json")),
            ContentParser.ParseStaff(Read("staff.json")),
            ContentParser.ParseReadings(Read("readings.json")),
            ContentParser.ParseForecast(Read("forecast.json")),
            formats,
            teams.Teams,
            teams.HomeId,
            ContentParser.ParseGrass(Read("grass.json")),
            ContentParser.ParseRollers(Read("rollers.json")),
            ContentParser.ParseCompaction(Read("compaction.json")),
            ContentParser.ParsePitch(Read("pitch.json")),
            ContentParser.ParseWear(Read("wear.json")),
            ContentParser.ParseMatch(Read("match.json")),
            ContentParser.ParseCommentary(Read("commentary.json")),
            ContentParser.ParseRating(Read("rating.json")),
            ContentParser.ParseStakeholders(Read("stakeholders.json")));

        return (content, ContentParser.ParseSeason(Read("season.json"), formats, teams));
    }

    /// <summary>
    /// A fingerprint of every content file, so a save is only replayed on the content it was
    /// played with.
    /// </summary>
    public static string Hash(string directory)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        foreach (var file in Directory.GetFiles(directory, "*.json").OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal))
        {
            var name = System.Text.Encoding.UTF8.GetBytes(Path.GetFileName(file));
            sha.TransformBlock(name, 0, name.Length, null, 0);
            var bytes = File.ReadAllBytes(file);
            sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }
        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha.Hash!);
    }

    /// <summary>The phase 2 stand-in score, used by the harness only.</summary>
    public static ScoringSettings LoadScoring(string directory) =>
        ContentParser.ParseScoring(File.ReadAllText(Path.Combine(directory, "scoring.json")));
}
