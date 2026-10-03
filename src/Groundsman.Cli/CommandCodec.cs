using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Readings;
using Groundsman.Core.Staff;
using Groundsman.Core.Strips;

namespace Groundsman.Cli;

/// <summary>One command as a save holds it, with the turn it was given in.</summary>
public sealed record SavedCommand
{
    public int Turn { get; init; }
    public string Type { get; init; } = "";
    public int? Strip { get; init; }
    public string? By { get; init; }
    public string? Tool { get; init; }
    public double? HeightMm { get; init; }
    public string? Roller { get; init; }
    public double? Minutes { get; init; }
    public string? Fixture { get; init; }
    public string? Request { get; init; }
    public bool? Accept { get; init; }
}

/// <summary>Turns commands into saved records and back. Every command type must be covered.</summary>
public static class CommandCodec
{
    public static SavedCommand Encode(IGameCommand command, int turn) => command switch
    {
        WaterStrip c => Strip("water", turn, c.Strip, c.By),
        TakeReading c => Strip("read", turn, c.Strip, c.By) with { Tool = c.Tool.ToString() },
        CoverStrip c => Strip("cover", turn, c.Strip, c.By),
        UncoverStrip c => Strip("uncover", turn, c.Strip, c.By),
        MowStrip c => Strip("mow", turn, c.Strip, c.By) with { HeightMm = c.HeightMm },
        RollStrip c => Strip("roll", turn, c.Strip, c.By) with { Roller = c.RollerId, Minutes = c.Minutes },
        RepairEnds c => Strip("repair", turn, c.Strip, c.By),
        CleanFootholes c => Strip("clean", turn, c.Strip, c.By),
        FillFootholes c => Strip("fill", turn, c.Strip, c.By),
        AssignStrip c => new SavedCommand { Turn = turn, Type = "assign", Fixture = c.FixtureId, Strip = c.Strip.Number },
        AnswerRequest c => new SavedCommand { Turn = turn, Type = "answer", Request = c.RequestId, Accept = c.Accept },
        _ => throw new ArgumentException($"Saves don't know the command {command.GetType().Name}."),
    };

    public static IGameCommand Decode(SavedCommand saved)
    {
        var strip = new StripId(saved.Strip ?? 0);
        StaffId? by = saved.By == null ? null : new StaffId(saved.By);
        return saved.Type switch
        {
            "water" => new WaterStrip(strip, by),
            "read" => new TakeReading(strip, by, Enum.Parse<ReadingSource>(saved.Tool ?? nameof(ReadingSource.MoistureProbe))),
            "cover" => new CoverStrip(strip, by),
            "uncover" => new UncoverStrip(strip, by),
            "mow" => new MowStrip(strip, saved.HeightMm ?? 0, by),
            "roll" => new RollStrip(strip, saved.Roller ?? "", saved.Minutes ?? 0, by),
            "repair" => new RepairEnds(strip, by),
            "clean" => new CleanFootholes(strip, by),
            "fill" => new FillFootholes(strip, by),
            "assign" => new AssignStrip(saved.Fixture ?? "", strip),
            "answer" => new AnswerRequest(saved.Request ?? "", saved.Accept ?? false),
            _ => throw new InvalidDataException($"The save holds an unknown command \"{saved.Type}\"."),
        };
    }

    private static SavedCommand Strip(string type, int turn, StripId strip, StaffId? by) =>
        new SavedCommand { Turn = turn, Type = type, Strip = strip.Number, By = by?.Value };
}
