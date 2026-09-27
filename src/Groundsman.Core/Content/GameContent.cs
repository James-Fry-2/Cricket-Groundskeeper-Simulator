namespace Groundsman.Core.Content
{
    public sealed class GameContent
    {
        public GameContent(CalendarSettings calendar, GroundSettings ground, TaskSettings tasks, ReadingSettings readings)
        {
            Calendar = calendar;
            Ground = ground;
            Tasks = tasks;
            Readings = readings;
        }

        public CalendarSettings Calendar { get; }
        public GroundSettings Ground { get; }
        public TaskSettings Tasks { get; }
        public ReadingSettings Readings { get; }
    }
}
