using Groundsman.Cli;
using Groundsman.Core;
using Groundsman.Core.Time;
using Spectre.Console.Testing;

namespace Groundsman.Tests.Cli;

public class GameLoopTests
{
    private static (TestConsole Console, Game Game) Play(params string[] lines)
    {
        var console = new TestConsole().Interactive();
        console.Profile.Width = 120;
        foreach (var line in lines)
        {
            console.Input.PushTextWithEnter(line);
        }

        var game = new Game(TestContent.Setup(new GameTime(2027, 5, 10, 7), new[] { new DateTime(2027, 5, 20) }));
        new GameLoop(console, game).Run();
        return (console, game);
    }

    [Fact]
    public void Shows_the_ground_date_pace_and_next_match_at_the_start()
    {
        var (console, _) = Play("q");

        Assert.Contains("Test Ground", console.Output);
        Assert.Contains("Mon 10 May 2027, 07:00", console.Output);
        Assert.Contains("In season", console.Output);
        Assert.Contains("Thu 20 May (in 10 days)", console.Output);
    }

    [Fact]
    public void Reading_a_strip_reports_the_range()
    {
        var (console, game) = Play("r 3", "q");
        var range = game.View.Strips[2].SurfaceMoisture!.Range;

        Assert.Contains($"Strip 3 reads {Format.Percent(range)} surface moisture", console.Output);
    }

    [Fact]
    public void Reading_all_reads_every_strip()
    {
        var (_, game) = Play("r all", "q");

        Assert.All(game.View.Strips, strip => Assert.NotNull(strip.SurfaceMoisture));
    }

    [Fact]
    public void Watering_is_confirmed_then_carried_out_on_advance()
    {
        var (console, game) = Play("w 3", "", "q");

        Assert.Contains("Strip 3 is down for watering", console.Output);
        Assert.Contains("Advanced 24 hours to Tue 11 May 2027, 07:00", console.Output);
        Assert.Equal(new GameTime(2027, 5, 11, 7), game.View.Now);
        Assert.Equal(TestGround.Settings.Strips[2].SurfaceMoisture + TestContent.Tasks.WaterSurfaceGain, game.Square.Get(new Groundsman.Core.Strips.StripId(3)).SurfaceMoisture);
    }

    [Fact]
    public void A_rejected_command_shows_the_reason()
    {
        var (console, _) = Play("w 3", "w 3", "q");

        Assert.Contains("Strip 3 is already down for watering", console.Output);
    }

    [Fact]
    public void Invalid_input_points_to_help()
    {
        var (console, _) = Play("dig", "q");

        Assert.Contains("Type h for help", console.Output);
    }

    [Fact]
    public void Piped_input_is_echoed_and_its_end_quits()
    {
        var console = new TestConsole();
        var game = new Game(TestContent.Setup(new GameTime(2027, 5, 10, 7)));

        new GameLoop(console, game, new StringReader("w 3\n\n")).Run();

        Assert.Contains("> w 3", console.Output);
        Assert.Equal(new GameTime(2027, 5, 11, 7), game.View.Now);
    }

    [Fact]
    public void Help_lists_the_commands()
    {
        var (console, _) = Play("h", "q");

        Assert.Contains("r all", console.Output);
        Assert.Contains("w <strip>", console.Output);
    }
}
