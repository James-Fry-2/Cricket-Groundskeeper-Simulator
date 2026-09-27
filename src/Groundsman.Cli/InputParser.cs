using Groundsman.Core.Staff;
using Groundsman.Core.Strips;

namespace Groundsman.Cli;

public static class InputParser
{
    public static Input Parse(string text)
    {
        var words = text.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return new AdvanceInput();
        }

        if (words.Length > 3)
        {
            return Invalid(text);
        }
        StaffId? by = words.Length == 3 ? new StaffId(words[2]) : null;

        switch (words[0], words.Length)
        {
            case ("a" or "advance", 1):
                return new AdvanceInput();
            case ("q" or "quit", 1):
                return new QuitInput();
            case ("h" or "help" or "?", 1):
                return new HelpInput();
            case ("s" or "status", 1):
                return new StatusInput();
            case ("r" or "read", >= 2) when words[1] == "all":
                return new ReadInput(null, by);
            case ("r" or "read", >= 2) when TryStrip(words[1], out var strip):
                return new ReadInput(strip, by);
            case ("w" or "water", >= 2) when TryStrip(words[1], out var strip):
                return new WaterInput(strip, by);
            case ("c" or "cover", >= 2) when TryStrip(words[1], out var strip):
                return new CoverInput(strip, by);
            case ("u" or "uncover", >= 2) when TryStrip(words[1], out var strip):
                return new UncoverInput(strip, by);
            default:
                return Invalid(text);
        }
    }

    private static InvalidInput Invalid(string text) => new InvalidInput($"Didn't understand \"{text.Trim()}\". Type h for help.");

    private static bool TryStrip(string word, out StripId strip)
    {
        var ok = int.TryParse(word, out var number);
        strip = new StripId(number);
        return ok;
    }
}
