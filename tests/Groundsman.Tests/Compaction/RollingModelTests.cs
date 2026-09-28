using Groundsman.Core.Compaction;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;

namespace Groundsman.Tests.Compaction;

public class RollingModelTests
{
    private static readonly RollingModel Model = new RollingModel(TestRolling.Compaction);

    private static StripState Strip(double surface, double compaction = 0.55, LoamSettings? loam = null)
    {
        var strip = new StripState(new StripId(1), loam ?? TestLoams.Standard, surface, 28, compaction: TestRolling.Compaction);
        strip.Compaction = compaction;
        return strip;
    }

    [Fact]
    public void Strips_start_with_the_content_compaction()
    {
        var strip = new StripState(new StripId(1), TestLoams.Standard, 22, 28, compaction: TestRolling.Compaction);

        Assert.Equal(0.55, strip.Compaction);
        Assert.Equal(0, strip.StructureDamage);
    }

    [Fact]
    public void Rolling_in_the_moisture_window_compacts_without_harm()
    {
        var strip = Strip(22);

        Model.Roll(strip, TestRolling.Medium, minutes: 30);

        Assert.Equal(0.55 + 0.08 * 0.5 * (1 - 0.55), strip.Compaction, 9);
        Assert.Equal(0, strip.StructureDamage);
    }

    [Fact]
    public void Heavier_rollers_and_longer_rolling_compact_more()
    {
        double Gain(RollerSettings roller, double minutes)
        {
            var strip = Strip(22);
            Model.Roll(strip, roller, minutes);
            return strip.Compaction - 0.55;
        }

        Assert.True(Gain(TestRolling.Medium, 30) > Gain(TestRolling.Light, 30));
        Assert.True(Gain(TestRolling.Heavy, 30) > Gain(TestRolling.Medium, 30));
        Assert.True(Gain(TestRolling.Light, 45) > Gain(TestRolling.Light, 30));
    }

    [Fact]
    public void Gains_shrink_as_the_strip_nears_full_compaction()
    {
        var loose = Strip(22, compaction: 0.4);
        var tight = Strip(22, compaction: 0.9);

        Model.Roll(loose, TestRolling.Medium, 30);
        Model.Roll(tight, TestRolling.Medium, 30);

        Assert.True(loose.Compaction - 0.4 > tight.Compaction - 0.9);
        Assert.True(tight.Compaction < 1);
    }

    [Fact]
    public void Rolling_too_dry_does_nothing()
    {
        var strip = Strip(TestLoams.Standard.RollingWindowMin - 1);

        Model.Roll(strip, TestRolling.Heavy, 30);

        Assert.Equal(0.55, strip.Compaction);
        Assert.Equal(0, strip.StructureDamage);
    }

    [Fact]
    public void Rolling_too_wet_compacts_but_damages_the_structure_and_heavier_rollers_do_more_harm()
    {
        double Damage(RollerSettings roller)
        {
            var strip = Strip(TestLoams.Standard.RollingWindowMax + 4);
            Model.Roll(strip, roller, 30);
            Assert.True(strip.Compaction > 0.55);
            return strip.StructureDamage;
        }

        Assert.True(Damage(TestRolling.Light) > 0);
        Assert.True(Damage(TestRolling.Heavy) > Damage(TestRolling.Light));
    }

    [Fact]
    public void Wetter_rolling_does_more_harm()
    {
        var damp = Strip(TestLoams.Standard.RollingWindowMax + 1);
        var sodden = Strip(TestLoams.Standard.RollingWindowMax + 8);

        Model.Roll(damp, TestRolling.Medium, 30);
        Model.Roll(sodden, TestRolling.Medium, 30);

        Assert.True(sodden.StructureDamage > damp.StructureDamage);
    }

    [Fact]
    public void Overusing_the_heavy_roller_on_a_tight_strip_does_harm_but_the_light_one_doesnt()
    {
        var heavy = Strip(22, compaction: 0.9);
        var light = Strip(22, compaction: 0.9);

        Model.Roll(heavy, TestRolling.Heavy, 30);
        Model.Roll(light, TestRolling.Light, 30);

        Assert.True(heavy.StructureDamage > 0);
        Assert.Equal(0, light.StructureDamage);
    }

    [Fact]
    public void Structure_damage_never_goes_past_one()
    {
        var strip = Strip(TestLoams.Standard.Saturation);

        for (var i = 0; i < 100; i++)
        {
            Model.Roll(strip, TestRolling.Heavy, 120);
        }

        Assert.InRange(strip.StructureDamage, 0, 1);
        Assert.InRange(strip.Compaction, 0, 1);
    }

    [Fact]
    public void Hardness_rises_with_compaction_dryness_and_clay()
    {
        var baseline = Model.Hardness(Strip(20, compaction: 0.6));

        Assert.True(Model.Hardness(Strip(20, compaction: 0.8)) > baseline);
        Assert.True(Model.Hardness(Strip(12, compaction: 0.6)) > baseline);
        Assert.True(Model.Hardness(Strip(20, compaction: 0.6, loam: TestLoams.Heavy)) > baseline);
        Assert.InRange(Model.Hardness(Strip(TestLoams.Standard.AirDry, compaction: 1, loam: TestLoams.Heavy)), 0, 1);
    }
}
