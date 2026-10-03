using Groundsman.Core;
using Groundsman.Core.Match;
using Groundsman.Core.Pressures;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Review;

public class SeasonReviewTests
{
    private static readonly Fixture First = new Fixture(new DateTime(2027, 6, 10), TestFormats.FourDay, new StripId(6), TestTeams.Opponent, televised: true);
    private static readonly Fixture Second = new Fixture(new DateTime(2027, 6, 25), TestFormats.OneDay, new StripId(2), TestTeams.Opponent);

    private static Game Season(DateTime until, ulong seed = 1)
    {
        var content = TestContent.WithStakeholders(TestStakeholders.With(1, 1));
        var game = new Game(new GameSetup(content, new GameTime(2027, 6, 1, 7), new[] { First, Second }, seed));
        while (game.View.Now.Date < until)
        {
            game.Advance();
        }
        return game;
    }

    [Fact]
    public void There_is_no_review_until_the_last_fixture_is_over()
    {
        Assert.Null(Season(new DateTime(2027, 6, 24)).View.Review);
        Assert.NotNull(Season(new DateTime(2027, 6, 26)).View.Review);
    }

    [Fact]
    public void The_review_gives_each_stakeholders_satisfaction_mood_and_biggest_reasons()
    {
        var view = Season(new DateTime(2027, 6, 26)).View;
        var review = view.Review!;

        Assert.Equal(new[] { Stakeholder.Captain, Stakeholder.Board, Stakeholder.Referee }, review.Stakeholders.Select(s => s.Stakeholder));
        foreach (var stakeholder in review.Stakeholders)
        {
            var live = view.Stakeholders.Single(s => s.Stakeholder == stakeholder.Stakeholder);
            Assert.Equal(live.Satisfaction, stakeholder.Satisfaction);
            Assert.Equal(TestStakeholders.Settings.MoodFor(live.Satisfaction), stakeholder.Mood);
            Assert.True(stakeholder.TopReasons.Count <= 3);
            Assert.Equal(stakeholder.TopReasons.Select(r => Math.Abs(r.Total)).OrderByDescending(t => t), stakeholder.TopReasons.Select(r => Math.Abs(r.Total)));
            Assert.All(stakeholder.TopReasons, r => Assert.Equal(
                live.Changes.Where(c => c.Reason == r.Reason && c.Request == r.Request).Sum(c => c.Delta), r.Total, 9));
        }
    }

    [Fact]
    public void The_review_counts_ratings_results_and_demerits()
    {
        var view = Season(new DateTime(2027, 6, 26)).View;
        var review = view.Review!;

        Assert.Equal(view.Matches.Count(m => m.Rating?.Grade == PitchGrade.Unsatisfactory), review.Unsatisfactory);
        Assert.Equal(view.Matches.Count(m => m.Rating != null), review.VeryGood + review.Satisfactory + review.Unsatisfactory + review.Unfit);
        Assert.Equal(2, review.Wins + review.Losses + review.Draws + review.NoResults);
        Assert.Equal(view.DemeritsActive, review.Demerits);
        Assert.Equal(view.Banned, review.Banned);
    }

    [Fact]
    public void The_end_of_season_walk_reports_how_worn_each_strip_is()
    {
        var review = Season(new DateTime(2027, 6, 26)).View.Review!;

        Assert.Equal(12, review.Square.Count);
        Assert.Equal(1, review.Square[5].Matches);
        Assert.Equal(1, review.Square[1].Matches);
        Assert.Equal(0, review.Square[9].Matches);
        Assert.Equal(SquareWear.Fresh, review.Square[9].Wear);
        Assert.NotEqual(SquareWear.Fresh, review.Square[5].Wear);
    }

    [Theory]
    [InlineData(80, Mood.Delighted)]
    [InlineData(60, Mood.Content)]
    [InlineData(45, Mood.Uneasy)]
    [InlineData(20, Mood.Unhappy)]
    public void Moods_follow_satisfaction(double satisfaction, Mood mood)
    {
        Assert.Equal(mood, TestStakeholders.Settings.MoodFor(satisfaction));
    }
}
