using Groundsman.Core;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Weather;

public class ObservedWeatherTests
{
    private static Game NewGame(GameTime start, ulong seed = 1) => new Game(TestContent.Setup(start, seed: seed));

    [Fact]
    public void Nothing_is_observed_before_time_has_passed()
    {
        Assert.Null(NewGame(new GameTime(2027, 5, 10, 7)).View.Weather);
    }

    [Fact]
    public void Rain_gauge_shows_the_true_rain_of_the_last_24_hours()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var game = NewGame(new GameTime(2027, 5, 10, 7), seed);
            game.Advance();
            game.Advance();

            var expected = Enumerable.Range(1, 24)
                .Select(i => game.View.Now.AddHours(-i))
                .Sum(hour => game.Weather.Day(hour.Date).Hours[hour.Hour].RainMm);

            Assert.Equal(expected, game.View.Weather!.RainLast24HoursMm, 9);
        }
    }

    [Fact]
    public void A_week_long_turn_still_shows_only_the_last_24_hours()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var game = NewGame(new GameTime(2027, 1, 4, 7), seed);
            game.Advance();

            var yesterday = game.Weather.Day(new DateTime(2027, 1, 10));
            var today = game.Weather.Day(new DateTime(2027, 1, 11));
            var expected = yesterday.Hours.Skip(7).Sum(h => h.RainMm) + today.Hours.Take(7).Sum(h => h.RainMm);

            Assert.Equal(expected, game.View.Weather!.RainLast24HoursMm, 9);
        }
    }

    [Fact]
    public void Temperature_and_wind_are_those_of_the_hour_just_gone()
    {
        var game = NewGame(new GameTime(2027, 5, 10, 7));
        game.Advance();

        var lastHour = game.Weather.Day(game.View.Now.Date).Hours[game.View.Now.Hour - 1];

        Assert.Equal(lastHour.Temperature, game.View.Weather!.Temperature);
        Assert.Equal(lastHour.WindKph, game.View.Weather.WindKph);
    }

    [Fact]
    public void Shows_yesterdays_low_and_high()
    {
        var game = NewGame(new GameTime(2027, 5, 10, 7));
        game.Advance();

        var yesterday = game.Weather.Day(new DateTime(2027, 5, 10));

        Assert.Equal(yesterday.MinTemperature, game.View.Weather!.YesterdayLow);
        Assert.Equal(yesterday.MaxTemperature, game.View.Weather.YesterdayHigh);
    }

    [Fact]
    public void Has_no_yesterday_on_the_first_day()
    {
        var game = NewGame(new GameTime(2027, 5, 19, 13), seed: 1);
        game.Advance();

        Assert.Equal(new DateTime(2027, 5, 20), game.View.Now.Date);
        Assert.NotNull(game.View.Weather!.YesterdayLow);

        var sameDay = new Groundsman.Core.Game(TestContent.Setup(new GameTime(2027, 5, 10, 0), new[] { new DateTime(2027, 5, 10) }));
        sameDay.Advance();

        Assert.Equal(new DateTime(2027, 5, 10), sameDay.View.Now.Date);
        Assert.Null(sameDay.View.Weather!.YesterdayLow);
        Assert.Null(sameDay.View.Weather.YesterdayHigh);
    }

    [Fact]
    public void The_same_seed_gives_the_same_weather_in_the_game()
    {
        var a = NewGame(new GameTime(2027, 5, 10, 7), seed: 9);
        var b = NewGame(new GameTime(2027, 5, 10, 7), seed: 9);

        for (var i = 0; i < 10; i++)
        {
            a.Advance();
            b.Advance();
            Assert.Equal(a.View.Weather!.RainLast24HoursMm, b.View.Weather!.RainLast24HoursMm);
        }
    }
}
