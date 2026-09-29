using Groundsman.Core.Content;

namespace Groundsman.Tests;

internal static class TestRating
{
    public static RatingSettings Settings { get; } = ContentParser.ParseRating(@"{
        ""unfitBelow"": 3.0,
        ""abandonBelow"": 2.5,
        ""unsatisfactory"": {
            ""consistencyBelow"": 6.5, ""carryBelow"": 3.5, ""bowlersOversShare"": 0.5, ""bowlersRunsPerWicket"": 20,
            ""limitedParShare"": 0.5, ""lifelessRunsPerWicket"": 50, ""lifelessMovementBelow"": 3
        },
        ""veryGood"": { ""consistencyAtLeast"": 8.5, ""carryAtLeast"": 5.5, ""movementAtLeast"": 4 },
        ""demerits"": { ""unsatisfactory"": 1, ""unfit"": 3, ""windowYears"": 5, ""banAt"": 5 },
        ""reasons"": {
            ""dangerous"": ""R:dangerous {value} {cause}"",
            ""abandoned"": ""R:abandoned {cause}"",
            ""uneven"": ""R:uneven {value} {cause}"",
            ""dead"": ""R:dead {value} {cause}"",
            ""bowlers"": ""R:bowlers {value}"",
            ""limitedLow"": ""R:limitedLow"",
            ""lifeless"": ""R:lifeless"",
            ""consistent"": ""R:consistent {value}"",
            ""carry"": ""R:carry {value}"",
            ""movement"": ""R:movement"",
            ""flat"": ""R:flat""
        }
    }");
}
