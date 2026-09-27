using Groundsman.Core.Content;

namespace Groundsman.Tests;

internal static class TestCalendar
{
    /// <summary>Fixed test values, independent of the shipped content so tuning it can't break tests.</summary>
    public static CalendarSettings Settings { get; } = new CalendarSettings(
        seasonStart: new MonthDay(4, 1),
        seasonEnd: new MonthDay(9, 30),
        morningHour: 7,
        afternoonHour: 13,
        offSeasonStepDays: 7,
        finalPrepDays: 3,
        matchDayDecisionHours: new[] { 8, 13, 16, 18 });
}
