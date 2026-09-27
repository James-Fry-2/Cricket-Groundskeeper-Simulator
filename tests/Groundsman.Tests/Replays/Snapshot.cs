using System.Runtime.CompilerServices;

namespace Groundsman.Tests.Replays;

/// <summary>
/// Compares text against a file in tests/Snapshots. Set UPDATE_SNAPSHOTS=1 to rewrite the files
/// after an intended rule change, then review the diff before committing.
/// </summary>
internal static class Snapshot
{
    public static void Match(string name, string actual, [CallerFilePath] string callerPath = "")
    {
        var directory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(callerPath)!, "..", "Snapshots"));
        var path = Path.Combine(directory, name + ".json");
        actual = actual.Replace("\r\n", "\n");

        if (Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "1")
        {
            File.WriteAllText(path, actual);
            return;
        }
        if (!File.Exists(path))
        {
            File.WriteAllText(path, actual);
            Assert.Fail($"No snapshot existed, so one was written to {path}. Review it and run the tests again.");
        }

        var expected = File.ReadAllText(path).Replace("\r\n", "\n");
        if (expected == actual)
        {
            return;
        }

        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');
        var line = 0;
        while (line < expectedLines.Length && line < actualLines.Length && expectedLines[line] == actualLines[line])
        {
            line++;
        }
        Assert.Fail(
            $"{name} differs from the snapshot at line {line + 1}.\n" +
            $"  expected: {(line < expectedLines.Length ? expectedLines[line] : "(end)")}\n" +
            $"  actual:   {(line < actualLines.Length ? actualLines[line] : "(end)")}\n" +
            "If the rule change was intended, rerun with UPDATE_SNAPSHOTS=1 and review the diff.");
    }
}
