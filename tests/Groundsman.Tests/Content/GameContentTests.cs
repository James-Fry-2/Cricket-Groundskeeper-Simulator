using Groundsman.Core.Content;
using Groundsman.Core.Strips;

namespace Groundsman.Tests.Content;

public class GameContentTests
{
    private static GameContent With(GroundSettings ground) => new GameContent(
        TestCalendar.Settings, TestClimate.Settings, ground, TestLoams.All, TestMoisture.Settings, TestContent.Covers, TestContent.Tasks, TestStaff.Settings, TestContent.Readings, TestForecast.Settings, TestFormats.All, TestTeams.All, TestTeams.Home.Id, TestGrass.Settings);

    [Fact]
    public void Rejects_a_strip_naming_an_unknown_loam()
    {
        var ground = new GroundSettings("Test", new[] { new StripSettings(new StripId(1), "mystery", 20, 25) });

        var error = Assert.Throws<ContentException>(() => With(ground));

        Assert.Contains("mystery", error.Message);
    }

    [Fact]
    public void Rejects_starting_moisture_above_the_loams_saturation()
    {
        var ground = new GroundSettings("Test", new[] { new StripSettings(new StripId(1), TestLoams.Standard.Id, TestLoams.Standard.Saturation + 1, 25) });

        var error = Assert.Throws<ContentException>(() => With(ground));

        Assert.Contains("saturation", error.Message);
    }

    [Fact]
    public void Finds_a_loam_by_id()
    {
        Assert.Same(TestLoams.Heavy, TestContent.Content.Loam(TestLoams.Heavy.Id));
    }
}
