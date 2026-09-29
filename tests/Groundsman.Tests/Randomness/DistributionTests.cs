using Groundsman.Core.Randomness;

namespace Groundsman.Tests.Randomness;

public class DistributionTests
{
    private const int Samples = 100_000;

    [Fact]
    public void Output_is_pinned_for_a_known_seed()
    {
        // Values from an independent reference implementation. The tolerance allows for
        // last-digit differences between maths libraries, not for a changed algorithm.
        var random = new RandomStreams(2026).Get(RandomStream.Weather);

        Assert.Equal(-1.636582503344879, random.NextGaussian(), 12);
        Assert.Equal(-1.7231924374322911, random.NextGaussian(), 12);
        Assert.Equal(-2.2780908062755643, random.NextGaussian(), 12);
        Assert.Equal(8.503154268633512, random.NextExponential(5.0), 12);
        Assert.Equal(2.7317678497206406, random.NextExponential(5.0), 12);
    }

    [Fact]
    public void Gaussian_has_the_requested_mean_and_spread()
    {
        var random = new RandomStreams(11).Get(RandomStream.Weather);
        var values = Enumerable.Range(0, Samples).Select(_ => random.NextGaussian(10.0, 2.0)).ToArray();

        var mean = values.Average();
        var sd = Math.Sqrt(values.Select(v => (v - mean) * (v - mean)).Average());

        Assert.InRange(mean, 9.98, 10.02);
        Assert.InRange(sd, 1.98, 2.02);
    }

    [Fact]
    public void Gaussian_is_symmetric()
    {
        var random = new RandomStreams(12).Get(RandomStream.Weather);
        var below = Enumerable.Range(0, Samples).Count(_ => random.NextGaussian() < 0);

        Assert.InRange(below / (double)Samples, 0.495, 0.505);
    }

    [Fact]
    public void Gaussian_with_zero_spread_returns_the_mean()
    {
        var random = new RandomStreams(13).Get(RandomStream.Weather);

        Assert.Equal(7.5, random.NextGaussian(7.5, 0.0));
    }

    [Fact]
    public void Exponential_is_positive_with_the_requested_mean()
    {
        var random = new RandomStreams(14).Get(RandomStream.Weather);
        var values = Enumerable.Range(0, Samples).Select(_ => random.NextExponential(6.0)).ToArray();

        Assert.All(values, v => Assert.True(v >= 0));
        Assert.InRange(values.Average(), 5.9, 6.1);
    }

    [Fact]
    public void Rejects_invalid_parameters()
    {
        var random = new RandomStreams(15).Get(RandomStream.Weather);

        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextGaussian(0.0, -1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextExponential(0.0));
    }
}

public class PoissonTests
{
    [Theory]
    [InlineData(0.5)]
    [InlineData(3.0)]
    [InlineData(12.0)]
    public void Poisson_has_the_requested_mean(double mean)
    {
        var random = new RandomStreams(21).Get(RandomStream.Match);
        var values = Enumerable.Range(0, 50_000).Select(_ => random.NextPoisson(mean)).ToArray();

        Assert.All(values, v => Assert.True(v >= 0));
        Assert.InRange(values.Average(), mean * 0.97, mean * 1.03);
    }

    [Fact]
    public void Poisson_of_zero_is_zero()
    {
        Assert.Equal(0, new RandomStreams(1).Get(RandomStream.Match).NextPoisson(0));
    }
}
