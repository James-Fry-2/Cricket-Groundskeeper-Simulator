using Groundsman.Core.Content;
using Groundsman.Core.Staff;

namespace Groundsman.Tests;

internal static class TestStaff
{
    public static readonly StaffId You = new StaffId("you");
    public static readonly StaffId Sam = new StaffId("sam");
    public static readonly StaffId Jo = new StaffId("jo");

    public static StaffSettings Settings { get; } = new StaffSettings(
        new[]
        {
            new StaffMemberSettings(You, "You", hoursPerDay: 8, readingSkill: 1.0),
            new StaffMemberSettings(Sam, "Sam Test", hoursPerDay: 8, readingSkill: 0.8),
            new StaffMemberSettings(Jo, "Jo Test", hoursPerDay: 4, readingSkill: 1.4),
        },
        waterHours: 1,
        coverHours: 0.25,
        uncoverHours: 0.25,
        probeReadingHours: 0.25,
        feelReadingHours: 0.05);
}
