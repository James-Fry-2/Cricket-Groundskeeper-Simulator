using Groundsman.Core.Time;

namespace Groundsman.Tests.Time;

public class GameTimeTests
{
    [Fact]
    public void Adding_hours_rolls_over_midnight()
    {
        var time = new GameTime(2027, 4, 30, 22).AddHours(3);

        Assert.Equal(new GameTime(2027, 5, 1, 1), time);
        Assert.Equal(new DateTime(2027, 5, 1), time.Date);
        Assert.Equal(1, time.Hour);
    }

    [Fact]
    public void Adding_days_rolls_over_the_year_end()
    {
        var time = new GameTime(2027, 12, 29, 7).AddDays(5);

        Assert.Equal(new GameTime(2028, 1, 3, 7), time);
    }

    [Fact]
    public void AtHour_keeps_the_date()
    {
        var time = new GameTime(2027, 6, 10, 7).AtHour(13);

        Assert.Equal(new GameTime(2027, 6, 10, 13), time);
    }

    [Fact]
    public void HoursUntil_counts_whole_hours_between_times()
    {
        var from = new GameTime(2027, 6, 10, 7);

        Assert.Equal(24, from.HoursUntil(new GameTime(2027, 6, 11, 7)));
        Assert.Equal(-6, from.HoursUntil(new GameTime(2027, 6, 10, 1)));
    }

    [Fact]
    public void Times_order_chronologically()
    {
        var earlier = new GameTime(2027, 6, 10, 23);
        var later = new GameTime(2027, 6, 11, 0);

        Assert.True(earlier < later);
        Assert.True(later > earlier);
        Assert.True(earlier <= new GameTime(2027, 6, 10, 23));
        Assert.Equal(-1, earlier.CompareTo(later));
    }

    [Fact]
    public void Formats_as_date_and_hour()
    {
        Assert.Equal("2027-04-01 07:00", new GameTime(2027, 4, 1, 7).ToString());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public void Rejects_an_hour_outside_the_day(int hour)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameTime(2027, 4, 1, hour));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameTime(2027, 4, 1, 7).AtHour(hour));
    }
}
