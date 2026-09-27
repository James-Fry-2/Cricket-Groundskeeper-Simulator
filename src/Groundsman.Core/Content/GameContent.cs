namespace Groundsman.Core.Content
{
    public sealed class GameContent
    {
        public GameContent(CalendarSettings calendar, GroundSettings ground, TaskSettings tasks)
        {
            Calendar = calendar;
            Ground = ground;
            Tasks = tasks;
        }

        public CalendarSettings Calendar { get; }
        public GroundSettings Ground { get; }
        public TaskSettings Tasks { get; }
    }
}
