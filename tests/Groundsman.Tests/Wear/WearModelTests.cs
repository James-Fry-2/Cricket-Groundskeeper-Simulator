using Groundsman.Core.Content;
using Groundsman.Core.Randomness;
using Groundsman.Core.Strips;
using Groundsman.Core.Wear;

namespace Groundsman.Tests.Wear;

public class WearModelTests
{
    private static readonly WearModel Model = new WearModel(TestWear.Settings, TestGrass.Settings);
    private static readonly AttackProfile Seam = new AttackProfile(seam: 0.7, leftArm: 0.2, heavyFooted: 0.5);

    private static StripState Strip(double surface = 22, double compaction = 0.75, double roots = 80, LoamSettings? loam = null)
    {
        var strip = new StripState(new StripId(1), loam ?? TestLoams.Standard, surface, 26, TestGrass.Settings);
        strip.Compaction = compaction;
        strip.RootDepthMm = roots;
        return strip;
    }

    private static StripState Played(double overs, AttackProfile attack, StripState? strip = null)
    {
        strip ??= Strip();
        Model.ApplyOvers(strip, overs, attack);
        return strip;
    }

    [Fact]
    public void Seamers_dig_footholes_and_heavy_footed_ones_dig_deeper()
    {
        var light = Played(96, new AttackProfile(0.7, 0.2, heavyFooted: 0));
        var heavy = Played(96, new AttackProfile(0.7, 0.2, heavyFooted: 1));
        var spin = Played(96, new AttackProfile(0.2, 0.2, heavyFooted: 1));

        Assert.True(light.Footholes > 0);
        Assert.True(heavy.Footholes > light.Footholes);
        Assert.True(spin.Footholes < heavy.Footholes);
    }

    [Fact]
    public void Left_armers_make_more_useful_rough()
    {
        Assert.True(Played(96, new AttackProfile(0.7, 0.8, 0.5)).Rough > Played(96, new AttackProfile(0.7, 0, 0.5)).Rough);
    }

    [Fact]
    public void Play_in_the_wet_does_more_damage_and_a_dusty_surface_wears_faster()
    {
        var moist = Played(96, Seam, Strip(surface: 22));
        var wet = Played(96, Seam, Strip(surface: TestLoams.Standard.RollingWindowMax + 3));
        var dusty = Played(96, Seam, Strip(surface: TestLoams.Standard.RollingWindowMin - 5));

        Assert.True(wet.Footholes > moist.Footholes * 1.5);
        Assert.True(dusty.SurfaceWear > moist.SurfaceWear);
    }

    [Fact]
    public void A_well_rolled_well_rooted_clay_strip_resists_wear()
    {
        var prepared = Played(96, Seam, Strip(compaction: 0.8, roots: 90, loam: TestLoams.Heavy));
        var loose = Played(96, Seam, Strip(compaction: 0.5, roots: 40));

        Assert.True(prepared.Footholes < loose.Footholes);
        Assert.True(Model.Resistance(Strip(compaction: 0.8)) > Model.Resistance(Strip(compaction: 0.5)));
    }

    [Fact]
    public void Footholes_build_over_a_four_day_match_and_more_on_an_unprepared_strip()
    {
        var prepared = Strip();
        var unprepared = Strip(compaction: 0.55, roots: 60);

        var afterDayOne = Played(96, Seam, prepared).Footholes;
        Played(264, Seam, prepared);
        Played(360, Seam, unprepared);

        Assert.InRange(afterDayOne, 0.01, 0.15);
        Assert.InRange(prepared.Footholes, 0.1, 0.5);
        Assert.True(unprepared.Footholes > prepared.Footholes);
    }

    [Fact]
    public void Play_scuffs_the_grass()
    {
        var strip = Played(96, Seam);

        Assert.True(strip.GrassCover < TestGrass.Settings.StartingCover);
    }

    [Fact]
    public void Wear_stays_between_zero_and_one()
    {
        var random = new RandomStreams(8).Get(RandomStream.Events);
        for (var i = 0; i < 200; i++)
        {
            var strip = Strip(surface: random.NextDouble(6, 40), compaction: random.NextDouble(), roots: random.NextDouble(0, 100));
            Model.ApplyOvers(strip, random.NextDouble(0, 2000), new AttackProfile(random.NextDouble(), random.NextDouble(), random.NextDouble()));

            Assert.InRange(strip.Footholes, 0, 1);
            Assert.InRange(strip.Rough, 0, 1);
            Assert.InRange(strip.SurfaceWear, 0, 1);
            Assert.InRange(strip.GrassCover, 0, 100);
        }
    }

    [Fact]
    public void Cracks_open_as_a_clay_surface_dries_and_more_on_heavier_clay()
    {
        var standard = Strip(surface: TestLoams.Standard.AirDry + 1);
        var heavy = Strip(surface: TestLoams.Heavy.AirDry + 1, loam: TestLoams.Heavy);

        for (var hour = 0; hour < 48; hour++)
        {
            Model.RunHour(standard, growth: 0);
            Model.RunHour(heavy, growth: 0);
        }

        Assert.True(standard.Cracks > 0);
        Assert.True(heavy.Cracks > standard.Cracks);
    }

    [Fact]
    public void Structure_damage_makes_a_strip_crack_faster_and_a_moist_one_doesnt_crack()
    {
        var sound = Strip(surface: TestLoams.Standard.AirDry + 1);
        var damaged = Strip(surface: TestLoams.Standard.AirDry + 1);
        damaged.StructureDamage = 0.5;
        var moist = Strip(surface: 22);

        for (var hour = 0; hour < 48; hour++)
        {
            Model.RunHour(sound, growth: 0);
            Model.RunHour(damaged, growth: 0);
            Model.RunHour(moist, growth: 0);
        }

        Assert.True(damaged.Cracks > sound.Cracks);
        Assert.Equal(0, moist.Cracks);
    }

    [Fact]
    public void Cracks_close_when_rain_swells_the_clay()
    {
        var strip = Strip(surface: TestLoams.Standard.FieldCapacity + 2);
        strip.Cracks = 0.5;

        for (var hour = 0; hour < 12; hour++)
        {
            Model.RunHour(strip, growth: 0);
        }

        Assert.True(strip.Cracks < 0.5);
        Assert.InRange(strip.Cracks, 0, 1);
    }

    [Fact]
    public void Wear_recovers_as_grass_grows_and_repaired_ends_recover_faster()
    {
        var left = Played(360, Seam, Strip(compaction: 0.55, roots: 60));
        var repaired = Played(360, Seam, Strip(compaction: 0.55, roots: 60));
        var worn = left.Footholes;

        Model.Repair(repaired);
        Assert.Equal(worn * (1 - TestWear.Settings.RepairFills), repaired.Footholes, 9);
        Assert.True(repaired.EndsRepaired);

        for (var hour = 0; hour < 24 * 3; hour++)
        {
            Model.RunHour(left, growth: 1);
            Model.RunHour(repaired, growth: 1);
        }

        Assert.Equal(worn - 3 * TestWear.Settings.RecoveryPerDay, left.Footholes, 6);
        Assert.True(repaired.Footholes < left.Footholes * (1 - TestWear.Settings.RepairFills));
    }

    [Fact]
    public void Nothing_recovers_when_grass_isnt_growing()
    {
        var strip = Played(96, Seam);
        var worn = strip.Footholes;

        for (var hour = 0; hour < 48; hour++)
        {
            Model.RunHour(strip, growth: 0);
        }

        Assert.Equal(worn, strip.Footholes);
    }

    [Fact]
    public void A_new_match_undoes_the_repaired_state()
    {
        var strip = Played(96, Seam);
        Model.Repair(strip);

        Played(10, Seam, strip);

        Assert.False(strip.EndsRepaired);
    }

    [Fact]
    public void Run_ups_wear_a_neighbours_ends_by_a_share_of_the_match_strips_footholes_and_more_when_its_wet()
    {
        var dry = Strip(surface: 20);
        var wet = Strip(surface: 35);

        Model.ApplyRunUps(dry, 0.1);
        Model.ApplyRunUps(wet, 0.1);

        Assert.Equal(0.1 * TestWear.Settings.NeighbourShare, dry.Footholes, 9);
        Assert.Equal(0.1 * TestWear.Settings.NeighbourShare * TestWear.Settings.WetPlay, wet.Footholes, 9);
        Assert.Equal(0, dry.Rough);
    }

    [Fact]
    public void Applying_overs_reports_the_footholes_dug()
    {
        var strip = Strip();

        var dug = Model.ApplyOvers(strip, 16, Seam);

        Assert.True(dug > 0);
        Assert.Equal(strip.Footholes, dug, 9);
    }
}
