using Groundsman.Core;
using Groundsman.Core.Simulation;
using Groundsman.Core.Time;
using Groundsman.Tests.Simulation;

namespace Groundsman.Tests;

public class GameTests
{
    private static readonly DateTime MatchDay = new DateTime(2027, 5, 20);

    private static Game NewGame(GameTime start, params IHourlySystem[] extraSystems) =>
        new Game(TestContent.Setup(start, MatchDay), extraSystems);

    [Fact]
    public void View_shows_the_start_time()
    {
        var game = NewGame(new GameTime(2027, 5, 10, 7));

        Assert.Equal(new GameTime(2027, 5, 10, 7), game.View.Now);
    }

    [Fact]
    public void Advance_runs_every_hour_up_to_the_next_decision_point()
    {
        var grass = new RecordingSystem(TickStep.Grass, new List<TickStep>());
        var game = NewGame(new GameTime(2027, 5, 10, 7), grass);

        var result = game.Advance();

        Assert.Equal(new GameTime(2027, 5, 10, 7), result.From);
        Assert.Equal(new GameTime(2027, 5, 11, 7), result.To);
        Assert.Equal(24, result.HoursRun);
        Assert.Equal(24, grass.Hours.Count);
        Assert.Equal(new GameTime(2027, 5, 10, 7), grass.Hours[0]);
        Assert.Equal(new GameTime(2027, 5, 11, 6), grass.Hours[23]);
        Assert.Equal(new GameTime(2027, 5, 11, 7), game.View.Now);
    }

    [Fact]
    public void Advance_follows_the_pace_rules_into_a_match()
    {
        var game = NewGame(new GameTime(2027, 5, 19, 13));

        Assert.Equal(new GameTime(2027, 5, 20, 8), game.Advance().To);
        Assert.Equal(new GameTime(2027, 5, 20, 13), game.Advance().To);
    }

    [Fact]
    public void Advancing_a_year_ticks_every_hour_exactly_once()
    {
        var grass = new RecordingSystem(TickStep.Grass, new List<TickStep>());
        var start = new GameTime(2027, 1, 1, 7);
        var end = new GameTime(2028, 1, 1, 7);
        var game = NewGame(start, grass);

        while (game.View.Now < end)
        {
            game.Advance();
        }

        Assert.Equal(start.HoursUntil(game.View.Now), grass.Hours.Count);
        for (var i = 0; i < grass.Hours.Count; i++)
        {
            Assert.Equal(start.AddHours(i), grass.Hours[i]);
        }
    }

    [Fact]
    public void Rejects_a_command_it_does_not_know()
    {
        var game = NewGame(new GameTime(2027, 5, 10, 7));

        var result = game.Submit(new UnknownCommand());

        Assert.False(result.Accepted);
    }

    private sealed class UnknownCommand : IGameCommand
    {
    }
}
