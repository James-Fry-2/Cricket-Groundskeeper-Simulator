using Groundsman.Core.Content;
using Groundsman.Core.Grass;
using Groundsman.Core.Strips;
using Groundsman.Core.Weather;

namespace Groundsman.Tests.Grass;

public class GrassModelTests
{
    private static readonly GrassModel Model = new GrassModel(TestGrass.Settings);

    private static StripState Strip(double subsurface = 28) =>
        new StripState(new StripId(1), TestLoams.Standard, 24, subsurface, TestGrass.Settings);

    private static HourWeather At(double temperature) => new HourWeather(rainMm: 0, temperature: temperature, windKph: 10, sunshine: 0.5);

    private static void RunDays(StripState strip, double temperature, int days)
    {
        for (var hour = 0; hour < 24 * days; hour++)
        {
            Model.RunHour(strip, At(temperature));
        }
    }

    [Fact]
    public void Strips_start_with_the_content_grass()
    {
        var strip = Strip();

        Assert.Equal(85, strip.GrassCover);
        Assert.Equal(15, strip.GrassHeightMm);
        Assert.Equal(60, strip.RootDepthMm);
    }

    [Fact]
    public void Grass_grows_about_the_content_rate_a_day_in_ideal_conditions()
    {
        var strip = Strip(subsurface: TestLoams.Standard.FieldCapacity);

        RunDays(strip, TestGrass.Settings.OptimumTemperature, 5);

        Assert.Equal(15 + 5 * TestGrass.Settings.HeightPerDayMm, strip.GrassHeightMm, 6);
    }

    [Fact]
    public void Nothing_grows_in_the_cold()
    {
        var strip = Strip();

        RunDays(strip, TestGrass.Settings.MinTemperature - 1, 5);

        Assert.Equal(15, strip.GrassHeightMm);
    }

    [Fact]
    public void Growth_slows_away_from_the_best_temperature_and_in_dry_soil()
    {
        double GrowthIn5Days(double temperature, double subsurface)
        {
            var strip = Strip(subsurface);
            RunDays(strip, temperature, 5);
            return strip.GrassHeightMm - 15;
        }

        var ideal = GrowthIn5Days(18, TestLoams.Standard.FieldCapacity);
        Assert.True(GrowthIn5Days(10, TestLoams.Standard.FieldCapacity) < ideal);
        Assert.True(GrowthIn5Days(27, TestLoams.Standard.FieldCapacity) < ideal);
        Assert.True(GrowthIn5Days(18, 12) < ideal);
    }

    [Fact]
    public void Cover_thickens_towards_its_ceiling_but_never_past_it()
    {
        var strip = Strip(subsurface: TestLoams.Standard.FieldCapacity);

        RunDays(strip, 18, 60);

        Assert.Equal(TestGrass.Settings.MaxCover, strip.GrassCover, 6);
    }

    [Fact]
    public void Drought_thins_the_cover()
    {
        var strip = Strip(subsurface: TestLoams.Standard.AirDry + 0.5);

        RunDays(strip, 18, 4);

        Assert.True(strip.GrassCover < 85);
    }

    [Fact]
    public void Roots_deepen_up_to_their_limit()
    {
        var strip = Strip(subsurface: TestLoams.Standard.FieldCapacity);

        RunDays(strip, 18, 10);
        Assert.True(strip.RootDepthMm > 60);

        RunDays(strip, 18, 60);
        Assert.Equal(TestGrass.Settings.MaxRootDepthMm, strip.RootDepthMm, 6);
    }

    [Fact]
    public void A_gentle_cut_sets_the_height_without_harm()
    {
        var strip = Strip();

        Model.Mow(strip, 12);

        Assert.Equal(12, strip.GrassHeightMm);
        Assert.Equal(85, strip.GrassCover);
    }

    [Fact]
    public void Taking_off_more_than_a_third_at_once_scalps_the_strip()
    {
        var strip = Strip();

        Model.Mow(strip, 5);

        var allowed = 15 * TestGrass.Settings.ScalpShare;
        var expectedLoss = (15 - 5 - allowed) * TestGrass.Settings.ScalpCoverLossPerMm;
        Assert.Equal(5, strip.GrassHeightMm);
        Assert.Equal(85 - expectedLoss, strip.GrassCover, 6);
    }

    [Fact]
    public void Mowing_above_the_grass_cuts_nothing()
    {
        var strip = Strip();

        Model.Mow(strip, 20);

        Assert.Equal(15, strip.GrassHeightMm);
        Assert.Equal(85, strip.GrassCover);
    }
}
