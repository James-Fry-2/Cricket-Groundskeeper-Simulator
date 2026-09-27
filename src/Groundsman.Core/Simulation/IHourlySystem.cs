using Groundsman.Core.Time;

namespace Groundsman.Core.Simulation
{
    public interface IHourlySystem
    {
        TickStep Step { get; }
        void RunHour(GameTime hour);
    }
}
