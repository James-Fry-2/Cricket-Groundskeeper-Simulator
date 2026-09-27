using Groundsman.Core.Staff;

namespace Groundsman.Core.Content
{
    public sealed class StaffMemberSettings
    {
        public StaffMemberSettings(StaffId id, string name, double hoursPerDay, double readingSkill)
        {
            if (string.IsNullOrWhiteSpace(id.Value))
            {
                throw new ContentException("staff: every member of staff needs an id.");
            }
            if (hoursPerDay < 0 || hoursPerDay > 24)
            {
                throw new ContentException($"staff[{id}].hoursPerDay ({hoursPerDay}) must be from 0 to 24.");
            }
            if (readingSkill <= 0)
            {
                throw new ContentException($"staff[{id}].readingSkill ({readingSkill}) must be above 0.");
            }

            Id = id;
            Name = name;
            HoursPerDay = hoursPerDay;
            ReadingSkill = readingSkill;
        }

        public StaffId Id { get; }
        public string Name { get; }
        public double HoursPerDay { get; }

        /// <summary>
        /// Multiplies the width of this person's readings: below 1 reads tighter than the tool's
        /// standard, above 1 looser.
        /// </summary>
        public double ReadingSkill { get; }
    }
}
