using System.Reflection;

namespace Groundsman.Cli;

public static class GameInfo
{
    /// <summary>The build's version, stamped from git by scripts/publish.sh; "dev" for local builds.</summary>
    public static string Version { get; } =
        typeof(GameInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion is { } version && version != "1.0.0"
            ? version
            : "dev";
}
