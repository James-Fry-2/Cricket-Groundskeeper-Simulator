using Groundsman.Core;
using Groundsman.Core.Pressures;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;
using Groundsman.Harness.Policies;

namespace Groundsman.Tests.Harness;

public class StripPolicyTests
{
    private static IReadOnlyList<Fixture> ShippedFixtures()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "content");
        var formats = Groundsman.Core.Content.ContentParser.ParseFormats(File.ReadAllText(Path.Combine(directory, "formats.json")));
        var teams = Groundsman.Core.Content.ContentParser.ParseTeams(File.ReadAllText(Path.Combine(directory, "teams.json")));
        var season = Groundsman.Core.Content.ContentParser.ParseSeason(File.ReadAllText(Path.Combine(directory, "season.json")), formats, teams);
        return season.Fixtures.Select(f => new Fixture(f.Start, TestFormats.All.Single(t => t.Id == f.Format.Id), null, TestTeams.Opponent, f.Televised)).ToArray();
    }

    private static Game ShippedSeason(double captainChance = 0, double boardChance = 0) =>
        new Game(new GameSetup(TestContent.WithStakeholders(TestStakeholders.With(captainChance, boardChance)), new GameTime(2027, 3, 25, 7), ShippedFixtures(), 1));

    [Fact]
    public void Planned_rotation_puts_televised_matches_on_centre_strips_and_uses_no_strip_three_times()
    {
        var game = ShippedSeason();
        var plan = PlannedPolicy.Plan(game.View);

        Assert.Equal(game.View.Fixtures.Count, plan.Count);
        foreach (var fixture in game.View.Fixtures)
        {
            var strip = plan[fixture.Id];
            Assert.Equal(fixture.Fixture.Televised, game.View.Strips[strip.Number - 1].Centre);
        }
        Assert.All(plan.Values.GroupBy(s => s), g => Assert.InRange(g.Count(), 1, 2));
    }

    [Fact]
    public void Planned_play_assigns_its_whole_rotation_on_the_first_turn()
    {
        var game = ShippedSeason();

        new PlannedPolicy().PlayTurn(game);

        Assert.All(game.View.Fixtures, f => Assert.NotNull(f.Strip));
    }

    [Fact]
    public void Greedy_waits_until_the_day_before_the_lock_then_takes_a_centre_strip_with_established_ends()
    {
        var game = ShippedSeason();
        var policy = new GreedyPolicy();
        var first = game.View.Fixtures[0];

        while (game.View.Now.Date < first.LocksOn.AddDays(-2))
        {
            policy.PlayTurn(game);
            Assert.Null(game.View.Fixtures[0].Strip);
            game.Advance();
        }
        while (game.View.Fixtures[0].Strip == null)
        {
            policy.PlayTurn(game);
            game.Advance();
        }

        var strip = game.View.Fixtures[0].Strip!.Value;
        Assert.True(game.View.Strips[strip.Number - 1].Centre);
        Assert.False(game.View.Fixtures[0].Locked && game.View.Notices.OfType<StripLockedNotice>().Any(n => n.ByDefault));
    }

    [Fact]
    public void Both_say_yes_only_to_what_a_true_well_prepared_pitch_tends_to_give()
    {
        foreach (IPolicy policy in new IPolicy[] { new GreedyPolicy(), new PlannedPolicy() })
        {
            var game = ShippedSeason(captainChance: 1, boardChance: 1);
            while (game.View.Requests.Count < 4)
            {
                policy.PlayTurn(game);
                game.Advance();
            }
            policy.PlayTurn(game);

            Assert.All(game.View.Requests.Where(r => r.Status != RequestStatus.Ignored), r =>
                Assert.Equal(PlannedPolicy.Accepts.Contains(r.Kind) ? RequestStatus.Accepted : RequestStatus.Declined, r.Status));
        }
    }
}
