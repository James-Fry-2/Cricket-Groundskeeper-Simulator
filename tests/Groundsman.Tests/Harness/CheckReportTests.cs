using Groundsman.Core.Content;
using Groundsman.Core.Match;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;
using Groundsman.Harness;

namespace Groundsman.Tests.Harness;

public class CheckReportTests
{
    private static MatchResult Result(ulong seed, PitchGrade? grade, ResultKind kind = ResultKind.Win, bool onTarget = false, params string[] reasons) =>
        new MatchResult(seed, 0, new StripId(1), new GameTime(2027, 5, 1, 8), 20, 25, onTarget ? MatchMorningMiss.None : MatchMorningMiss.SubsurfaceDry)
        {
            Result = kind,
            Grade = grade,
            Demerits = grade switch { PitchGrade.Unsatisfactory => 1, PitchGrade.Unfit => 3, _ => 0 },
            ReasonIds = reasons,
        };

    private static IReadOnlyList<MatchResult>[] Seasons(params MatchResult[][] seasons) => seasons;

    // Neglect: every match fails. Random: mixed. By the book: three of four at least satisfactory.
    private static readonly IReadOnlyList<IReadOnlyList<MatchResult>[]> Runs = new[]
    {
        Seasons(
            new[] { Result(1, PitchGrade.Unsatisfactory, reasons: "dead"), Result(1, PitchGrade.Unfit, reasons: "dangerous") },
            new[] { Result(2, PitchGrade.Unsatisfactory, ResultKind.Draw, reasons: "dead"), Result(2, PitchGrade.Unsatisfactory, reasons: new[] { "dead", "uneven" }) }),
        Seasons(
            new[] { Result(1, PitchGrade.Satisfactory), Result(1, PitchGrade.Unsatisfactory, reasons: "uneven") },
            new[] { Result(2, null, ResultKind.NoResult), Result(2, PitchGrade.Satisfactory) }),
        Seasons(
            new[] { Result(1, PitchGrade.VeryGood, onTarget: true), Result(1, PitchGrade.Satisfactory, ResultKind.Draw) },
            new[] { Result(2, PitchGrade.Satisfactory, onTarget: true), Result(2, PitchGrade.Unsatisfactory, reasons: "bowlers") }),
    };

    private static CheckReport Report(double satisfactory = 0.7, double neglectDemerits = 0.3) =>
        CheckReport.From(Runs, TestRating.Settings, satisfactory, neglectDemerits);

    [Fact]
    public void Counts_grades_among_rated_matches_and_demerits_per_season()
    {
        var neglect = Report().Policies[0];
        var random = Report().Policies[1];
        var book = Report().Policies[2];

        Assert.Equal(4, neglect.Matches);
        Assert.Equal(0.25, neglect.Unfit);
        Assert.Equal(0.75, neglect.Unsatisfactory);
        Assert.Equal(1.0, neglect.WithDemerits);
        Assert.Equal(3.0, neglect.DemeritsPerSeason);
        Assert.Equal(0.25, neglect.Draws);

        Assert.Equal(3, random.Rated);
        Assert.Equal(2.0 / 3, random.SatisfactoryOrBetter, 9);
        Assert.Equal(0.25, random.NoResults);

        Assert.Equal(0.25, book.VeryGood);
        Assert.Equal(0.75, book.SatisfactoryOrBetter);
        Assert.Equal(0.5, book.OnTarget);
    }

    [Fact]
    public void Counts_seasons_that_reach_the_ban_threshold()
    {
        var neglect = Report().Policies[0];

        var expected = new[] { 4, 2 }.Count(d => d >= TestRating.Settings.BanAt) / 2.0;
        Assert.Equal(expected, neglect.BannedSeasons);
    }

    [Fact]
    public void Breaks_failures_down_by_reason_as_a_share_of_matches()
    {
        var neglect = Report().Policies[0];

        Assert.Equal(0.75, neglect.Faults["dead"]);
        Assert.Equal(0.25, neglect.Faults["uneven"]);
        Assert.Equal(0.25, neglect.Faults["dangerous"]);
        Assert.False(Report().Policies[2].Faults.ContainsKey("dead"));
    }

    [Fact]
    public void Passes_only_when_by_the_book_is_good_enough_and_neglect_is_punished()
    {
        Assert.True(Report().Passed);
        Assert.False(Report(satisfactory: 0.8).Passed);
        Assert.False(Report(neglectDemerits: 1.01).Passed);
    }

    [Fact]
    public void The_summary_names_each_policy_and_the_verdict()
    {
        var text = Report().SummaryTable();

        Assert.Contains("by the book", text);
        Assert.Contains("Check 3 PASSED", text);
        Assert.Contains("dead", text);
    }
}
