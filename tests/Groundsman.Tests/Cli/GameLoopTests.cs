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
        Assert.Contains("One-day v Test Visitors, Thu 20 May on strip 1 (in 10 days)", console.Output);
    }

    [Fact]
    public void Shows_the_forecast_from_today()
    {
        var (console, game) = Play("q");
        var today = game.View.Forecast[0];

        Assert.Contains("Forecast", console.Output);
        Assert.Contains("Today", console.Output);
        Assert.Contains("Sun 16", console.Output);
        Assert.Contains(Format.Temperature(today.MaxTemperature), console.Output);
        Assert.Contains("Chance", console.Output);
        Assert.Contains(Format.Chance(today.ChanceOfRain), console.Output);
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
        var (_, unwatered) = Play("", "q");

        Assert.Contains("Strip 3 is down for watering", console.Output);
        Assert.Contains("Advanced 24 hours to Tue 11 May 2027, 07:00", console.Output);
        Assert.Contains("Rain in the last 24 hours:", console.Output);
        Assert.Equal(new GameTime(2027, 5, 11, 7), game.View.Now);
        Assert.True(game.Inspect().Strips[2].SurfaceMoisture > unwatered.Inspect().Strips[2].SurfaceMoisture);
    }

    [Fact]
    public void Covering_is_confirmed_shown_as_an_order_then_as_covered()
    {
        var (console, game) = Play("c 5", "s", "", "q");

        Assert.Contains("Strip 5 is down for covering", console.Output);
        Assert.Contains("cover on", console.Output);
        Assert.Contains("covered", console.Output);
        Assert.Contains($"Covers free: {TestContent.Covers.Count - 1} of {TestContent.Covers.Count}", console.Output);
        Assert.True(game.View.Strips[4].Covered);
    }

    [Fact]
    public void Uncovering_is_confirmed()
    {
        var (console, game) = Play("c 5", "", "u 5", "", "q");

        Assert.Contains("Strip 5 is down for uncovering", console.Output);
        Assert.False(game.View.Strips[4].Covered);
    }

    [Fact]
    public void Shows_everyones_hours_left_today()
    {
        var (console, _) = Play("w 1 sam", "s", "q");

        Assert.Contains("Hours left today: You 8/8, Sam Test 8/8, Jo Test 4/4.", console.Output);
        Assert.Contains("Hours left today: You 8/8, Sam Test 7/8, Jo Test 4/4.", console.Output);
    }

    [Fact]
    public void Reading_everything_stops_at_the_first_refusal()
    {
        var (console, game) = Play("w 1 jo", "w 2 jo", "w 3 jo", "r all jo", "q");

        Assert.Equal(4, game.View.Strips.Count(s => s.SurfaceMoisture != null));
        Assert.Contains("Jo Test has 0 hours left today", console.Output);
        Assert.Single(console.Output.Split('\n'), line => line.Contains("hours left today;"));
    }

    [Fact]
    public void Reading_everything_skips_strips_already_read_this_turn()
    {
        var (console, game) = Play("r 3", "r all", "q");

        Assert.All(game.View.Strips, s => Assert.NotNull(s.SurfaceMoisture));
        Assert.DoesNotContain("already been read", console.Output);
    }

    [Fact]
    public void A_feel_reading_reports_its_word()
    {
        var (console, game) = Play("f 3", "q");

        Assert.Contains($"Strip 3 feels {game.View.Strips[2].SurfaceMoisture!.Word}", console.Output);
    }

    [Fact]
    public void An_old_reading_shows_its_widened_range()
    {
        var (console, game) = Play("r 3", "", "", "q");
        var strip = game.View.Strips[2];

        Assert.Contains(Format.Percent(strip.SurfaceMoistureNow!.Value), console.Output);
        Assert.Contains("2 days ago", console.Output);
    }

    [Fact]
    public void A_soil_core_reports_and_shows_moisture_below()
    {
        var (console, game) = Play("d 3", "s", "q");
        var core = game.View.Strips[2].SubsurfaceMoisture!;

        Assert.Contains($"Strip 3 cores {Format.Percent(core.Range)} below the surface", console.Output);
        Assert.Contains("Below", console.Output);
    }

    [Fact]
    public void Readings_say_who_took_them()
    {
        var (console, _) = Play("r 3 sam", "s", "q");

        Assert.Contains("today, Sam Test", console.Output);
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
    public void Debug_mode_shows_true_moisture_beside_readings()
    {
        var console = new TestConsole();
        console.Profile.Width = 120;
        var game = new Game(TestContent.Setup(new GameTime(2027, 5, 10, 7)));

        new GameLoop(console, game, new StringReader(""), game.Inspect).Run();

        Assert.Contains("Truth", console.Output);
        Assert.Contains($"{TestGround.Settings.Strips[2].SurfaceMoisture:0.0}%", console.Output);
    }

    [Fact]
    public void Normal_mode_never_shows_truth()
    {
        var (console, _) = Play("q");

        Assert.DoesNotContain("Truth", console.Output);
    }

    [Fact]
    public void Help_lists_the_commands()
    {
        var (console, _) = Play("h", "q");

        Assert.Contains("r all", console.Output);
        Assert.Contains("w <strip>", console.Output);
        Assert.Contains("c <strip>", console.Output);
        Assert.Contains("d <strip>", console.Output);
        Assert.Contains("u <strip>", console.Output);
    }
}
