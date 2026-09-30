using Groundsman.Core.Strips;

namespace Groundsman.Harness.Policies;

/// <summary>Strip plans for the shipped season, by fixture id.</summary>
public static class SeasonPlans
{
    /// <summary>
    /// The strips the season file named before players chose them: a simple rotation over nine
    /// strips that no build-ups overlap in. Keeps phase 3's harness results comparable.
    /// </summary>
    public static IReadOnlyDictionary<string, StripId> Original { get; } = new Dictionary<string, StripId>
    {
        ["2027-04-16"] = new StripId(6),
        ["2027-04-25"] = new StripId(8),
        ["2027-05-02"] = new StripId(5),
        ["2027-05-13"] = new StripId(7),
        ["2027-05-20"] = new StripId(4),
        ["2027-06-01"] = new StripId(9),
        ["2027-06-06"] = new StripId(3),
        ["2027-06-13"] = new StripId(6),
        ["2027-06-26"] = new StripId(8),
        ["2027-07-04"] = new StripId(10),
        ["2027-07-11"] = new StripId(5),
        ["2027-07-24"] = new StripId(7),
        ["2027-08-02"] = new StripId(4),
        ["2027-08-14"] = new StripId(9),
        ["2027-08-22"] = new StripId(3),
        ["2027-09-01"] = new StripId(6),
        ["2027-09-14"] = new StripId(8),
        ["2027-09-22"] = new StripId(5),
    };
}
