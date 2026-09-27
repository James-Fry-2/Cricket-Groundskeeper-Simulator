using Groundsman.Core.Strips;

namespace Groundsman.Cli;

public abstract record Input;

public sealed record AdvanceInput : Input;

public sealed record QuitInput : Input;

public sealed record HelpInput : Input;

public sealed record StatusInput : Input;

/// <summary>Reads one strip, or every strip when <see cref="Strip"/> is null.</summary>
public sealed record ReadInput(StripId? Strip) : Input;

public sealed record WaterInput(StripId Strip) : Input;

public sealed record InvalidInput(string Message) : Input;
