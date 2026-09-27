using Groundsman.Core.Content;

namespace Groundsman.Cli;

public static class ContentLoader
{
    public static string DefaultDirectory => Path.Combine(AppContext.BaseDirectory, "content");

    public static (GameContent Content, SeasonSettings Season) Load(string directory)
    {
        string Read(string file) => File.ReadAllText(Path.Combine(directory, file));

        var content = new GameContent(
            ContentParser.ParseCalendar(Read("calendar.json")),
            ContentParser.ParseClimate(Read("climate.json")),
            ContentParser.ParseGround(Read("ground.json")),
            ContentParser.ParseTasks(Read("tasks.json")),
            ContentParser.ParseReadings(Read("readings.json")));

        return (content, ContentParser.ParseSeason(Read("season.json")));
    }
}
