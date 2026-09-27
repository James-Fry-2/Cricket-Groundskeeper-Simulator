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
    [InlineData(0.0, 0.0, "dry")]
    [InlineData(0.0, 2.3, "0–3 mm")]
    [InlineData(4.6, 11.2, "4–12 mm")]
    public void Rain_ranges_round_outwards_and_read_dry_when_nothing_is_expected(double low, double high, string expected)
    {
        Assert.Equal(expected, Format.Rain(new ValueRange(low, high)));
    }

    [Fact]
    public void Temperature_ranges_round_outwards()
    {
        Assert.Equal("17–22°C", Format.Temperature(new ValueRange(17.6, 21.2)));
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
    public void Weather_shows_gauge_temperature_and_wind()
    {
        var text = Format.Weather(new Groundsman.Core.WeatherObservation(3.26, 11.4, 17.6, 9.6, 22.4));

        Assert.Equal("Rain in the last 24 hours: 3.3 mm. Now 11°C, wind 18 km/h. Yesterday 10–22°C.", text);
    }

    [Fact]
    public void Weather_leaves_out_yesterday_when_there_was_none()
    {
        var text = Format.Weather(new Groundsman.Core.WeatherObservation(0, 11.4, 17.6, null, null));

        Assert.Equal("Rain in the last 24 hours: 0.0 mm. Now 11°C, wind 18 km/h.", text);
    }

    [Fact]
    public void Times_show_day_date_and_hour()
    {
        Assert.Equal("Thu 25 Mar 2027, 07:00", Format.Time(new GameTime(2027, 3, 25, 7)));
    }
}
