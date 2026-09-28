using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;
using Groundsman.Harness;

namespace Groundsman.Tests.Harness;

public class SeasonTraceTests
{
    [Fact]
    public void Records_every_decision_point_up_to_the_end_with_truth_per_strip()
    {
        var season = new SeasonSettings(new DateTime(2027, 4, 1), new[] { new Fixture(new DateTime(2027, 4, 10), TestFormats.OneDay, new StripId(1), TestTeams.Opponent) });

        var csv = SeasonTrace.Run(TestContent.Content, season, seed: 4, end: new DateTime(2027, 4, 15));
        var lines = csv.TrimEnd().Split('\n');

        Assert.StartsWith("time,rain_24h_mm,temperature,s1_surface,s1_subsurface,s1_grass_mm,s1_cover,s1_hardness,s2_surface", lines[0]);
        Assert.Equal(3 + 5 * 12, lines[0].Split(',').Length);
        Assert.StartsWith("2027-04-01 07:00,", lines[1]);
        Assert.StartsWith("2027-04-15 07:00,", lines[^1]);
        Assert.Contains(lines, l => l.StartsWith("2027-04-10 13:00,"));
    }

    [Fact]
    public void The_same_seed_gives_the_same_trace()
    {
        var season = new SeasonSettings(new DateTime(2027, 4, 1), Array.Empty<Fixture>());

        Assert.Equal(
            SeasonTrace.Run(TestContent.Content, season, seed: 4, end: new DateTime(2027, 5, 1)),
            SeasonTrace.Run(TestContent.Content, season, seed: 4, end: new DateTime(2027, 5, 1)));
    }
}
