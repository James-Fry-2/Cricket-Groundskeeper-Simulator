using Groundsman.Core.Match;

namespace Groundsman.Tests.Match;

public class DemeritLedgerTests
{
    private static RatingView Rated(PitchGrade grade, int demerits, DateTime on) => new RatingView(grade, demerits, Array.Empty<string>(), on);

    [Fact]
    public void Counts_demerits_inside_the_window_and_drops_them_after()
    {
        var ledger = new DemeritLedger(TestRating.Settings);
        ledger.Add(Rated(PitchGrade.Unfit, 3, new DateTime(2027, 6, 1)));
        ledger.Add(Rated(PitchGrade.Unsatisfactory, 1, new DateTime(2029, 7, 1)));

        Assert.Equal(4, ledger.Active(new DateTime(2030, 1, 1)));
        Assert.Equal(1, ledger.Active(new DateTime(2032, 6, 2)));
        Assert.Equal(0, ledger.Active(new DateTime(2034, 7, 2)));
    }

    [Fact]
    public void Flags_a_ban_at_the_threshold()
    {
        var ledger = new DemeritLedger(TestRating.Settings);
        ledger.Add(Rated(PitchGrade.Unfit, 3, new DateTime(2027, 6, 1)));
        Assert.False(ledger.Banned(new DateTime(2027, 6, 2)));

        ledger.Add(Rated(PitchGrade.Unsatisfactory, 1, new DateTime(2027, 7, 1)));
        ledger.Add(Rated(PitchGrade.Unsatisfactory, 1, new DateTime(2027, 8, 1)));

        Assert.True(ledger.Banned(new DateTime(2027, 8, 2)));
    }
}
