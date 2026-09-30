using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Randomness;
using Groundsman.Core.Readings;
using Groundsman.Core.Strips;

namespace Groundsman.Harness.Policies;

/// <summary>Up to three random jobs a turn, legal or not, of every kind: a baseline that proves the others differ.</summary>
public sealed class RandomPolicy : IPolicy
{
    private readonly RandomSource _random;

    public RandomPolicy(ulong seed)
    {
        _random = new RandomStreams(seed).Get(RandomStream.Events);
    }

    public string Name => "random";

    public void PlayTurn(IGame game)
    {
        var view = game.View;
        var jobs = _random.NextInt(4);
        for (var i = 0; i < jobs; i++)
        {
            var strip = new StripId(_random.NextInt(1, view.Strips.Count + 1));
            var by = view.Staff[_random.NextInt(view.Staff.Count)].Id;
            var fixture = view.Fixtures[_random.NextInt(view.Fixtures.Count)];
            IGameCommand command = _random.NextInt(9) switch
            {
                0 => new WaterStrip(strip, by),
                1 => new CoverStrip(strip, by),
                2 => new UncoverStrip(strip, by),
                3 => new TakeReading(strip, by),
                4 => new TakeReading(strip, by, ReadingSource.Feel),
                5 => new MowStrip(strip, _random.NextInt(5, 26), by),
                6 => new RollStrip(strip, view.Rollers[_random.NextInt(view.Rollers.Count)].Id, _random.NextInt(10, 61), by),
                7 => new AssignStrip(fixture.Id, strip),
                _ => new RepairEnds(strip, by),
            };
            game.Submit(command);
        }
    }
}
