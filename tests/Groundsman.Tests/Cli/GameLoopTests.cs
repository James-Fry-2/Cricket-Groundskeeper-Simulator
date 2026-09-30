using Groundsman.Cli;
using Groundsman.Core;
using Groundsman.Core.Content;
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
    public void Mowing_is_confirmed_ordered_then_shown_as_the_last_cut()
    {
        var (console, game) = Play("m 3 10", "s", "", "q");

        Assert.Contains("Strip 3 is down for mowing to 10 mm", console.Output);
        Assert.Contains("mow 10", console.Output);
        Assert.Contains("10mm yesterday", console.Output);
        Assert.Equal(10, game.View.Strips[2].LastMown!.HeightMm);
    }

    [Fact]
    public void Rolling_is_confirmed_ordered_then_shown_as_the_last_roll()
    {
        var (console, game) = Play("l 3 medium 20", "s", "", "q");

        Assert.Contains("Strip 3 is down for 20 minutes with the Medium roller", console.Output);
        Assert.Contains("roll", console.Output);
        Assert.Contains("medium 20m yesterday", console.Output);
        Assert.Equal("medium", game.View.Strips[2].LastRolled!.RollerId);
    }

    [Fact]
    public void Repairing_is_confirmed_ordered_then_shown()
    {
        var (console, _) = Play("e 3 sam", "s", "", "q");

        Assert.Contains("Strip 3 is down for end repairs", console.Output);
        Assert.Contains("repair", console.Output);
        Assert.Contains("repaired yesterday", console.Output);
    }

    [Fact]
    public void Mowing_everything_stops_at_the_first_refusal()
    {
        var (_, game) = Play("m all 10 jo", "q");

        Assert.Equal(8, game.View.Strips.Count(s => s.MowingQueued));
    }

    [Fact]
    public void Readings_say_who_took_them()
    {
        var (console, _) = Play("r 3 sam", "s", "q");

        Assert.Contains("today, Sam", console.Output);
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

    private static (TestConsole Console, Game Game) PlayMatch(FormatSettings format, string input)
    {
        var console = new TestConsole();
        console.Profile.Width = 120;
        var fixture = new Groundsman.Core.Fixture(new DateTime(2027, 5, 20), format, new Groundsman.Core.Strips.StripId(6), TestTeams.Opponent);
        var game = new Game(new GameSetup(TestContent.Content, new GameTime(2027, 5, 19, 13), new[] { fixture }, 2));
        new GameLoop(console, game, new StringReader(input)).Run();
        return (console, game);
    }

    [Fact]
    public void A_match_shows_its_scoreboard_commentary_and_verdict()
    {
        var (console, game) = PlayMatch(TestFormats.OneDay, "\n\n\n\n\n");

        var match = game.View.LatestMatch!;
        Assert.True(match.Finished);
        Assert.Contains(Format.Innings(match.Innings[0]), console.Output);
        Assert.Contains(match.Result!.Text, console.Output);
        Assert.Single(console.Output.Split('\n'), l => l.Contains("The referee's verdict: One-day v Test Visitors, strip 6"));
        Assert.Contains(Format.Grade(match.Rating!.Grade), console.Output);
        Assert.Contains(match.Rating.Reasons[0], console.Output);
        Assert.Contains($"for this match. {Format.Demerits(game.View.DemeritsActive)} in the last five years.", console.Output);
        Assert.All(match.Commentary, line => Assert.Contains(line.Text, console.Output));
        Assert.Single(console.Output.Split('\n'), l => l.Contains(match.Commentary[0].Text));
    }

    [Fact]
    public void A_match_day_turn_shows_the_scoreboard_then_commentary_then_the_interval()
    {
        var (console, game) = PlayMatch(TestFormats.FourDay, "\n\n\n\n");
        Assert.Equal(new GameTime(2027, 5, 20, 18), game.View.Now);

        var output = console.Output;
        var turn = output.LastIndexOf("Advanced", StringComparison.Ordinal);
        var score = output.IndexOf(Format.Innings(game.View.LatestMatch!.Innings[0]), turn, StringComparison.Ordinal);
        var commentary = output.IndexOf(game.View.LatestMatch.Commentary[^1].Text, turn, StringComparison.Ordinal);
        var interval = output.IndexOf("Stumps. On strip 6 you can clean the footholes (clean 6) or fill them for the night (fill 6).", turn, StringComparison.Ordinal);

        Assert.True(turn < score && score < commentary && commentary < interval, $"{turn} {score} {commentary} {interval}");
    }

    [Fact]
    public void Status_repeats_the_scoreboard_during_a_match()
    {
        var (console, game) = PlayMatch(TestFormats.FourDay, "\n\n\n\ns\n");

        var line = Format.Innings(game.View.LatestMatch!.Innings[0]);
        var status = console.Output.LastIndexOf("> s", StringComparison.Ordinal);
        Assert.True(console.Output.IndexOf(line, status, StringComparison.Ordinal) > status);
    }

    [Fact]
    public void The_record_lists_each_finished_match_with_its_rating()
    {
        var (console, game) = PlayMatch(TestFormats.OneDay, "\n\n\n\n\nv\n");
        var match = game.View.LatestMatch!;

        var record = console.Output.Substring(console.Output.LastIndexOf("Season record", StringComparison.Ordinal));
        Assert.Contains("20 May", record);
        Assert.Contains("One-day v Test Visitors", record);
        Assert.Contains(match.Result!.Text, record);
        Assert.Contains(Format.Grade(match.Rating!.Grade), record);
    }

    [Fact]
    public void The_record_says_when_nothing_has_been_played()
    {
        var (console, _) = Play("v", "q");

        Assert.Contains("No matches played yet.", console.Output);
    }

    [Fact]
    public void Help_lists_the_commands()
    {
        var (console, _) = Play("h", "q");

        Assert.Contains("r all", console.Output);
        Assert.Contains("w <strip>", console.Output);
        Assert.Contains("c <strip>", console.Output);
        Assert.Contains("d <strip>", console.Output);
        Assert.Contains("m <strip> <mm>", console.Output);
        Assert.Contains("l <strip> <roller> <min>", console.Output);
        Assert.Contains("e <strip>", console.Output);
        Assert.Contains("clean <strip>", console.Output);
        Assert.Contains("fill <strip>", console.Output);
        Assert.Contains("u <strip>", console.Output);
        Assert.Contains("season record", console.Output);
    }

    [Fact]
    public void The_fixture_list_shows_strips_and_locks_and_p_assigns_one()
    {
        var console = new TestConsole();
        console.Profile.Width = 120;
        var open = new Groundsman.Core.Fixture(new DateTime(2027, 5, 20), TestFormats.OneDay, null, TestTeams.Opponent, televised: true);
        var game = new Game(new GameSetup(TestContent.Content, new GameTime(2027, 5, 1, 7), new[] { open }, 1));

        new GameLoop(console, game, new StringReader("x\np 1 7\np 2 7\nx\n")).Run();

        var output = console.Output;
        Assert.Contains("Fixtures", output);
        Assert.Contains("10 May", output);
        Assert.Contains("TV", output);
        Assert.Contains("none", output);
        Assert.Contains("One-day v Test Visitors on Thu 20 May will be played on strip 7.", output);
        Assert.Contains("There's no fixture 2.", output);
        Assert.Equal(new Groundsman.Core.Strips.StripId(7), game.View.Fixtures[0].Strip);
    }

    [Fact]
    public void A_default_strip_at_the_lock_is_announced()
    {
        var console = new TestConsole();
        console.Profile.Width = 200;
        var open = new Groundsman.Core.Fixture(new DateTime(2027, 5, 20), TestFormats.OneDay, null, TestTeams.Opponent);
        var game = new Game(new GameSetup(TestContent.Content, new GameTime(2027, 5, 9, 7), new[] { open }, 1));

        new GameLoop(console, game, new StringReader("\n")).Run();

        Assert.Contains("No strip was chosen for One-day v Test Visitors on Thu 20 May, so the head groundsman has put it on strip 1.", console.Output);
    }

    [Fact]
    public void Centre_strips_are_marked_in_the_table()
    {
        var (console, _) = Play("q");

        Assert.Contains(" 5c ", console.Output);
        Assert.Contains(" 8c ", console.Output);
        Assert.DoesNotContain(" 4c ", console.Output);
        Assert.Contains("c: a centre strip", console.Output);
    }
}
