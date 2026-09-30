using Groundsman.Core.Compaction;
using Groundsman.Core.Content;
using Groundsman.Core.Pitch;
using Groundsman.Core.Randomness;
using Groundsman.Core.Strips;

namespace Groundsman.Tests.Pitch;

public class PitchModelTests
{
    private static readonly PitchModel Model = new PitchModel(TestPitch.Settings, new RollingModel(TestRolling.Compaction));

    /// <summary>A reasonably prepared strip to vary one driver at a time from.</summary>
    private static StripState Strip(
        double surface = 18,
        double subsurface = 26,
        double compaction = 0.7,
        double damage = 0,
        double cover = 80,
        double heightMm = 7,
        LoamSettings? loam = null,
        double cracks = 0,
        double footholes = 0,
        double rough = 0,
        double surfaceWear = 0)
    {
        var strip = new StripState(new StripId(1), loam ?? TestLoams.Standard, surface, subsurface);
        strip.Compaction = compaction;
        strip.StructureDamage = damage;
        strip.GrassCover = cover;
        strip.GrassHeightMm = heightMm;
        strip.Cracks = cracks;
        strip.Footholes = footholes;
        strip.Rough = rough;
        strip.SurfaceWear = surfaceWear;
        return strip;
    }

    private static PitchCharacteristics Of(StripState strip) => Model.Characterise(strip);

    [Fact]
    public void A_harder_drier_strip_plays_faster()
    {
        Assert.True(Of(Strip(compaction: 0.85)).Pace > Of(Strip()).Pace);
        Assert.True(Of(Strip(surface: 12)).Pace > Of(Strip()).Pace);
        Assert.True(Of(Strip(surface: 28)).Pace < Of(Strip()).Pace);
    }

    [Fact]
    public void Compaction_clay_and_moisture_at_depth_give_bounce()
    {
        Assert.True(Of(Strip(compaction: 0.85)).Bounce > Of(Strip()).Bounce);
        Assert.True(Of(Strip(loam: TestLoams.Heavy)).Bounce > Of(Strip()).Bounce);
        Assert.True(Of(Strip(subsurface: 14)).Bounce < Of(Strip()).Bounce);
    }

    [Fact]
    public void Damage_loose_soil_and_cracking_make_bounce_less_consistent()
    {
        var baseline = Of(Strip()).Consistency;

        Assert.True(Of(Strip(damage: 0.4)).Consistency < baseline);
        Assert.True(Of(Strip(compaction: 0.35)).Consistency < baseline);
        Assert.True(Of(Strip(cracks: 0.5)).Consistency < baseline);
        Assert.True(Of(Strip(footholes: 0.5)).Consistency < baseline);
        Assert.True(Of(Strip(surfaceWear: 0.5)).Consistency < baseline);
    }

    [Fact]
    public void A_well_prepared_strip_is_true()
    {
        Assert.True(Of(Strip()).Consistency >= 9);
    }

    [Fact]
    public void Long_grass_cushions_the_ball()
    {
        var mown = Of(Strip(heightMm: 8));
        var long_ = Of(Strip(heightMm: 18));

        Assert.True(long_.Pace < mown.Pace);
        Assert.True(long_.Bounce < mown.Bounce);
    }

    [Fact]
    public void An_unrolled_strip_bounces_unevenly()
    {
        Assert.True(Of(Strip(compaction: TestRolling.Compaction.StartingCompaction)).Consistency < Of(Strip()).Consistency - 0.5);
    }

    [Fact]
    public void Carry_follows_pace_and_bounce_together()
    {
        var pitch = Of(Strip());

        Assert.InRange(pitch.Carry, Math.Min(pitch.Pace, pitch.Bounce), Math.Max(pitch.Pace, pitch.Bounce));
        Assert.True(Of(Strip(compaction: 0.85)).Carry > pitch.Carry);
    }

    [Fact]
    public void Grass_and_a_damp_surface_help_the_seamers()
    {
        var baseline = Of(Strip()).Seam;

        Assert.True(Of(Strip(heightMm: 10)).Seam > baseline);
        Assert.True(Of(Strip(cover: 95)).Seam > baseline);
        Assert.True(Of(Strip(surface: 26)).Seam > baseline);
        Assert.True(Of(Strip(cover: 40, heightMm: 4)).Seam < baseline);
    }

    [Fact]
    public void A_dry_bare_surface_helps_the_spinners()
    {
        var baseline = Of(Strip()).Spin;

        Assert.True(Of(Strip(surface: 10)).Spin > baseline);
        Assert.True(Of(Strip(cover: 50, heightMm: 5)).Spin > baseline);
        Assert.True(Of(Strip(surface: 26)).Spin < baseline);
    }

    [Fact]
    public void Cracking_is_the_cracks_that_have_opened()
    {
        Assert.Equal(0, Of(Strip()).Cracking);
        Assert.Equal(6, Of(Strip(cracks: 0.6)).Cracking, 9);
    }

    [Fact]
    public void Rough_and_a_worn_surface_help_the_spinners()
    {
        var baseline = Of(Strip()).Spin;

        Assert.True(Of(Strip(rough: 0.5)).Spin > baseline);
        Assert.True(Of(Strip(surfaceWear: 0.5)).Spin > baseline);
        Assert.True(Of(Strip(cracks: 0.5)).Spin > baseline);
    }

    [Fact]
    public void A_damp_green_early_season_strip_seams_more_than_it_turns_and_a_dry_worn_one_the_reverse()
    {
        var green = Of(Strip(surface: 25, cover: 92, heightMm: 10));
        var dry = Of(Strip(surface: 9, cover: 55, heightMm: 5));

        Assert.True(green.Seam > green.Spin);
        Assert.True(dry.Spin > dry.Seam);
    }

    [Fact]
    public void Every_characteristic_stays_between_zero_and_ten()
    {
        var random = new RandomStreams(4).Get(RandomStream.Events);
        for (var i = 0; i < 5000; i++)
        {
            var loam = random.Chance(0.5) ? TestLoams.Standard : TestLoams.Heavy;
            var pitch = Of(Strip(
                surface: random.NextDouble(loam.AirDry, loam.Saturation),
                subsurface: random.NextDouble(loam.AirDry, loam.Saturation),
                compaction: random.NextDouble(),
                damage: random.NextDouble(),
                cover: random.NextDouble(0, 100),
                heightMm: random.NextDouble(0, 60),
                loam: loam,
                cracks: random.NextDouble(),
                footholes: random.NextDouble(),
                rough: random.NextDouble(),
                surfaceWear: random.NextDouble()));

            foreach (var value in new[] { pitch.Pace, pitch.Bounce, pitch.Consistency, pitch.Carry, pitch.Seam, pitch.Spin, pitch.Cracking })
            {
                Assert.InRange(value, 0, 10);
            }
        }
    }

    [Fact]
    public void Lasting_wear_and_ends_that_havent_grown_back_make_bounce_less_consistent()
    {
        var fresh = Strip();
        var worn = Strip();
        worn.LastingWear = 0.2;
        var thin = Strip();
        thin.EndsEstablishment = 0.3;

        Assert.True(Of(worn).Consistency < Of(fresh).Consistency);
        Assert.True(Of(thin).Consistency < Of(fresh).Consistency);
        Assert.Equal(TestPitch.Settings.ConsistencyLastingWeight * 0.2, Model.Explain(worn).LastingWear, 9);
        Assert.Equal(TestPitch.Settings.ConsistencyEndsWeight * 0.7, Model.Explain(thin).ThinEnds, 9);
    }
}
