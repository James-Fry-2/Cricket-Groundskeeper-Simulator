using Groundsman.Cli;
using Groundsman.Core;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;
using Spectre.Console.Testing;

namespace Groundsman.Tests.Cli;

public class OnboardingTests
{
    private static readonly Fixture[] Fixtures =
    {
        new Fixture(new DateTime(2027, 5, 20), TestFormats.OneDay, null, TestTeams.Opponent),
        new Fixture(new DateTime(2027, 6, 10), TestFormats.OneDay, null, TestTeams.Opponent),
    };

    private static Game NewGame() =>
        new Game(new GameSetup(TestContent.WithStakeholders(TestStakeholders.With(1, 1)), new GameTime(2027, 5, 1, 7), Fixtures, 1));

    private static string Play(TipBook tips, string input, Game? game = null)
    {
        var console = new TestConsole();
        console.Profile.Width = 160;
        new GameLoop(console, game ?? NewGame(), new StringReader(input), tips: tips).Run();
        return console.Output;
    }

    [Fact]
    public void Each_tip_shows_the_first_time_its_moment_comes_and_never_again()
    {
        var tips = new TipBook();
        var input = string.Join("\n", Enumerable.Repeat("", 40));

        var output = Play(tips, input);

        Assert.Contains(Onboarding.Tips["start"].Substring(0, 40), output);
        Assert.Contains(Onboarding.Tips["request"].Substring(0, 40), output);
        Assert.Contains(Onboarding.Tips["lock"].Substring(0, 40), output);
        Assert.Contains(Onboarding.Tips["match"].Substring(0, 40), output);
        Assert.Contains(Onboarding.Tips["verdict"].Substring(0, 40), output);
        Assert.All(new[] { "start", "request", "lock", "match", "verdict" }, id =>
            Assert.Single(output.Split('\n'), line => line.Contains(Onboarding.Tips[id].Substring(0, 40))));
    }

    [Fact]
    public void A_later_season_doesnt_repeat_tips_already_seen()
    {
        var tips = new TipBook();
        Play(tips, string.Join("\n", Enumerable.Repeat("", 40)));

        var second = Play(tips, string.Join("\n", Enumerable.Repeat("", 40)));

        Assert.DoesNotContain(Onboarding.Tips["request"].Substring(0, 40), second);
    }

    [Fact]
    public void Seen_tips_are_remembered_on_disk()
    {
        var path = Path.Combine(Path.GetTempPath(), $"groundsman-tips-{Guid.NewGuid():N}.json");
        var tips = TipBook.Load(path);
        Assert.True(tips.Show("start"));
        Assert.False(tips.Show("start"));

        Assert.False(TipBook.Load(path).Show("start"));
        Assert.True(TipBook.Load(path).Show("request"));
        File.Delete(path);
    }

    [Fact]
    public void Tips_off_stops_them_for_good()
    {
        var tips = new TipBook();

        var output = Play(tips, "tips off\n" + string.Join("\n", Enumerable.Repeat("", 40)));

        Assert.Contains("Tips are off.", output);
        Assert.DoesNotContain(Onboarding.Tips["request"].Substring(0, 40), output);
        Assert.False(tips.Show("match"));
    }

    [Fact]
    public void Intro_shows_the_introduction_again()
    {
        var output = Play(new TipBook(), "intro\n");

        Assert.Contains("You are head groundsman at Kestrel Lane", output);
    }
}
