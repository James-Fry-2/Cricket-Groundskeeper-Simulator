using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Match;
using Groundsman.Core.Pitch;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Match;

public class RefereeTests
{
    private static readonly Referee Referee = new Referee(TestRating.Settings, TestCommentary.Settings);

    private static RatingView RateOf(MatchState match) =>
        Referee.Rate(match) ?? throw new InvalidOperationException("Expected a rating: the match had dry play.");

    private static PitchCharacteristics Pitch(double consistency = 8, double carry = 5, double seam = 2, double spin = 2) =>
        new PitchCharacteristics(pace: carry, bounce: carry, consistency: consistency, carry: carry, seam: seam, spin: spin, cracking: 0);

    private static PitchDrivers Drivers(double structureDamage = 0, double loose = 0) =>
        new PitchDrivers(0, 0, structureDamage, loose, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>A match with the given pitch for each hour of play, from 11:00 on its first day.</summary>
    private static MatchState Match(
        FormatSettings format,
        IEnumerable<PitchCharacteristics> hours,
        (double Runs, int Wickets, double Overs)[] innings,
        ResultKind result,
        PitchDrivers? drivers = null)
    {
        var fixture = new Fixture(new DateTime(2027, 6, 10), format, new StripId(6), TestTeams.Opponent);
        var match = new MatchState(fixture, TestTeams.Home);
        var hour = new GameTime(2027, 6, 10, 11);
        foreach (var pitch in hours)
        {
            match.Hours.Add(new MatchHour(hour, false, 16, 50, 1, pitch, drivers ?? Drivers(), 7));
            hour = hour.AddHours(1);
        }
        var batting = TestTeams.Home;
        var bowling = TestTeams.Opponent;
        foreach (var (runs, wickets, overs) in innings)
        {
            match.Innings.Add(new InningsState(batting, bowling) { Runs = runs, Wickets = wickets, Overs = overs, Closed = true });
            (batting, bowling) = (bowling, batting);
        }
        match.Finished = true;
        match.Result = new ResultView(result, result == ResultKind.Win ? TestTeams.Home.Name : null, result.ToString());
        return match;
    }

    private static IEnumerable<PitchCharacteristics> Repeat(PitchCharacteristics pitch, int hours) => Enumerable.Repeat(pitch, hours);

    private static readonly (double, int, double)[] NormalFourDay = { (320, 10, 110), (290, 10, 100), (260, 8, 80), (120, 4, 40) };

    [Fact]
    public void Dangerous_bounce_at_any_point_is_unfit_and_costs_three_demerits()
    {
        var hours = Repeat(Pitch(consistency: 8), 10).Append(Pitch(consistency: 2.8));

        var rating = RateOf(Match(TestFormats.FourDay, hours, NormalFourDay, ResultKind.Draw, Drivers(structureDamage: 0.6)));

        Assert.Equal(PitchGrade.Unfit, rating.Grade);
        Assert.Equal(3, rating.Demerits);
        Assert.StartsWith("R:dangerous 2.8 cause:structureDamage", rating.Reasons[0]);
    }

    [Fact]
    public void Uneven_bounce_on_average_is_unsatisfactory_and_names_the_commentarys_cause()
    {
        var match = Match(TestFormats.FourDay, Repeat(Pitch(consistency: 5.5), 20), NormalFourDay, ResultKind.Win);
        match.Commentary.Add(new CommentaryLine(new GameTime(2027, 6, 10, 12), "uneven", "loose", "UNEVEN. cause:loose.", "cause:loose"));

        var rating = RateOf(match);

        Assert.Equal(PitchGrade.Unsatisfactory, rating.Grade);
        Assert.Equal(1, rating.Demerits);
        Assert.Contains("R:uneven 5.5 cause:loose", rating.Reasons);
    }

    [Fact]
    public void A_slow_low_pitch_is_unsatisfactory()
    {
        var rating = RateOf(Match(TestFormats.FourDay, Repeat(Pitch(carry: 3), 20), NormalFourDay, ResultKind.Draw, Drivers(loose: 0.3)));

        Assert.Equal(PitchGrade.Unsatisfactory, rating.Grade);
        Assert.Contains(rating.Reasons, r => r.StartsWith("R:dead 3 cause:loose"));
    }

    [Fact]
    public void A_four_day_match_over_inside_half_its_overs_at_a_low_average_is_too_much_for_the_bowlers()
    {
        var innings = new (double, int, double)[] { (150, 10, 45), (120, 10, 40), (110, 10, 40), (145, 4, 25) };

        var rating = RateOf(Match(TestFormats.FourDay, Repeat(Pitch(), 10), innings, ResultKind.Win));

        Assert.Equal(PitchGrade.Unsatisfactory, rating.Grade);
        Assert.Contains("R:bowlers 39", rating.Reasons);
    }

    [Fact]
    public void A_limited_overs_match_where_neither_side_got_near_par_is_too_much_for_the_bowlers()
    {
        var innings = new (double, int, double)[] { (98, 10, 31), (99, 3, 20) };

        var rating = RateOf(Match(TestFormats.OneDay, Repeat(Pitch(), 5), innings, ResultKind.Win));

        Assert.Equal(PitchGrade.Unsatisfactory, rating.Grade);
        Assert.Contains("R:limitedLow", rating.Reasons);
    }

    [Fact]
    public void A_high_scoring_draw_with_nothing_for_the_bowlers_is_lifeless()
    {
        var innings = new (double, int, double)[] { (610, 7, 180), (560, 8, 170) };

        var rating = RateOf(Match(TestFormats.FourDay, Repeat(Pitch(consistency: 9, carry: 5, seam: 1, spin: 1), 24), innings, ResultKind.Draw));

        Assert.Equal(PitchGrade.Unsatisfactory, rating.Grade);
        Assert.Contains("R:lifeless", rating.Reasons);
    }

    [Fact]
    public void True_bounce_good_carry_and_something_for_the_bowlers_is_very_good()
    {
        var hours = Repeat(Pitch(consistency: 9, carry: 6, seam: 5), 6).Concat(Repeat(Pitch(consistency: 9, carry: 6, seam: 2), 18));

        var rating = RateOf(Match(TestFormats.FourDay, hours, NormalFourDay, ResultKind.Win));

        Assert.Equal(PitchGrade.VeryGood, rating.Grade);
        Assert.Equal(0, rating.Demerits);
        Assert.Contains("R:consistent 9", rating.Reasons);
        Assert.Contains("R:carry 6", rating.Reasons);
        Assert.Contains("R:movement", rating.Reasons);
    }

    [Fact]
    public void A_decent_pitch_is_satisfactory()
    {
        var rating = RateOf(Match(TestFormats.FourDay, Repeat(Pitch(consistency: 8, carry: 5, seam: 3), 24), NormalFourDay, ResultKind.Win));

        Assert.Equal(PitchGrade.Satisfactory, rating.Grade);
        Assert.Equal(0, rating.Demerits);
    }

    [Fact]
    public void Rain_hours_dont_count_towards_the_averages()
    {
        var match = Match(TestFormats.FourDay, Repeat(Pitch(consistency: 9, carry: 6, seam: 5), 24), NormalFourDay, ResultKind.Win);
        match.Hours.Add(new MatchHour(new GameTime(2027, 6, 11, 11), true, 0, 0, 0, Pitch(consistency: 1, carry: 1), Drivers(), 7));

        Assert.Equal(PitchGrade.VeryGood, RateOf(match).Grade);
    }
}
