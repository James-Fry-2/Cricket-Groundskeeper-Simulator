using System.Collections.Generic;
using Groundsman.Core.Staff;

namespace Groundsman.Core.Content
{
    public sealed class StaffSettings
    {
        public StaffSettings(
            IReadOnlyList<StaffMemberSettings> members,
            double waterHours,
            double coverHours,
            double uncoverHours,
            double probeReadingHours,
            double feelReadingHours)
        {
            if (members.Count == 0)
            {
                throw new ContentException("staff needs at least one member: the player, listed first.");
            }
            var ids = new HashSet<StaffId>();
            foreach (var member in members)
            {
                if (!ids.Add(member.Id))
                {
                    throw new ContentException($"staff: two members have the id \"{member.Id}\".");
                }
            }
            CheckHours("water", waterHours);
            CheckHours("cover", coverHours);
            CheckHours("uncover", uncoverHours);
            CheckHours("probeReading", probeReadingHours);
            CheckHours("feelReading", feelReadingHours);

            Members = new List<StaffMemberSettings>(members).AsReadOnly();
            WaterHours = waterHours;
            CoverHours = coverHours;
            UncoverHours = uncoverHours;
            ProbeReadingHours = probeReadingHours;
            FeelReadingHours = feelReadingHours;
        }

        /// <summary>The first member is the player, who does any job not given to someone else.</summary>
        public IReadOnlyList<StaffMemberSettings> Members { get; }

        public double WaterHours { get; }
        public double CoverHours { get; }
        public double UncoverHours { get; }
        public double ProbeReadingHours { get; }
        public double FeelReadingHours { get; }

        private static void CheckHours(string job, double hours)
        {
            if (hours < 0 || hours > 24)
            {
                throw new ContentException($"staff.jobHours.{job} ({hours}) must be from 0 to 24.");
            }
        }
    }
}
