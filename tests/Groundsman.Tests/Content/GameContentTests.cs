using Groundsman.Core.Content;
using Groundsman.Core.Strips;

namespace Groundsman.Tests.Content;

public class GameContentTests
{
    private static GameContent With(GroundSettings ground) => new GameContent(
        TestCalendar.Settings, TestClimate.Settings, ground, TestLoams.All, TestMoisture.Settings, TestContent.Covers, TestContent.Tasks, TestStaff.Settings, TestContent.Readings, TestForecast.Settings, TestFormats.All, TestTeams.All, TestTeams.Home.Id, TestGrass.Settings, TestRolling.Rollers, TestRolling.Compaction, TestPitch.Settings, TestWear.Settings, TestMatch.Settings, TestCommentary.Settings, TestRating.Settings, TestStakeholders.Settings);

    [Fact]
    public void Rejects_a_strip_naming_an_unknown_loam()
    {
        var ground = new GroundSettings("Test", new[] { "A", "B" }, new[] { new StripSettings(new StripId(1), "mystery", 20, 25) });

        var error = Assert.Throws<ContentException>(() => With(ground));

        Assert.Contains("mystery", error.Message);
    }

    [Fact]
    public void Rejects_starting_moisture_above_the_loams_saturation()
    {
        var ground = new GroundSettings("Test", new[] { "A", "B" }, new[] { new StripSettings(new StripId(1), TestLoams.Standard.Id, TestLoams.Standard.Saturation + 1, 25) });

        var error = Assert.Throws<ContentException>(() => With(ground));

        Assert.Contains("saturation", error.Message);
    }

    [Fact]
    public void Finds_a_loam_by_id()
    {
        Assert.Same(TestLoams.Heavy, TestContent.Content.Loam(TestLoams.Heavy.Id));
    }

    [Fact]
    public void Rejects_captain_characters_for_a_format_that_doesnt_exist()
    {
        var stakeholders = new StakeholderSettings(
            50, 5, 0.6,
            new Dictionary<string, IReadOnlyDictionary<Groundsman.Core.Pressures.RequestKind, double>> { ["hundred"] = new Dictionary<Groundsman.Core.Pressures.RequestKind, double> { [Groundsman.Core.Pressures.RequestKind.Flat] = 1 } },
            0.3, TestStakeholders.Settings.CaptainAnswers, TestStakeholders.Settings.BoardAnswers,
            3, -3, 4, -6, -4, 3, -6, -10, 6, 2, -8, -20, 5, 4, 5.5, 8, 3, 55, 75, 40, 0.01, 0.03, 0.06);

        var error = Assert.Throws<ContentException>(() => new GameContent(
            TestCalendar.Settings, TestClimate.Settings, TestGround.Settings, TestLoams.All, TestMoisture.Settings, TestContent.Covers, TestContent.Tasks, TestStaff.Settings, TestContent.Readings, TestForecast.Settings, TestFormats.All, TestTeams.All, TestTeams.Home.Id, TestGrass.Settings, TestRolling.Rollers, TestRolling.Compaction, TestPitch.Settings, TestWear.Settings, TestMatch.Settings, TestCommentary.Settings, TestRating.Settings, stakeholders));

        Assert.Contains("hundred", error.Message);
    }
}
