using Groundsman.Cli;
using Groundsman.Core.Readings;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Cli;

public class FormatTests
{
    [Theory]
    [InlineData(21.2, 29.2, "21–30%")]
    [InlineData(22.0, 30.0, "22–30%")]
    [InlineData(21.9, 29.9, "21–30%")]
    public void Ranges_round_outwards_so_the_truth_stays_inside(double low, double high, string expected)
    {
        Assert.Equal(expected, Format.Percent(new ValueRange(low, high)));
    }

    [Theory]
    [InlineData(0, "today")]
    [InlineData(1, "yesterday")]
    [InlineData(5, "5 days ago")]
    public void Ages_read_in_days(int days, string expected)
    {
        var now = new GameTime(2027, 5, 10, 7);

        Assert.Equal(expected, Format.Age(now.AddDays(-days).AtHour(13), now));
    }

    [Fact]
    public void Times_show_day_date_and_hour()
    {
        Assert.Equal("Thu 25 Mar 2027, 07:00", Format.Time(new GameTime(2027, 3, 25, 7)));
    }
}
