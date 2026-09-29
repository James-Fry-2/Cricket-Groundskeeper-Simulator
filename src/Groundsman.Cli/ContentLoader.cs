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
            ContentParser.ParseWear(Read("wear.json")));

        return (content, ContentParser.ParseSeason(Read("season.json"), formats, teams));
    }

    /// <summary>The phase 2 stand-in score, used by the harness only.</summary>
    public static ScoringSettings LoadScoring(string directory) =>
        ContentParser.ParseScoring(File.ReadAllText(Path.Combine(directory, "scoring.json")));
}
