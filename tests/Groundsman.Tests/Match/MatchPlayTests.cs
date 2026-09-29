using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Match;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Match;

public class MatchPlayTests
{
    private static readonly StripId MatchStrip = new StripId(6);

    /// <summary>A game with the match strip rolled and mown, so the pitch plays like a prepared one.</summary>
    private static Game GameWith(FormatSettings format, ulong seed = 1, DateTime? start = null, bool prepared = true)
    {
        var fixture = new Fixture(start ?? new DateTime(2027, 6, 10), format, MatchStrip, TestTeams.Opponent);
        var game = new Game(new GameSetup(TestContent.Content, new GameTime(2027, 6, 1, 7), new[] { fixture }, seed));
        if (prepared)
        {
            var strip = game.Square.Get(MatchStrip);
            strip.Compaction = 0.8;
            strip.GrassHeightMm = 7;
        }
        return game;
    }

    private static Game PlayThrough(Game game)
    {
        var fixture = game.View.NextFixture!;
        while (game.View.Now.Date <= fixture.End)
        {
            game.Advance();
        }
        return game;
    }

    [Fact]
    public void A_one_day_match_is_two_innings_of_up_to_fifty_overs_with_a_result()
    {
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var match = PlayThrough(GameWith(TestFormats.OneDay, seed)).View.LatestMatch!;

            Assert.True(match.Finished);
            Assert.NotNull(match.Result);
            if (match.Result!.Kind == ResultKind.NoResult)
            {
                continue;
            }
            Assert.Equal(2, match.Innings.Count);
            Assert.All(match.Innings, i => Assert.InRange(i.Overs, 0, 50));
            Assert.NotNull(match.Result);
            var first = match.Innings[0];
            var second = match.Innings[1];
            if (match.Result!.Kind == ResultKind.Win)
            {
                var winner = second.Runs > first.Runs ? second.Batting : first.Batting;
                Assert.Equal(winner, match.Result.Winner);
            }
            else if (match.Result.Kind == ResultKind.Tie)
            {
                Assert.Equal(first.Runs, second.Runs);
            }
        }
    }

    [Fact]
    public void A_chase_stops_once_the_target_is_passed()
    {
        for (ulong seed = 1; seed <= 30; seed++)
        {
            var match = PlayThrough(GameWith(TestFormats.OneDay, seed)).View.LatestMatch!;
            if (match.Innings.Count < 2)
            {
                continue;
            }
            var second = match.Innings[1];
            if (second.Runs > match.Innings[0].Runs)
            {
                Assert.True(second.Wickets < 10);
                Assert.Contains("wicket", match.Result!.Text);
            }
        }
    }

    [Fact]
    public void A_t20_is_played_in_the_evening()
    {
        var game = GameWith(TestFormats.T20);
        while (game.View.Now < new GameTime(2027, 6, 10, 18))
        {
            game.Advance();
        }

        var beforePlay = game.View.LatestMatch;
        Assert.True(beforePlay == null || beforePlay.Innings.Sum(i => i.Overs) == 0);

        PlayThrough(game);
        var match = game.View.LatestMatch!;
        Assert.True(match.Finished);
        Assert.All(match.Innings, i => Assert.InRange(i.Overs, 0, 20));
    }

    [Fact]
    public void A_four_day_match_finishes_by_its_last_day_with_wins_and_draws_across_seeds()
    {
        var kinds = new HashSet<ResultKind>();
        var declarations = 0;
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var match = PlayThrough(GameWith(TestFormats.FourDay, seed)).View.LatestMatch!;

            Assert.True(match.Finished);
            Assert.InRange(match.Innings.Count, 2, 4);
            kinds.Add(match.Result!.Kind);
            declarations += match.Innings.Count(i => i.Declared);
        }

        Assert.Contains(ResultKind.Win, kinds);
        Assert.Contains(ResultKind.Draw, kinds);
        Assert.True(declarations > 0);
    }

    [Fact]
    public void Totals_on_a_prepared_pitch_look_like_county_cricket()
    {
        var oneDay = new List<double>();
        var fourDay = new List<double>();
        for (ulong seed = 1; seed <= 60; seed++)
        {
            var one = PlayThrough(GameWith(TestFormats.OneDay, seed)).View.LatestMatch!.Innings[0];
            if (one.Overs >= 45 || one.Wickets == 10)
            {
                oneDay.Add(one.Runs);
            }
            var four = PlayThrough(GameWith(TestFormats.FourDay, seed)).View.LatestMatch!.Innings[0];
            if (four.Wickets == 10)
            {
                fourDay.Add(four.Runs);
            }
        }

        Assert.InRange(oneDay.Average(), 220, 330);
        Assert.InRange(fourDay.Average(), 220, 420);
    }

    [Fact]
    public void A_neglected_pitch_makes_for_lower_scoring_cricket_than_a_prepared_one()
    {
        double Average(bool prepared) => Enumerable.Range(1, 40)
            .Select(seed => PlayThrough(GameWith(TestFormats.OneDay, (ulong)seed, prepared: prepared)).View.LatestMatch!.Innings[0].Runs)
            .Average();

        Assert.True(Average(prepared: false) < Average(prepared: true));
    }

    [Fact]
    public void A_first_innings_rained_down_to_under_half_its_overs_means_no_result()
    {
        var sawNoResult = false;
        for (ulong seed = 1; seed <= 60; seed++)
        {
            var match = PlayThrough(GameWith(TestFormats.OneDay, seed)).View.LatestMatch!;
            var first = match.Innings[0];
            if (first.Overs < 25 && first.Wickets < 10)
            {
                sawNoResult = true;
                Assert.Equal(ResultKind.NoResult, match.Result!.Kind);
            }
            if (match.Innings.Count == 2 && first.Overs < 50 && first.Wickets < 10)
            {
                Assert.InRange(match.Innings[1].Overs, 0, first.Overs + 1e-9);
            }
        }

        Assert.True(sawNoResult, "No seed had a first innings rained short; pick more seeds");
    }

    [Fact]
    public void No_overs_are_bowled_in_hours_when_it_rains()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var game = PlayThrough(GameWith(TestFormats.FourDay, seed));
            foreach (var hour in game.Matches.Latest!.Hours.Where(h => h.Rained))
            {
                Assert.Equal(0, hour.Overs);
            }
        }
    }

    [Fact]
    public void The_match_strip_is_covered_outside_play_and_in_rain_and_open_for_dry_play()
    {
        var game = GameWith(TestFormats.FourDay);
        while (game.View.Now < new GameTime(2027, 6, 10, 8))
        {
            game.Advance();
        }

        Assert.True(game.View.Strips[5].Covered);

        game.Covers.RunHour(new GameTime(2027, 6, 10, 11));
        game.Weather.RunHour(new GameTime(2027, 6, 10, 11));
        var raining = game.Weather.LastHour!.Value.RainMm > 0;
        Assert.Equal(raining, game.Covers.IsCovered(MatchStrip));
    }

    [Fact]
    public void Play_wears_the_match_strip_and_no_other()
    {
        var game = PlayThrough(GameWith(TestFormats.FourDay));

        Assert.True(game.Square.Get(MatchStrip).Footholes > 0);
        Assert.Equal(0, game.Square.Get(new StripId(5)).Footholes);
    }

    [Fact]
    public void The_same_seed_plays_the_same_match()
    {
        var a = PlayThrough(GameWith(TestFormats.FourDay, 7)).View.LatestMatch!;
        var b = PlayThrough(GameWith(TestFormats.FourDay, 7)).View.LatestMatch!;

        Assert.Equal(a.Innings.Select(i => i.Runs), b.Innings.Select(i => i.Runs));
        Assert.Equal(a.Result!.Text, b.Result!.Text);
    }

    [Fact]
    public void The_pitch_is_logged_for_every_hour_of_play()
    {
        var game = PlayThrough(GameWith(TestFormats.OneDay));
        var hours = game.Matches.Latest!.Hours;

        Assert.NotEmpty(hours);
        Assert.All(hours, h => Assert.InRange(h.Pitch.Pace, 0, 10));
    }
}
