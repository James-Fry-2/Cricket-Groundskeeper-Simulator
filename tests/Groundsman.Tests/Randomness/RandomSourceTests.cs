using Groundsman.Core.Randomness;

namespace Groundsman.Tests.Randomness;

public class RandomSourceTests
{
    [Fact]
    public void Same_seed_and_stream_give_the_same_sequence()
    {
        var a = new RandomStreams(12345).Get(RandomStream.Weather);
        var b = new RandomStreams(12345).Get(RandomStream.Weather);

        for (var i = 0; i < 1000; i++)
        {
            Assert.Equal(a.NextULong(), b.NextULong());
        }
    }

    [Fact]
    public void Different_seeds_give_different_sequences()
    {
        var a = new RandomStreams(1).Get(RandomStream.Weather);
        var b = new RandomStreams(2).Get(RandomStream.Weather);

        Assert.NotEqual(Take(a, 8), Take(b, 8));
    }

    [Fact]
    public void Streams_from_one_seed_differ_from_each_other()
    {
        var streams = new RandomStreams(99);

        Assert.NotEqual(Take(streams.Get(RandomStream.Weather), 8), Take(streams.Get(RandomStream.Match), 8));
    }

    [Fact]
    public void Drawing_from_one_stream_does_not_shift_another()
    {
        var untouched = new RandomStreams(42);
        var busy = new RandomStreams(42);

        for (var i = 0; i < 500; i++)
        {
            busy.Get(RandomStream.Match).NextDouble();
        }

        Assert.Equal(Take(untouched.Get(RandomStream.Weather), 16), Take(busy.Get(RandomStream.Weather), 16));
    }

    [Fact]
    public void Restored_state_continues_the_exact_sequence()
    {
        var original = new RandomStreams(777);
        foreach (var stream in RandomStreams.All)
        {
            Take(original.Get(stream), 10);
        }

        var restored = RandomStreams.FromState(original.CaptureState());

        foreach (var stream in RandomStreams.All)
        {
            Assert.Equal(Take(original.Get(stream), 20), Take(restored.Get(stream), 20));
        }
    }

    [Fact]
    public void Output_is_pinned_for_a_known_seed()
    {
        // Pinned so any change to the generator or stream derivation, which would silently
        // alter every stored replay and save, fails here first.
        var weather = new RandomStreams(2026).Get(RandomStream.Weather);

        Assert.Equal(PinnedWeatherOutput, Take(weather, 4));
    }

    [Fact]
    public void NextDouble_stays_in_the_unit_interval()
    {
        var random = new RandomStreams(5).Get(RandomStream.Events);

        for (var i = 0; i < 10_000; i++)
        {
            var value = random.NextDouble();
            Assert.InRange(value, 0.0, 0.9999999999999999);
        }
    }

    [Fact]
    public void NextInt_stays_in_range_and_reaches_every_value()
    {
        var random = new RandomStreams(8).Get(RandomStream.Match);
        var seen = new bool[6];

        for (var i = 0; i < 10_000; i++)
        {
            var value = random.NextInt(6);
            Assert.InRange(value, 0, 5);
            seen[value] = true;
        }

        Assert.All(seen, Assert.True);
    }

    [Fact]
    public void NextInt_with_bounds_includes_min_and_excludes_max()
    {
        var random = new RandomStreams(8).Get(RandomStream.Match);
        var seenMin = false;

        for (var i = 0; i < 10_000; i++)
        {
            var value = random.NextInt(-3, 3);
            Assert.InRange(value, -3, 2);
            seenMin |= value == -3;
        }

        Assert.True(seenMin);
    }

    [Fact]
    public void NextInt_rejects_an_empty_range()
    {
        var random = new RandomStreams(8).Get(RandomStream.Match);

        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt(4, 4));
    }

    [Fact]
    public void Chance_at_the_extremes_is_certain()
    {
        var random = new RandomStreams(3).Get(RandomStream.Readings);

        for (var i = 0; i < 1000; i++)
        {
            Assert.False(random.Chance(0.0));
            Assert.True(random.Chance(1.0));
        }
    }

    private static readonly ulong[] PinnedWeatherOutput =
    {
        17367954043230962011UL, 6834421990756294872UL, 15594124092865432783UL, 7845271086588238086UL,
    };

    private static ulong[] Take(RandomSource source, int count)
    {
        var values = new ulong[count];
        for (var i = 0; i < count; i++)
        {
            values[i] = source.NextULong();
        }
        return values;
    }
}
