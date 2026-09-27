using Groundsman.Core.Content;
using Groundsman.Core.Moisture;
using Groundsman.Core.Randomness;
using Groundsman.Core.Strips;
using Groundsman.Core.Weather;

namespace Groundsman.Tests.Moisture;

public class MoistureModelTests
{
    private static readonly MoistureModel Model = new MoistureModel(TestMoisture.Settings);
    private static readonly HourWeather Still = new HourWeather(rainMm: 0, temperature: 0, windKph: 0, sunshine: 0);
    private static readonly HourWeather HotSunny = new HourWeather(rainMm: 0, temperature: 25, windKph: 15, sunshine: 1);

    private static StripState Strip(double surface, double subsurface, LoamSettings? loam = null) =>
        new StripState(new StripId(1), loam ?? TestLoams.Standard, surface, subsurface);

    private static double WaterMm(StripState strip) =>
        strip.SurfaceMoisture / 100 * TestMoisture.Settings.SurfaceDepthMm +
        strip.SubsurfaceMoisture / 100 * TestMoisture.Settings.SubsurfaceDepthMm;

    [Fact]
    public void Moisture_stays_between_air_dry_and_saturation_whatever_the_weather()
    {
        var random = new RandomStreams(3).Get(RandomStream.Events);
        foreach (var loam in TestLoams.All)
        {
            var strip = Strip(25, 30, loam);
            for (var hour = 0; hour < 20_000; hour++)
            {
                var weather = new HourWeather(
                    rainMm: random.Chance(0.2) ? random.NextDouble(0, 30) : 0,
                    temperature: random.NextDouble(-5, 35),
                    windKph: random.NextDouble(0, 60),
                    sunshine: random.NextDouble());
                Model.RunHour(strip, weather, covered: random.Chance(0.3), wateringMm: random.Chance(0.05) ? 10 : 0);

                Assert.InRange(strip.SurfaceMoisture, loam.AirDry, loam.Saturation);
                Assert.InRange(strip.SubsurfaceMoisture, 0, loam.Saturation);
            }
        }
    }

    [Fact]
    public void A_covered_strip_takes_no_rain()
    {
        var covered = Strip(22, 22);
        var open = Strip(22, 22);
        var downpour = new HourWeather(rainMm: 10, temperature: 0, windKph: 0, sunshine: 0);

        Model.RunHour(covered, downpour, covered: true, wateringMm: 0);
        Model.RunHour(open, downpour, covered: false, wateringMm: 0);

        Assert.Equal(22, covered.SurfaceMoisture, 9);
        Assert.Equal(22, covered.SubsurfaceMoisture, 9);
        Assert.True(open.SurfaceMoisture > 22);
    }

    [Fact]
    public void Rain_that_overfills_the_surface_soaks_into_the_subsurface()
    {
        var strip = Strip(35, 20);

        Model.RunHour(strip, new HourWeather(rainMm: 5, temperature: 0, windKph: 0, sunshine: 0), covered: false, wateringMm: 0);

        Assert.True(strip.SubsurfaceMoisture > 20);
        Assert.True(strip.SurfaceMoisture <= TestLoams.Standard.Saturation);
    }

    [Fact]
    public void Rain_beyond_what_both_layers_hold_runs_off()
    {
        var strip = Strip(TestLoams.Standard.Saturation, TestLoams.Standard.Saturation);

        Model.RunHour(strip, new HourWeather(rainMm: 30, temperature: 0, windKph: 0, sunshine: 0), covered: false, wateringMm: 0);

        Assert.True(strip.SurfaceMoisture <= TestLoams.Standard.Saturation);
        Assert.True(strip.SubsurfaceMoisture <= TestLoams.Standard.Saturation);
    }

    [Fact]
    public void Without_rain_a_warm_sunny_strip_dries()
    {
        var strip = Strip(28, 28);
        var surfaces = new List<double>();

        for (var hour = 0; hour < 12; hour++)
        {
            Model.RunHour(strip, HotSunny, covered: false, wateringMm: 0);
            surfaces.Add(strip.SurfaceMoisture);
        }

        Assert.All(surfaces.Zip(surfaces.Skip(1)), pair => Assert.True(pair.Second < pair.First));
    }

    [Fact]
    public void Sun_heat_and_wind_each_speed_drying()
    {
        double DryingOver6Hours(HourWeather weather)
        {
            var strip = Strip(28, 28);
            for (var hour = 0; hour < 6; hour++)
            {
                Model.RunHour(strip, weather, covered: false, wateringMm: 0);
            }
            return 28 - strip.SurfaceMoisture;
        }

        var baseline = DryingOver6Hours(new HourWeather(0, temperature: 15, windKph: 5, sunshine: 0));

        Assert.True(DryingOver6Hours(new HourWeather(0, temperature: 15, windKph: 5, sunshine: 1)) > baseline);
        Assert.True(DryingOver6Hours(new HourWeather(0, temperature: 25, windKph: 5, sunshine: 0)) > baseline);
        Assert.True(DryingOver6Hours(new HourWeather(0, temperature: 15, windKph: 30, sunshine: 0)) > baseline);
    }

    [Fact]
    public void A_surface_at_air_dry_gives_up_no_more_water_to_the_air()
    {
        var strip = Strip(TestLoams.Standard.AirDry, TestLoams.Standard.AirDry);

        Model.RunHour(strip, HotSunny, covered: false, wateringMm: 0);

        Assert.Equal(TestLoams.Standard.AirDry, strip.SurfaceMoisture, 9);
    }

    [Fact]
    public void Watering_reaches_the_subsurface_over_about_a_day()
    {
        var strip = Strip(24, 22);
        var subsurface = new List<double> { strip.SubsurfaceMoisture };

        Model.RunHour(strip, Still, covered: false, wateringMm: 4);
        subsurface.Add(strip.SubsurfaceMoisture);
        for (var hour = 1; hour < 24; hour++)
        {
            Model.RunHour(strip, Still, covered: false, wateringMm: 0);
            subsurface.Add(strip.SubsurfaceMoisture);
        }

        var totalGain = subsurface[^1] - subsurface[0];
        Assert.True(subsurface[1] - subsurface[0] < totalGain / 4, "Most of the water should arrive after the first hour");
        Assert.True(totalGain > 1.5, $"Subsurface gained only {totalGain:0.00} points in a day");
    }

    [Fact]
    public void Heavier_clay_drains_more_slowly()
    {
        double ExcessLeftAfter12Hours(LoamSettings loam)
        {
            var strip = Strip(loam.Saturation, loam.FieldCapacity - 5, loam);
            for (var hour = 0; hour < 12; hour++)
            {
                Model.RunHour(strip, Still, covered: false, wateringMm: 0);
            }
            return (strip.SurfaceMoisture - loam.FieldCapacity) / (loam.Saturation - loam.FieldCapacity);
        }

        Assert.True(ExcessLeftAfter12Hours(TestLoams.Heavy) > ExcessLeftAfter12Hours(TestLoams.Standard));
    }

    [Fact]
    public void A_dry_surface_draws_water_up_from_below()
    {
        var strip = Strip(12, 28);

        Model.RunHour(strip, Still, covered: false, wateringMm: 0);

        Assert.True(strip.SurfaceMoisture > 12);
        Assert.True(strip.SubsurfaceMoisture < 28);
    }

    [Fact]
    public void Moving_water_between_layers_conserves_it()
    {
        var strip = Strip(35, 20);
        var before = WaterMm(strip);

        Model.RunHour(strip, Still, covered: false, wateringMm: 0);

        Assert.Equal(before, WaterMm(strip), 9);
    }
}
