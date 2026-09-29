using Groundsman.Core.Simulation;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Simulation;

public class HourlyTickTests
{
    [Fact]
    public void Runs_systems_in_the_fixed_order_whatever_order_they_were_given_in()
    {
        var log = new List<TickStep>();
        var systems = new[]
        {
            new RecordingSystem(TickStep.WearAndRecovery, log),
            new RecordingSystem(TickStep.Moisture, log),
            new RecordingSystem(TickStep.Weather, log),
            new RecordingSystem(TickStep.Tasks, log),
            new RecordingSystem(TickStep.Covers, log),
            new RecordingSystem(TickStep.Grass, log),
            new RecordingSystem(TickStep.Match, log),
        };

        new HourlyTick(systems).RunHour(new GameTime(2027, 5, 1, 7));

        Assert.Equal(new[]
        {
            TickStep.Weather,
            TickStep.Covers,
            TickStep.Moisture,
            TickStep.Grass,
            TickStep.Tasks,
            TickStep.Match,
            TickStep.WearAndRecovery,
        }, log);
    }

    [Fact]
    public void Skips_steps_with_no_system()
    {
        var log = new List<TickStep>();

        new HourlyTick(new[] { new RecordingSystem(TickStep.Grass, log) }).RunHour(new GameTime(2027, 5, 1, 7));

        Assert.Equal(new[] { TickStep.Grass }, log);
    }

    [Fact]
    public void Rejects_two_systems_for_one_step()
    {
        var log = new List<TickStep>();

        Assert.Throws<ArgumentException>(() => new HourlyTick(new[]
        {
            new RecordingSystem(TickStep.Moisture, log),
            new RecordingSystem(TickStep.Moisture, log),
        }));
    }
}

internal sealed class RecordingSystem : IHourlySystem
{
    private readonly List<TickStep> _log;

    public RecordingSystem(TickStep step, List<TickStep> log)
    {
        Step = step;
        _log = log;
    }

    public TickStep Step { get; }

    public List<GameTime> Hours { get; } = new List<GameTime>();

    public void RunHour(GameTime hour)
    {
        _log.Add(Step);
        Hours.Add(hour);
    }
}
