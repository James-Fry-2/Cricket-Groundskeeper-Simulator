using Groundsman.Core;
using Groundsman.Core.Match;
using Groundsman.Core.Pitch;
using Groundsman.Core.Pressures;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Pressures;

public class RequestJudgeTests
{
    private static readonly RequestJudge Judge = new RequestJudge(TestStakeholders.Settings);
    private static readonly PitchDrivers NoDrivers = new PitchDrivers(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    private static PitchCharacteristics Pitch(double consistency = 8.5, double carry = 5, double seam = 2, double spin = 2) =>
        new PitchCharacteristics(pace: carry, bounce: carry, consistency: consistency, carry: carry, seam: seam, spin: spin, cracking: 0);

    /// <summary>A four-day match from 10 June: six hours a day, each day's pitch given in turn.</summary>
    private static MatchState FourDay(params (PitchCharacteristics Pitch, bool Rained)[] days)
    {
        var fixture = new Fixture(new DateTime(2027, 6, 10), TestFormats.FourDay, new StripId(6), TestTeams.Opponent);
        var match = new MatchState(fixture, new StripId(6), TestTeams.Home);
        for (var day = 0; day < days.Length; day++)
        {
            for (var hour = 11; hour < 17; hour++)
            {
                match.Hours.Add(new MatchHour(new GameTime(2027, 6, 10 + day, hour), days[day].Rained, 16, 50, 1, days[day].Pitch, NoDrivers, 7));
            }
        }
        match.Finished = true;
        return match;
    }

    private static (PitchCharacteristics, bool) Day(PitchCharacteristics pitch, bool rained = false) => (pitch, rained);

    [Fact]
    public void Green_is_judged_on_day_one_seam()
    {
        Assert.Equal(RequestStatus.Delivered, Judge.Judge(RequestKind.Green, FourDay(Day(Pitch(seam: 6)), Day(Pitch(seam: 1)))));
        Assert.Equal(RequestStatus.NotDelivered, Judge.Judge(RequestKind.Green, FourDay(Day(Pitch(seam: 3)), Day(Pitch(seam: 9)))));
        Assert.Equal(RequestStatus.Spoiled, Judge.Judge(RequestKind.Green, FourDay(Day(Pitch(seam: 9), rained: true), Day(Pitch(seam: 9)))));
    }

    [Fact]
    public void Turning_is_judged_on_the_last_day_of_play()
    {
        Assert.Equal(RequestStatus.Delivered, Judge.Judge(RequestKind.Turning, FourDay(Day(Pitch(spin: 1)), Day(Pitch(spin: 2)), Day(Pitch(spin: 5)))));
        Assert.Equal(RequestStatus.NotDelivered, Judge.Judge(RequestKind.Turning, FourDay(Day(Pitch(spin: 6)), Day(Pitch(spin: 2)))));
        Assert.Equal(RequestStatus.NotDelivered, Judge.Judge(RequestKind.Turning, FourDay(Day(Pitch(spin: 5)), Day(Pitch(spin: 2)), Day(Pitch(spin: 9), rained: true))));
    }

    [Fact]
    public void Pace_needs_carry_and_true_bounce()
    {
        Assert.Equal(RequestStatus.Delivered, Judge.Judge(RequestKind.Pace, FourDay(Day(Pitch(carry: 6)), Day(Pitch(carry: 5.5)))));
        Assert.Equal(RequestStatus.NotDelivered, Judge.Judge(RequestKind.Pace, FourDay(Day(Pitch(carry: 4.5)))));
        Assert.Equal(RequestStatus.NotDelivered, Judge.Judge(RequestKind.Pace, FourDay(Day(Pitch(carry: 7, consistency: 6)))));
    }

    [Fact]
    public void Flat_needs_little_movement_and_true_bounce()
    {
        Assert.Equal(RequestStatus.Delivered, Judge.Judge(RequestKind.Flat, FourDay(Day(Pitch(seam: 2, spin: 1)))));
        Assert.Equal(RequestStatus.NotDelivered, Judge.Judge(RequestKind.Flat, FourDay(Day(Pitch(seam: 5, spin: 1)))));
        Assert.Equal(RequestStatus.NotDelivered, Judge.Judge(RequestKind.Flat, FourDay(Day(Pitch(seam: 1, spin: 1, consistency: 6)))));
    }

    [Fact]
    public void Lasting_four_days_needs_cricket_scheduled_on_day_four_even_if_rain_took_it()
    {
        Assert.Equal(RequestStatus.Delivered, Judge.Judge(RequestKind.LastsFourDays, FourDay(Day(Pitch()), Day(Pitch()), Day(Pitch()), Day(Pitch(), rained: true))));
        Assert.Equal(RequestStatus.NotDelivered, Judge.Judge(RequestKind.LastsFourDays, FourDay(Day(Pitch()), Day(Pitch()), Day(Pitch()))));
        Assert.True(Judge.ReachedDayFour(FourDay(Day(Pitch()), Day(Pitch()), Day(Pitch()), Day(Pitch()))));
    }

    [Fact]
    public void A_match_with_no_dry_play_spoils_a_pitch_character_request()
    {
        var washout = FourDay(Day(Pitch(), rained: true));

        Assert.Equal(RequestStatus.Spoiled, Judge.Judge(RequestKind.Pace, washout));
        Assert.Equal(RequestStatus.Spoiled, Judge.Judge(RequestKind.Flat, washout));
        Assert.Equal(RequestStatus.Spoiled, Judge.Judge(RequestKind.Turning, washout));
    }
}
