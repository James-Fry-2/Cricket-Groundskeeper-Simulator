using Groundsman.Core;
using Groundsman.Core.Randomness;
using Groundsman.Core.Time;
using Groundsman.Core.Weather;

namespace Groundsman.Tests.Forecasting;

public class ForecastTests
{
    private static readonly DateTime MatchDay = new DateTime(2027, 5, 20);

    private static Game NewGame(ulong seed = 1, GameTime? start = null) =>
        new Game(TestContent.Setup(start ?? new GameTime(2027, 5, 10, 7), new[] { MatchDay }, seed));

    [Fact]
    public void Covers_today_and_the_days_ahead()
    {
        var forecast = NewGame().View.Forecast;

        Assert.Equal(TestForecast.Settings.Days, forecast.Count);
        Assert.Equal(new DateTime(2027, 5, 10), forecast[0].Date);
        Assert.Equal(new DateTime(2027, 5, 16), forecast[6].Date);
        Assert.All(forecast, day => Assert.Equal(new DateTime(2027, 5, 10), day.IssuedOn));
    }

    [Fact]
    public void Stays_the_same_between_turns_on_the_same_day()
    {
        var game = NewGame(start: new GameTime(2027, 5, 18, 7));
        var morning = game.View.Forecast;

        game.Advance();

        Assert.Equal(new GameTime(2027, 5, 18, 13), game.View.Now);
        Assert.Equal(morning.Select(d => d.Rain), game.View.Forecast.Select(d => d.Rain));
        Assert.Equal(morning.Select(d => d.MaxTemperature), game.View.Forecast.Select(d => d.MaxTemperature));
    }

    [Fact]
    public void A_new_forecast_is_issued_each_new_day()
    {
        var game = NewGame();
        var monday = game.View.Forecast;

        game.Advance();
        var tuesday = game.View.Forecast;

        Assert.Equal(new DateTime(2027, 5, 11), tuesday[0].Date);
        Assert.Equal(new DateTime(2027, 5, 11), tuesday[0].IssuedOn);
        Assert.NotEqual(monday[1].MaxTemperature, tuesday[0].MaxTemperature);
    }

    [Fact]
    public void Is_less_certain_further_ahead()
    {
        var forecast = NewGame().View.Forecast;

        for (var lead = 1; lead < forecast.Count; lead++)
        {
            Assert.True(forecast[lead].MaxTemperature.Width > forecast[lead - 1].MaxTemperature.Width);
        }
    }

    [Fact]
    public void Range_widths_depend_only_on_how_far_ahead_so_they_give_nothing_away()
    {
        var a = NewGame(seed: 1).View.Forecast;
        var b = NewGame(seed: 2).View.Forecast;

        for (var lead = 0; lead < a.Count; lead++)
        {
            Assert.Equal(a[lead].MaxTemperature.Width, b[lead].MaxTemperature.Width, 9);
        }
    }

    [Fact]
    public void Rain_ranges_never_go_below_zero()
    {
        for (ulong seed = 1; seed <= 50; seed++)
        {
            Assert.All(NewGame(seed).View.Forecast, day => Assert.True(day.Rain.Low >= 0));
        }
    }

    [Fact]
    public void Truth_usually_falls_inside_the_range_but_not_always()
    {
        var temperatureHits = 0;
        var rainHits = 0;
        var total = 0;
        for (ulong seed = 1; seed <= 400; seed++)
        {
            var game = NewGame(seed);
            foreach (var day in game.View.Forecast)
            {
                var truth = game.Weather.Day(day.Date);
                total++;
                temperatureHits += day.MaxTemperature.Contains(truth.MaxTemperature) ? 1 : 0;
                rainHits += day.Rain.Contains(truth.RainMm) ? 1 : 0;
            }
        }

        // 1.5 spreads either side of a normal error holds the truth about 87% of the time.
        Assert.InRange(temperatureHits / (double)total, 0.84, 0.90);
        Assert.InRange(rainHits / (double)total, 0.84, 0.97);
    }

    [Fact]
    public void Looking_ahead_for_the_forecast_does_not_change_the_weather()
    {
        var game = NewGame(seed: 5);
        var generator = new WeatherGenerator(TestClimate.Settings, new RandomStreams(5).Get(RandomStream.Weather));

        for (var i = 0; i < 20; i++)
        {
            game.Advance();
        }

        for (var date = new DateTime(2027, 5, 10); date < new DateTime(2027, 5, 30); date = date.AddDays(1))
        {
            Assert.Equal(generator.Next(date).RainMm, game.Weather.Day(date).RainMm);
        }
    }
}
