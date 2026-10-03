using Groundsman.Cli;
using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;
using Spectre.Console.Testing;

namespace Groundsman.Tests.Cli;

public class FastForwardTests
{
    private static readonly Fixture[] Fixtures =
    {
        new Fixture(new DateTime(2027, 5, 20), TestFormats.FourDay, null, TestTeams.Opponent, televised: true),
        new Fixture(new DateTime(2027, 6, 12), TestFormats.OneDay, null, TestTeams.Opponent),
    };

    private static Game NewGame(double requests = 1, GameTime? start = null) =>
        new Game(new GameSetup(TestContent.WithStakeholders(TestStakeholders.With(requests, requests)), start ?? new GameTime(2027, 4, 20, 7), Fixtures, 1));

    [Fact]
    public void A_quiet_turn_doesnt_stop_it()
    {
        var game = NewGame(requests: 0, start: new GameTime(2027, 4, 1, 7));
        game.Submit(new AssignStrip(Fixtures[0].Id, new StripId(6)));
        game.Advance();

        Assert.Null(FastForward.StopReason(game.View));
    }

    [Fact]
    public void A_message_a_match_day_or_the_end_of_the_season_stops_it()
    {
        var game = NewGame();
        while (!game.View.Notices.Any())
        {
            game.Advance();
        }
        Assert.NotNull(FastForward.StopReason(game.View));

        while (game.View.Interval == null)
        {
            game.Advance();
        }
        Assert.Contains("match starts", FastForward.StopReason(game.View));
        game.Advance();
        Assert.Null(game.View.Notices.Any() ? null : FastForward.StopReason(game.View));
        while (game.View.Interval?.CanFill != true)
        {
            game.Advance();
        }
        Assert.Contains("close of play", FastForward.StopReason(game.View));

        while (game.View.Review == null)
        {
            game.Advance();
        }
        Assert.NotNull(FastForward.StopReason(game.View));
    }

    [Fact]
    public void A_fixture_about_to_lock_without_a_strip_stops_it()
    {
        var game = NewGame(requests: 0, start: new GameTime(2027, 5, 9, 7));

        Assert.Equal(new DateTime(2027, 5, 10), game.View.Fixtures[0].LocksOn);
        Assert.Contains("strip", FastForward.StopReason(game.View));
    }

    [Fact]
    public void Likely_rain_on_an_uncovered_strip_in_its_build_up_stops_it()
    {
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var game = new Game(new GameSetup(TestContent.WithStakeholders(TestStakeholders.With(0, 0)), new GameTime(2027, 5, 12, 7), new[] { new Fixture(new DateTime(2027, 5, 20), TestFormats.FourDay, new StripId(6), TestTeams.Opponent) }, seed));
            game.Advance();
            var view = game.View;
            var rainy = view.Forecast.Take(2).Any(d => d.ChanceOfRain >= FastForward.RainChance);
            var reason = FastForward.StopReason(view);

            Assert.Equal(rainy && !view.Notices.Any() && view.Interval == null, reason?.Contains("rain") == true);
            Assert.Null(view.Notices.Any() ? null : FastForward.StopReason(view, rainWarnedOn: view.Now.Date));
        }
    }

    private sealed class Watched : IGame
    {
        private readonly Game _inner;
        private readonly List<string> _events;

        public Watched(Game inner, List<string> events)
        {
            _inner = inner;
            _events = events;
        }

        public DateTime? RainWarnedOn { get; set; }

        public GameView View => _inner.View;
        public CommandResult Submit(IGameCommand command) => _inner.Submit(command);

        public AdvanceResult Advance()
        {
            var result = _inner.Advance();
            var reason = FastForward.StopReason(_inner.View, RainWarnedOn);
            if (reason?.StartsWith("rain") == true)
            {
                RainWarnedOn = _inner.View.Now.Date;
            }
            _events.Add(reason == null ? "quiet" : "stop");
            return result;
        }
    }

    private sealed class WatchedInput : TextReader
    {
        private readonly List<string> _events;
        private int _left;

        public WatchedInput(List<string> events, int lines)
        {
            _events = events;
            _left = lines;
        }

        public override string? ReadLine()
        {
            _events.Add("input");
            return _left-- > 0 ? "ff" : null;
        }
    }

    [Fact]
    public void Over_a_season_it_never_skips_a_turn_that_needs_the_player()
    {
        var events = new List<string>();
        var console = new TestConsole();
        console.Profile.Width = 160;

        new GameLoop(console, new Watched(NewGame(start: new GameTime(2027, 4, 1, 7)), events), new WatchedInput(events, 300)).Run();

        Assert.Contains("stop", events);
        for (var i = 0; i < events.Count - 1; i++)
        {
            if (events[i] == "stop")
            {
                Assert.Equal("input", events[i + 1]);
            }
        }
        Assert.Contains("Fast-forwarded", console.Output);
    }

    [Fact]
    public void It_logs_how_far_it_went_and_why_it_stopped()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"groundsman-ff-{Guid.NewGuid():N}");
        var telemetry = new Telemetry(Path.Combine(folder, Telemetry.FileName));
        var game = RecordingGame.Start(TestContent.WithStakeholders(TestStakeholders.With(1, 1)), Fixtures, new GameTime(2027, 4, 1, 7), 1, "hash", telemetry: telemetry);

        new GameLoop(new TestConsole(), game, new StringReader("ff\n")).Run();

        var lines = File.ReadAllLines(Path.Combine(folder, Telemetry.FileName));
        var summary = Assert.Single(lines, l => l.Contains("\"event\":\"fast_forward\""));
        Assert.Contains("\"turns\":", summary);
        Assert.Contains("\"stopped\":", summary);
        Assert.Contains(lines, l => l.Contains("\"event\":\"advance\"") && l.Contains("\"ff\":true"));
        Directory.Delete(folder, recursive: true);
    }
}
