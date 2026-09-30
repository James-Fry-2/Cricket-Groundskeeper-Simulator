using Groundsman.Core.Content;

namespace Groundsman.Tests;

internal static class TestCommentary
{
    public static CommentarySettings Settings { get; } = ContentParser.ParseCommentary(@"{
        ""events"": {
            ""seam"": { ""threshold"": 6, ""text"": ""SEAM for {bowling} against {batting}. {cause}."" },
            ""dead"": { ""threshold"": 3, ""text"": ""DEAD. {cause}."" },
            ""uneven"": { ""threshold"": 6, ""text"": ""UNEVEN. {cause}."" },
            ""keepingLow"": { ""threshold"": 3.5, ""text"": ""LOW. {cause}."" },
            ""turn"": { ""threshold"": 5, ""text"": ""TURN. {cause}."" },
            ""dust"": { ""threshold"": 0.4, ""text"": ""DUST. {cause}."" },
            ""cracks"": { ""threshold"": 0.3, ""text"": ""CRACKS. {cause}."" },
            ""footholes"": { ""threshold"": 0.3, ""text"": ""FOOTHOLES at the {end}. {cause}."" },
            ""danger"": { ""threshold"": 4, ""text"": ""DANGER. {cause}."" },
            ""collapse"": { ""threshold"": 4, ""text"": ""COLLAPSE {batting} lose {wickets}. {cause}."" },
            ""true"": { ""threshold"": 8.5, ""text"": ""TRUE. {cause}."" }
        },
        ""causes"": {
            ""grass"": ""cause:grass {grassMm}mm"",
            ""damp"": ""cause:damp"",
            ""wet"": ""cause:wet"",
            ""loose"": ""cause:loose"",
            ""longGrass"": ""cause:longGrass"",
            ""structureDamage"": ""cause:structureDamage"",
            ""cracks"": ""cause:cracks"",
            ""footholes"": ""cause:footholes"",
            ""surfaceWear"": ""cause:surfaceWear"",
            ""dry"": ""cause:dry"",
            ""rough"": ""cause:rough"",
            ""hard"": ""cause:hard"",
            ""lastingWear"": ""cause:lastingWear"",
            ""thinEnds"": ""cause:thinEnds""
        },
        ""rain"": ""RAIN"",
        ""session"": ""{break}: {score}""
    }");
}
