using Groundsman.Core.Staff;

namespace Groundsman.Core
{
    public sealed class StaffView
    {
        public StaffView(StaffId id, string name, double hoursPerDay, double hoursLeft)
        {
            Id = id;
            Name = name;
            HoursPerDay = hoursPerDay;
            HoursLeft = hoursLeft;
        }

        public StaffId Id { get; }
        public string Name { get; }
        public double HoursPerDay { get; }

        /// <summary>Hours left today, after the jobs already ordered.</summary>
        public double HoursLeft { get; }
    }
}
