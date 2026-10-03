using Groundsman.Cli;
using Groundsman.Core;
using Groundsman.Core.Time;
using Groundsman.Harness;
using Spectre.Console.Testing;

namespace Groundsman.Tests.Harness;

public class PlaytestReportTests
{
    private static readonly Fixture[] Fixtures =
    {
        new Fixture(new DateTime(2027, 5, 10), TestFormats.OneDay, null, TestTeams.Opponent),
        new Fixture(new DateTime(2027, 5, 25), TestFormats.OneDay, null, TestTeams.Opponent),
        new Fixture(new DateTime(2027, 6, 10), TestFormats.OneDay, null, TestTeams.Opponent),
        new Fixture(new DateTime(2027, 6, 25), TestFormats.OneDay, null, TestTeams.Opponent),
        new Fixture(new DateTime(2027, 7, 10), TestFormats.OneDay, null, TestTeams.Opponent),
        new Fixture(new DateTime(2027, 7, 25), TestFormats.OneDay, null, TestTeams.Opponent),
    };

    private static readonly Groundsman.Core.Content.GameContent Content = TestContent.WithStakeholders(TestStakeholders.With(1, 1));

    private static string Root() => Path.Combine(Path.GetTempPath(), $"groundsman-playtest-{Guid.NewGuid():N}");

    /// <summary>A tester's season folder, played by feeding the Cli these lines.</summary>
    private static void Season(string root, string tester, ulong seed, string input)
    {
        var folder = Path.Combine(root, tester, $"season-2026-10-03-1000-{seed}");
        var game = RecordingGame.Start(Content, Fixtures, new GameTime(2027, 5, 1, 7), seed, "hash", Path.Combine(folder, PlaytestFolder.SaveName), telemetry: new Telemetry(Path.Combine(folder, Telemetry.FileName)));
        new GameLoop(new TestConsole(), game, new StringReader(input)).Run();
    }

    private static string Lines(params string[] lines) => string.Join("\n", lines) + "\n";

    private static PlaytestReport Run(string root) => PlaytestReport.Run(root, Content, Fixtures, "hash");

    [Fact]
    public void A_planner_who_reads_and_turns_requests_down_scores_on_each_row()
    {
        var root = Root();
        // The first fixture is already locked when the season starts, so it's planned from the second.
        var input = Lines("p 2 3", "p 3 4", "p 4 5", "p 5 6", "p 6 7", "f 2", "w 2", "f 3", "w 3", "r 4", "w 4", "f 5", "w 5", "r 6", "w 6")
            + string.Join("\n", Enumerable.Repeat("ff\nno 1", 80)) + "\n";
        Season(root, "T1", 1, input);

        var season = Assert.Single(Run(root).Seasons);

        Assert.Equal("T1", season.Tester);
        Assert.True(season.Replayed, season.ReplayNote);
        Assert.True(season.Finished);
        Assert.Equal(4, season.FixturesPlannedAhead);
        Assert.True(season.PlansAhead);
        Assert.Equal(5, season.KeyJobs);
        Assert.Equal(5, season.KeyJobsAfterReading);
        Assert.True(season.ReadingsDrive);
        Assert.True(season.TurnsDown);
        Assert.True(season.FastForwards > 0);
        Assert.True(season.PaceHolds);
    }

    [Fact]
    public void A_player_who_just_presses_on_scores_on_none_and_the_long_runs_show()
    {
        var root = Root();
        Season(root, "T2", 2, string.Join("\n", Enumerable.Repeat("", 30)) + "\n");

        var season = Assert.Single(Run(root).Seasons);

        Assert.False(season.Finished);
        Assert.False(season.PlansAhead);
        Assert.False(season.ReadingsDrive);
        Assert.False(season.TurnsDown);
        Assert.Equal(30, season.LongestPlainRun);
        Assert.False(season.PaceHolds);
    }

    [Fact]
    public void A_save_from_other_content_is_reported_not_replayed()
    {
        var root = Root();
        Season(root, "T3", 3, Lines("", ""));

        var season = Assert.Single(PlaytestReport.Run(root, Content, Fixtures, "other").Seasons);

        Assert.False(season.Replayed);
        Assert.Contains("content", season.ReplayNote);
    }

    [Fact]
    public void Gate_c_combines_finishing_from_the_logs_with_the_questionnaire_answers()
    {
        var root = Root();
        var ff = string.Join("\n", Enumerable.Repeat("ff", 120)) + "\n";
        Season(root, "T1", 1, ff);
        Season(root, "T2", 2, ff);
        Season(root, "T3", 3, Lines(""));
        File.WriteAllText(Path.Combine(root, PlaytestReport.AnswersName), "tester,explained_verdict,play_again\nT1,yes,yes\nT2,yes,maybe\nT3,no,yes\n");

        var report = Run(root);

        Assert.Equal(3, report.Testers.Count);
        Assert.Equal(2, report.Testers.Count(t => t.Finished));
        Assert.Equal(2, report.Testers.Count(t => t.ExplainedVerdict == true));
        Assert.Equal(2, report.Testers.Count(t => t.PlayAgain == true));
        Assert.True(report.GateCPassed);
        Assert.Contains("Gate C PASSED", report.Summary());
    }

    [Fact]
    public void Without_answers_gate_c_waits_for_them()
    {
        var root = Root();
        Season(root, "T1", 1, Lines(""));

        var report = Run(root);

        Assert.Null(report.Testers[0].PlayAgain);
        Assert.False(report.GateCPassed);
        Assert.Contains(PlaytestReport.AnswersName, report.Summary());
    }
}
