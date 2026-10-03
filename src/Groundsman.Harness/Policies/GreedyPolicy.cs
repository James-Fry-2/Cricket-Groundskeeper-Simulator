using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Readings;
using Groundsman.Core.Strips;

namespace Groundsman.Harness.Policies;

/// <summary>
/// Prepares by the book, but takes each fixture as it comes: just before its strip locks it
/// gets the best-looking strip now: established ends first, then a centre strip, then the one
/// rested longest. Requests are answered as planned play answers them, so Gate B compares strip
/// rotation alone. Strong early, short of good strips late.
/// </summary>
public sealed class GreedyPolicy : ByTheBookPolicy
{
    public override string Name => "greedy";

    protected override void ChooseStrips(IGame game)
    {
        var today = game.View.Now.Date;
        foreach (var fixture in game.View.Fixtures.Where(f => !f.Locked && f.Strip == null && (f.LocksOn - today).Days <= 1))
        {
            // Look at every strip's ends first: the best-looking strip is one whose ends are
            // established, then a centre one, then the one rested longest.
            foreach (var strip in game.View.Strips.Where(s => s.Ends?.TakenAt.Date != today))
            {
                game.Submit(new TakeReading(strip.Id, null, ReadingSource.Feel));
            }
            var byLooks = game.View.Strips
                .OrderByDescending(s => s.Ends?.State ?? EndsState.Established)
                .ThenByDescending(s => s.Centre)
                .ThenBy(s => s.LastPlayed?.End ?? DateTime.MinValue)
                .ThenBy(s => s.Id.Number);
            foreach (var strip in byLooks)
            {
                if (game.Submit(new AssignStrip(fixture.Id, strip.Id)).Accepted)
                {
                    break;
                }
            }
        }
    }

    protected override void AnswerRequests(IGame game) => PlannedPolicy.Answer(game);
}
