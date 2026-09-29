using Groundsman.Core.Content;
using Groundsman.Core.Match;
using Groundsman.Core.Pitch;
using Groundsman.Core.Randomness;

namespace Groundsman.Tests.Match;

public class MatchModelTests
{
    private const int Blocks = 3000;

    private static readonly PitchCharacteristics Flat = new PitchCharacteristics(pace: 6, bounce: 6, consistency: 9.5, carry: 6, seam: 1, spin: 1, cracking: 0);

    private static TeamSettings Team(double batting = 60, double bowling = 60, double seam = 0.7) =>
        new TeamSettings("t", "Team", batting, bowling, new AttackProfile(seam, 0.2, 0.5));

    private static (double RunsPerOver, double WicketsPerOver) Average(
        PitchCharacteristics pitch,
        TeamSettings? batting = null,
        TeamSettings? bowling = null,
        FormatSettings? format = null,
        ulong seed = 1)
    {
        var model = new MatchModel(TestMatch.Settings, new RandomStreams(seed).Get(RandomStream.Match));
        double runs = 0, wickets = 0, overs = 0;
        for (var i = 0; i < Blocks; i++)
        {
            var block = model.PlayBlock(pitch, batting ?? Team(), bowling ?? Team(), format ?? TestFormats.FourDay, overs: 16, wicketsInHand: 10);
            runs += block.Runs;
            wickets += block.Wickets;
            overs += block.Overs;
        }
        return (runs / overs, wickets / overs);
    }

    private static PitchCharacteristics With(double? carry = null, double? consistency = null, double? seam = null, double? spin = null) =>
        new PitchCharacteristics(Flat.Pace, Flat.Bounce, consistency ?? Flat.Consistency, carry ?? Flat.Carry, seam ?? Flat.Seam, spin ?? Flat.Spin, 0);

    [Fact]
    public void A_flat_pitch_plays_close_to_the_formats_base_rates()
    {
        var (runs, wickets) = Average(Flat);

        Assert.InRange(runs, TestFormats.FourDay.RunsPerOver * 0.9, TestFormats.FourDay.RunsPerOver * 1.3);
        Assert.InRange(wickets, TestFormats.FourDay.WicketsPerOver * 0.8, TestFormats.FourDay.WicketsPerOver * 1.8);
    }

    [Fact]
    public void Seam_helps_a_seam_attack_and_spin_helps_a_spin_attack()
    {
        var flat = Average(Flat, bowling: Team(seam: 0.8)).WicketsPerOver;

        Assert.True(Average(With(seam: 7), bowling: Team(seam: 0.8)).WicketsPerOver > flat * 1.3);
        Assert.True(Average(With(spin: 7), bowling: Team(seam: 0.2)).WicketsPerOver > Average(Flat, bowling: Team(seam: 0.2)).WicketsPerOver * 1.3);
        Assert.True(Average(With(spin: 7), bowling: Team(seam: 0.9)).WicketsPerOver < Average(With(spin: 7), bowling: Team(seam: 0.2)).WicketsPerOver);
    }

    [Fact]
    public void Uneven_bounce_takes_wickets_and_costs_runs()
    {
        var flat = Average(Flat);
        var uneven = Average(With(consistency: 4));

        Assert.True(uneven.WicketsPerOver > flat.WicketsPerOver * 1.5);
        Assert.True(uneven.RunsPerOver < flat.RunsPerOver);
    }

    [Fact]
    public void A_dead_pitch_is_slow_to_score_on_and_hard_to_take_wickets_on()
    {
        var flat = Average(Flat);
        var dead = Average(With(carry: 1.5));

        Assert.True(dead.RunsPerOver < flat.RunsPerOver);
        Assert.True(dead.WicketsPerOver < flat.WicketsPerOver);
    }

    [Fact]
    public void Stronger_batting_scores_more_and_loses_fewer_wickets()
    {
        var even = Average(Flat);
        var strong = Average(Flat, batting: Team(batting: 80));

        Assert.True(strong.RunsPerOver > even.RunsPerOver);
        Assert.True(strong.WicketsPerOver < even.WicketsPerOver);
    }

    [Fact]
    public void Shorter_formats_score_faster()
    {
        Assert.True(Average(Flat, format: TestFormats.T20).RunsPerOver > Average(Flat, format: TestFormats.OneDay).RunsPerOver);
        Assert.True(Average(Flat, format: TestFormats.OneDay).RunsPerOver > Average(Flat).RunsPerOver);
    }

    [Fact]
    public void A_side_bowled_out_mid_block_only_uses_the_overs_it_faced()
    {
        var model = new MatchModel(TestMatch.Settings, new RandomStreams(3).Get(RandomStream.Match));
        var crumbling = With(consistency: 0, seam: 10);
        var bowledOut = 0;
        var early = 0;

        for (var i = 0; i < 500; i++)
        {
            var block = model.PlayBlock(crumbling, Team(batting: 20), Team(bowling: 95, seam: 1), TestFormats.T20, overs: 10, wicketsInHand: 1);
            Assert.InRange(block.Wickets, 0, 1);
            Assert.InRange(block.Overs, 0, 10);
            if (block.Wickets == 0)
            {
                Assert.Equal(10, block.Overs);
                continue;
            }
            bowledOut++;
            early += block.Overs < 10 ? 1 : 0;
        }

        Assert.True(bowledOut > 400);
        Assert.True(early > bowledOut * 0.9, $"{early} of {bowledOut} ended early");
    }

    [Fact]
    public void The_same_seed_plays_the_same()
    {
        Assert.Equal(Average(Flat, seed: 9), Average(Flat, seed: 9));
    }
}
