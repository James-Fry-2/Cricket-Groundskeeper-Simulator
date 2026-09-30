using Groundsman.Core;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests;

public class FixtureViewTests
{
    private static readonly Fixture FourDay = new Fixture(new DateTime(2027, 5, 20), TestFormats.FourDay, new StripId(6), TestTeams.Opponent);
    private static readonly Fixture OneDay = new Fixture(new DateTime(2027, 6, 1), TestFormats.OneDay, new StripId(9), TestTeams.Opponent);

    private static Game NewGame(GameTime start) =>
        new Game(new GameSetup(TestContent.Content, start, new[] { FourDay, OneDay }, seed: 1));

    [Fact]
    public void Shows_the_next_fixture_with_its_strip()
    {
        var next = NewGame(new GameTime(2027, 5, 10, 7)).View.NextFixture;

        Assert.Same(FourDay, next);
    }

    [Fact]
    public void A_fixture_stays_next_until_its_last_day_is_over()
    {
        Assert.Same(FourDay, NewGame(new GameTime(2027, 5, 23, 8)).View.NextFixture);
        Assert.Same(OneDay, NewGame(new GameTime(2027, 5, 24, 7)).View.NextFixture);
        Assert.Null(NewGame(new GameTime(2027, 6, 2, 7)).View.NextFixture);
    }

    [Fact]
    public void The_fixture_names_its_format_and_opponent()
    {
        var next = NewGame(new GameTime(2027, 5, 10, 7)).View.NextFixture!;

        Assert.Equal("Four-day", next.Format.Name);
        Assert.Equal("Test Visitors", next.Opponent.Name);
    }

    [Fact]
    public void Every_day_of_a_fixture_is_a_match_day()
    {
        var game = NewGame(new GameTime(2027, 5, 21, 7));

        Assert.Equal(DayPace.MatchDay, game.View.Pace);
    }

    [Fact]
    public void The_whole_fixture_list_is_known_all_season()
    {
        Assert.Equal(new[] { FourDay, OneDay }, NewGame(new GameTime(2027, 5, 10, 7)).View.Fixtures);
        Assert.Equal(new[] { FourDay, OneDay }, NewGame(new GameTime(2027, 6, 2, 7)).View.Fixtures);
    }
}
