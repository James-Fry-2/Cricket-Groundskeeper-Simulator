using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Pressures;
using Groundsman.Core.Strips;

namespace Groundsman.Harness.Policies;

/// <summary>
/// Prepares by the book with a rotation planned for the whole season at the start: centre
/// strips kept for televised matches, outer strips for the rest, each strip's uses spaced out,
/// and strips next to a recent or coming match avoided. It says yes only to requests a true,
/// well-prepared pitch can deliver.
/// </summary>
public sealed class PlannedPolicy : ByTheBookPolicy
{
    public const int RestWantedDays = 35;
    public const int NeighbourWindowDays = 14;

    /// <summary>What a plain by-the-book build-up tends to give; the rest put the referee at risk.</summary>
    public static readonly RequestKind[] Accepts = { RequestKind.Flat, RequestKind.LastsFourDays };

    private IReadOnlyDictionary<string, StripId>? _rotation;

    public override string Name => "planned";

    protected override void ChooseStrips(IGame game)
    {
        _rotation ??= Plan(game.View);
        Follow(game, _rotation);
    }

    protected override void AnswerRequests(IGame game) => Answer(game);

    /// <summary>Yes to what a true, well-prepared pitch tends to give; no to the rest.</summary>
    public static void Answer(IGame game)
    {
        foreach (var request in game.View.Requests.Where(r => r.Status == RequestStatus.Open))
        {
            game.Submit(new AnswerRequest(request.Id, Accepts.Contains(request.Kind)));
        }
    }

    /// <summary>Strips for every fixture still open, in date order, each the least costly that's free.</summary>
    public static IReadOnlyDictionary<string, StripId> Plan(GameView view)
    {
        var lockDays = view.Fixtures.Count == 0 ? 0 : (view.Fixtures[0].Fixture.Start - view.Fixtures[0].LocksOn).Days;
        var booked = view.Fixtures.Where(f => f.Strip != null).ToDictionary(f => f.Fixture, f => f.Strip!.Value);
        var plan = new Dictionary<string, StripId>();
        foreach (var fixture in view.Fixtures.Where(f => !f.Locked).Select(f => f.Fixture))
        {
            booked.Remove(fixture);
            var best = view.Strips
                .Where(s => !booked.Any(b => b.Value == s.Id && Overlaps(fixture, b.Key, lockDays)))
                .OrderBy(s => Cost(fixture, s, booked))
                .ThenBy(s => s.Id.Number)
                .First();
            booked[fixture] = best.Id;
            plan[fixture.Id] = best.Id;
        }
        return plan;
    }

    private static double Cost(Fixture fixture, StripView strip, Dictionary<Fixture, StripId> booked)
    {
        var cost = 0.0;
        cost += fixture.Televised ? (strip.Centre ? 0 : 100) : (strip.Centre ? 30 : 0);

        var earlier = booked.Where(b => b.Value == strip.Id && b.Key.End < fixture.Start).Select(b => b.Key).ToList();
        cost += 10 * earlier.Count;
        if (earlier.Count > 0)
        {
            var rest = (fixture.Start - earlier.Max(f => f.End)).Days;
            cost += Math.Max(0, RestWantedDays - rest);
        }

        var neighbours = booked.Where(b => Math.Abs(b.Value.Number - strip.Id.Number) == 1);
        cost += 15 * neighbours.Count(b => Math.Abs((b.Key.Start - fixture.Start).Days) <= NeighbourWindowDays);
        return cost;
    }

    private static bool Overlaps(Fixture a, Fixture b, int lockDays) =>
        a.Start.AddDays(-lockDays) <= b.End && b.Start.AddDays(-lockDays) <= a.End;
}
