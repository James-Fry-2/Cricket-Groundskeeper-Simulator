using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Pressures;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Pressures;

public class RequestTests
{
    private static readonly Fixture FourDay = new Fixture(new DateTime(2027, 6, 20), TestFormats.FourDay, new StripId(6), TestTeams.Opponent);
    private static readonly Fixture OneDay = new Fixture(new DateTime(2027, 7, 10), TestFormats.OneDay, new StripId(9), TestTeams.Opponent);

    private static Game GameAt(DateTime date, double captainChance = 1, double boardChance = 1, ulong seed = 1, params Fixture[] fixtures)
    {
        var content = TestContent.WithStakeholders(TestStakeholders.With(captainChance, boardChance));
        var game = new Game(new GameSetup(content, new GameTime(2027, 6, 1, 7), fixtures.Length == 0 ? new[] { FourDay } : fixtures, seed));
        while (game.View.Now.Date < date)
        {
            game.Advance();
        }
        return game;
    }

    private static RequestView Captains(Game game) => game.View.Requests.Single(r => r.Stakeholder == Stakeholder.Captain);

    private static double Satisfaction(Game game, Stakeholder who) => game.View.Stakeholders.Single(s => s.Stakeholder == who).Satisfaction;

    [Fact]
    public void Everyone_starts_at_the_starting_satisfaction_with_no_requests()
    {
        var game = GameAt(new DateTime(2027, 6, 4));

        Assert.Empty(game.View.Requests);
        Assert.Equal(new[] { Stakeholder.Captain, Stakeholder.Board, Stakeholder.Referee }, game.View.Stakeholders.Select(s => s.Stakeholder));
        Assert.All(game.View.Stakeholders, s => Assert.Equal(50, s.Satisfaction));
    }

    [Fact]
    public void Requests_arrive_five_days_before_the_lock_with_a_notice_and_are_answered_by_the_lock()
    {
        var game = GameAt(new DateTime(2027, 6, 5));

        var captain = Captains(game);
        var board = game.View.Requests.Single(r => r.Stakeholder == Stakeholder.Board);
        Assert.Same(FourDay, captain.Fixture);
        Assert.Contains(captain.Kind, new[] { RequestKind.Green, RequestKind.Turning, RequestKind.Pace });
        Assert.Equal(RequestKind.LastsFourDays, board.Kind);
        Assert.Equal(new DateTime(2027, 6, 10), captain.AnswerBy);
        Assert.Equal(RequestStatus.Open, captain.Status);
        Assert.Equal(2, game.View.Notices.OfType<RequestNotice>().Count());
    }

    [Fact]
    public void The_board_only_asks_four_day_matches_to_last()
    {
        var game = GameAt(new DateTime(2027, 6, 30), fixtures: new[] { OneDay });

        Assert.Contains(Captains(game).Kind, new[] { RequestKind.Pace, RequestKind.Flat });
        Assert.DoesNotContain(game.View.Requests, r => r.Stakeholder == Stakeholder.Board);
    }

    [Fact]
    public void With_no_chance_of_a_request_none_come()
    {
        Assert.Empty(GameAt(new DateTime(2027, 6, 9), captainChance: 0, boardChance: 0).View.Requests);
    }

    [Fact]
    public void Captains_ask_for_each_character_the_format_allows()
    {
        var kinds = Enumerable.Range(1, 40)
            .Select(seed => Captains(GameAt(new DateTime(2027, 6, 5), seed: (ulong)seed)).Kind)
            .Distinct()
            .ToList();

        Assert.Equal(new[] { RequestKind.Green, RequestKind.Turning, RequestKind.Pace }, kinds.OrderBy(k => k));
    }

    [Fact]
    public void Accepting_waits_for_the_match_and_declining_costs_a_little_now()
    {
        var accepted = GameAt(new DateTime(2027, 6, 5));
        var declined = GameAt(new DateTime(2027, 6, 5));

        Assert.True(accepted.Submit(new AnswerRequest(Captains(accepted).Id, accept: true)).Accepted);
        Assert.True(declined.Submit(new AnswerRequest(Captains(declined).Id, accept: false)).Accepted);

        Assert.Equal(RequestStatus.Accepted, Captains(accepted).Status);
        Assert.Equal(50, Satisfaction(accepted, Stakeholder.Captain));
        Assert.Equal(RequestStatus.Declined, Captains(declined).Status);
        Assert.Equal(50 + TestStakeholders.Settings.CaptainAnswers.Declined, Satisfaction(declined, Stakeholder.Captain));
        var change = declined.View.Notices.OfType<SatisfactionNotice>().Single().Change;
        Assert.Equal(SatisfactionReason.RequestDeclined, change.Reason);
        Assert.Same(FourDay, change.Fixture);
    }

    [Fact]
    public void A_request_left_unanswered_at_the_lock_is_ignored_and_costs_more()
    {
        var game = GameAt(new DateTime(2027, 6, 10));

        Assert.Equal(RequestStatus.Ignored, Captains(game).Status);
        Assert.Equal(50 + TestStakeholders.Settings.CaptainAnswers.Ignored, Satisfaction(game, Stakeholder.Captain));
        Assert.Equal(50 + TestStakeholders.Settings.BoardAnswers.Ignored, Satisfaction(game, Stakeholder.Board));
    }

    [Fact]
    public void A_request_can_be_answered_once_and_not_after_the_lock()
    {
        var game = GameAt(new DateTime(2027, 6, 5));
        var id = Captains(game).Id;

        Assert.True(game.Submit(new AnswerRequest(id, true)).Accepted);
        Assert.False(game.Submit(new AnswerRequest(id, false)).Accepted);
        Assert.False(game.Submit(new AnswerRequest("nonsense", true)).Accepted);

        var late = GameAt(new DateTime(2027, 6, 10));
        Assert.False(late.Submit(new AnswerRequest(Captains(late).Id, true)).Accepted);
    }

    [Fact]
    public void Requests_are_the_same_for_the_same_seed()
    {
        Assert.Equal(Captains(GameAt(new DateTime(2027, 6, 5), seed: 9)).Kind, Captains(GameAt(new DateTime(2027, 6, 5), seed: 9)).Kind);
    }
}
