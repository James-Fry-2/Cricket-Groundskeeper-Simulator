using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Match;
using Groundsman.Core.Pressures;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Pressures;

public class MatchJudgingTests
{
    private static Game PlayFourDay(StripId strip, bool televised, bool accept, ulong seed = 1)
    {
        var fixture = new Fixture(new DateTime(2027, 6, 20), TestFormats.FourDay, strip, TestTeams.Opponent, televised);
        var content = TestContent.WithStakeholders(TestStakeholders.With(1, 1));
        var game = new Game(new GameSetup(content, new GameTime(2027, 6, 1, 7), new[] { fixture }, seed));
        while (game.View.Now.Date < new DateTime(2027, 6, 5))
        {
            game.Advance();
        }
        foreach (var request in game.View.Requests)
        {
            game.Submit(new AnswerRequest(request.Id, accept));
        }
        while (game.View.Now.Date <= fixture.End.AddDays(2))
        {
            game.Advance();
        }
        return game;
    }

    private static IReadOnlyList<SatisfactionChange> Changes(Game game, Stakeholder who) =>
        game.View.Stakeholders.Single(s => s.Stakeholder == who).Changes;

    [Fact]
    public void After_a_match_accepted_requests_are_judged_and_paid_out()
    {
        var game = PlayFourDay(new StripId(6), televised: true, accept: true);

        foreach (var request in game.View.Requests)
        {
            Assert.Contains(request.Status, new[] { RequestStatus.Delivered, RequestStatus.NotDelivered, RequestStatus.Spoiled });
            var reasons = Changes(game, request.Stakeholder).Select(c => c.Reason).ToList();
            if (request.Status == RequestStatus.Delivered)
            {
                Assert.Contains(SatisfactionReason.RequestDelivered, reasons);
            }
            if (request.Status == RequestStatus.NotDelivered)
            {
                Assert.Contains(SatisfactionReason.RequestNotDelivered, reasons);
            }
        }
    }

    [Fact]
    public void The_captain_follows_the_result_and_the_referee_the_rating()
    {
        var game = PlayFourDay(new StripId(6), televised: false, accept: false);
        var match = game.View.LatestMatch!;
        var captain = Changes(game, Stakeholder.Captain).Select(c => c.Reason).ToList();

        if (match.Result!.Kind == ResultKind.Win)
        {
            Assert.Contains(match.Result.Winner == TestTeams.Home.Name ? SatisfactionReason.HomeWin : SatisfactionReason.HomeLoss, captain);
        }
        var rated = Assert.Single(Changes(game, Stakeholder.Referee));
        Assert.Equal(SatisfactionReason.Rated, rated.Reason);
        Assert.Equal(match.Rating!.Grade, rated.Grade);
    }

    [Fact]
    public void The_board_wants_day_four_televised_matches_on_a_centre_strip_and_no_demerits()
    {
        var centre = PlayFourDay(new StripId(6), televised: true, accept: false);
        var outer = PlayFourDay(new StripId(2), televised: true, accept: false);
        var board = Changes(centre, Stakeholder.Board).Select(c => c.Reason).ToList();
        var match = centre.View.LatestMatch!;

        Assert.Contains(SatisfactionReason.TelevisedCentre, board);
        Assert.Contains(SatisfactionReason.TelevisedOffCentre, Changes(outer, Stakeholder.Board).Select(c => c.Reason));
        Assert.Contains(board, r => r == SatisfactionReason.DayFourReached || r == SatisfactionReason.ShortFourDay);
        if (match.Rating!.Demerits > 0)
        {
            var demerits = Assert.Single(Changes(centre, Stakeholder.Board), c => c.Reason == SatisfactionReason.Demerits);
            Assert.Equal(match.Rating.Demerits, demerits.Demerits);
        }
    }

    [Fact]
    public void A_match_is_judged_once()
    {
        var game = PlayFourDay(new StripId(6), televised: true, accept: true);
        var count = game.View.Stakeholders.Sum(s => s.Changes.Count);

        game.Advance();
        game.Advance();

        Assert.Equal(count, game.View.Stakeholders.Sum(s => s.Changes.Count));
    }
}
