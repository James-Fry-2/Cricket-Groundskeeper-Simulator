using Groundsman.Core;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests;

public class FixtureViewTests
{
    private static readonly Fixture FourDay = new Fixture(new DateTime(2027, 5, 20), 4, new StripId(6));
    private static readonly Fixture OneDay = new Fixture(new DateTime(2027, 6, 1), 1, new StripId(9));

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
    public void Every_day_of_a_fixture_is_a_match_day()
    {
        var game = NewGame(new GameTime(2027, 5, 21, 7));

        Assert.Equal(DayPace.MatchDay, game.View.Pace);
    }
}
