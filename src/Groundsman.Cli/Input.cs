using Groundsman.Core.Readings;
using Groundsman.Core.Staff;
using Groundsman.Core.Strips;

namespace Groundsman.Cli;

public abstract record Input;

public sealed record AdvanceInput : Input;

public sealed record QuitInput : Input;

public sealed record HelpInput : Input;

public sealed record StatusInput : Input;

public sealed record RecordInput : Input;

/// <summary>Reads one strip, or every strip when <see cref="Strip"/> is null.</summary>
/// <remarks>On every strip job, <c>By</c> names who does it; null means the player.</remarks>
public sealed record ReadInput(StripId? Strip, StaffId? By = null, ReadingSource Tool = ReadingSource.MoistureProbe) : Input;

public sealed record WaterInput(StripId Strip, StaffId? By = null) : Input;

/// <summary>Mows one strip, or every strip when <see cref="Strip"/> is null.</summary>
public sealed record MowInput(StripId? Strip, double HeightMm, StaffId? By = null) : Input;

/// <summary>Rolls one strip, or every strip when <see cref="Strip"/> is null.</summary>
public sealed record RollInput(StripId? Strip, string RollerId, double Minutes, StaffId? By = null) : Input;

public sealed record RepairInput(StripId Strip, StaffId? By = null) : Input;

public sealed record CleanInput(StripId Strip, StaffId? By = null) : Input;

public sealed record FillInput(StripId Strip, StaffId? By = null) : Input;

public sealed record CoverInput(StripId Strip, StaffId? By = null) : Input;

public sealed record UncoverInput(StripId Strip, StaffId? By = null) : Input;

public sealed record InvalidInput(string Message) : Input;
