namespace Groundsman.Core.Content
{
    public sealed class GameContent
    {
        public GameContent(CalendarSettings calendar, ClimateSettings climate, GroundSettings ground, TaskSettings tasks, ReadingSettings readings)
        {
            Calendar = calendar;
            Climate = climate;
            Ground = ground;
            Tasks = tasks;
            Readings = readings;
        }

        public CalendarSettings Calendar { get; }
        public ClimateSettings Climate { get; }
        public GroundSettings Ground { get; }
        public TaskSettings Tasks { get; }
        public ReadingSettings Readings { get; }
    }
}
