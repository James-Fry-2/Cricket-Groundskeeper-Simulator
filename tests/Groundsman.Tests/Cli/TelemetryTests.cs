using System.Text.Json;
using Groundsman.Cli;
using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;
using Spectre.Console.Testing;

namespace Groundsman.Tests.Cli;

public class TelemetryTests
{
    private static readonly Fixture[] Fixtures =
    {
        new Fixture(new DateTime(2027, 5, 20), TestFormats.OneDay, null, TestTeams.Opponent),
        new Fixture(new DateTime(2027, 6, 10), TestFormats.OneDay, null, TestTeams.Opponent),
        new Fixture(new DateTime(2027, 6, 30), TestFormats.OneDay, null, TestTeams.Opponent),
    };

    private sealed class Clock
    {
        public DateTime Now { get; set; } = new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc);
    }

    private static (RecordingGame Game, string Path, Clock Clock) NewGame(ulong seed = 1)
    {
        var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"groundsman-telemetry-{Guid.NewGuid():N}");
        var clock = new Clock();
        var telemetry = new Telemetry(System.IO.Path.Combine(folder, Telemetry.FileName), () => clock.Now);
        var content = TestContent.WithStakeholders(TestStakeholders.With(1, 1));
        var game = RecordingGame.Start(content, Fixtures, new GameTime(2027, 5, 1, 7), seed, "hash", System.IO.Path.Combine(folder, PlaytestFolder.SaveName), telemetry: telemetry);
        return (game, System.IO.Path.Combine(folder, Telemetry.FileName), clock);
    }

    private static List<JsonElement> Lines(string path) =>
        File.ReadAllLines(path).Select(l => JsonDocument.Parse(l).RootElement).ToList();

    [Fact]
    public void Every_command_is_logged_with_its_turn_whether_accepted_or_not()
    {
        var (game, path, _) = NewGame();

        game.Submit(new WaterStrip(new StripId(3)));
        game.Submit(new WaterStrip(new StripId(3)));

        var commands = Lines(path).Where(l => l.GetProperty("event").GetString() == "command").ToList();
        Assert.Equal(2, commands.Count);
        Assert.All(commands, c =>
        {
            Assert.Equal("water", c.GetProperty("type").GetString());
            Assert.Equal(3, c.GetProperty("strip").GetInt32());
            Assert.Equal(0, c.GetProperty("turn").GetInt32());
            Assert.Equal("2027-05-01 07:00", c.GetProperty("game").GetString());
        });
        Assert.True(commands[0].GetProperty("accepted").GetBoolean());
        Assert.False(commands[1].GetProperty("accepted").GetBoolean());
        Assert.False(string.IsNullOrEmpty(commands[1].GetProperty("reason").GetString()));
    }

    [Fact]
    public void Advances_record_the_time_spent_on_the_turn_and_the_commands_given()
    {
        var (game, path, clock) = NewGame();

        clock.Now = clock.Now.AddSeconds(40);
        game.Submit(new WaterStrip(new StripId(3)));
        clock.Now = clock.Now.AddSeconds(5);
        game.Advance();
        clock.Now = clock.Now.AddSeconds(2);
        game.Advance();

        var advances = Lines(path).Where(l => l.GetProperty("event").GetString() == "advance").ToList();
        Assert.Equal(45, advances[0].GetProperty("seconds").GetDouble());
        Assert.Equal(1, advances[0].GetProperty("commands").GetInt32());
        Assert.Equal(2, advances[1].GetProperty("seconds").GetDouble());
        Assert.Equal(0, advances[1].GetProperty("commands").GetInt32());
        Assert.Equal(1, advances[1].GetProperty("turn").GetInt32());
        Assert.Equal("2027-05-01 07:00", advances[0].GetProperty("from").GetString());
    }

    [Fact]
    public void Strip_assignments_say_how_many_fixtures_ahead_they_were_made()
    {
        var (game, path, _) = NewGame();

        game.Submit(new AssignStrip(Fixtures[0].Id, new StripId(4)));
        game.Submit(new AssignStrip(Fixtures[2].Id, new StripId(5)));

        var assigns = Lines(path).Where(l => l.GetProperty("event").GetString() == "command" && l.GetProperty("type").GetString() == "assign").ToList();
        Assert.Equal(0, assigns[0].GetProperty("fixturesAhead").GetInt32());
        Assert.Equal(2, assigns[1].GetProperty("fixturesAhead").GetInt32());
    }

    [Fact]
    public void Request_answers_say_who_asked_and_for_what()
    {
        var (game, path, _) = NewGame();
        while (game.View.Requests.Count == 0)
        {
            game.Advance();
        }
        var request = game.View.Requests[0];

        game.Submit(new AnswerRequest(request.Id, accept: false));

        var answer = Lines(path).Single(l => l.GetProperty("event").GetString() == "command" && l.GetProperty("type").GetString() == "answer");
        Assert.Equal(request.Stakeholder.ToString(), answer.GetProperty("stakeholder").GetString());
        Assert.Equal(request.Kind.ToString(), answer.GetProperty("kind").GetString());
        Assert.False(answer.GetProperty("accept").GetBoolean());
    }

    [Fact]
    public void The_loop_logs_sessions_screens_and_input_it_didnt_understand()
    {
        var (game, path, _) = NewGame();
        var console = new TestConsole();
        console.Profile.Width = 160;

        new GameLoop(console, game, new StringReader("x\nv\nh\nfoo\ns\n")).Run();

        var lines = Lines(path);
        Assert.Equal("session_start", lines.First(l => l.GetProperty("event").GetString()!.StartsWith("session")).GetProperty("event").GetString());
        Assert.Equal("session_end", lines[^1].GetProperty("event").GetString());
        var screens = lines.Where(l => l.GetProperty("event").GetString() == "screen").Select(l => l.GetProperty("name").GetString()).ToList();
        Assert.Equal(new[] { "status", "fixtures", "record", "help", "status" }, screens);
        Assert.Equal("foo", lines.Single(l => l.GetProperty("event").GetString() == "invalid").GetProperty("text").GetString());
    }

    [Fact]
    public void The_playtest_folder_explains_what_is_recorded_the_first_time()
    {
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"groundsman-consent-{Guid.NewGuid():N}");
        var folder = new PlaytestFolder(root);

        Assert.True(folder.EnsureReadme());
        Assert.False(folder.EnsureReadme());
        var readme = File.ReadAllText(System.IO.Path.Combine(root, PlaytestFolder.ReadmeName));
        Assert.Contains("telemetry.jsonl", readme);
        Assert.Contains("nothing is sent", readme, StringComparison.OrdinalIgnoreCase);
        Directory.Delete(root, recursive: true);
    }
}
