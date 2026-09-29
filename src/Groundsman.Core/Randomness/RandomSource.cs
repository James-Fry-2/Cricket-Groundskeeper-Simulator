using System;

namespace Groundsman.Core.Randomness
{
    /// <summary>
    /// xoshiro256** (Blackman and Vigna). Implemented here rather than using System.Random so the
    /// sequence is fixed across runtimes, including Unity's, and the state can be saved.
    /// </summary>
    public sealed class RandomSource
    {
        private ulong _s0;
        private ulong _s1;
        private ulong _s2;
        private ulong _s3;

        public RandomSource(RandomState state)
        {
            if (state.IsZero)
            {
                throw new ArgumentException("xoshiro256** cannot run from an all-zero state.", nameof(state));
            }

            _s0 = state.S0;
            _s1 = state.S1;
            _s2 = state.S2;
            _s3 = state.S3;
        }

        public RandomState State => new RandomState(_s0, _s1, _s2, _s3);

        public ulong NextULong()
        {
            var result = RotateLeft(_s1 * 5, 7) * 9;
            var t = _s1 << 17;

            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;
            _s2 ^= t;
            _s3 = RotateLeft(_s3, 45);

            return result;
        }

        /// <summary>Uniform in [0, 1).</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

        /// <summary>Uniform in [min, max).</summary>
        public double NextDouble(double min, double max) => min + (max - min) * NextDouble();

        /// <summary>Uniform in [0, maxExclusive).</summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "Must be positive.");
            }

            return (int)NextBounded((ulong)maxExclusive);
        }

        /// <summary>Uniform in [minInclusive, maxExclusive).</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "Must be greater than the minimum.");
            }

            var span = (ulong)((long)maxExclusive - minInclusive);
            return (int)((long)minInclusive + (long)NextBounded(span));
        }

        public bool Chance(double probability) => NextDouble() < probability;

        /// <summary>Standard normal, by Box-Muller.</summary>
        public double NextGaussian()
        {
            // One value per call, discarding Box-Muller's second, so the generator's four words
            // stay the only state that saves need to capture.
            var u1 = 1.0 - NextDouble();
            var u2 = NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        public double NextGaussian(double mean, double standardDeviation)
        {
            if (standardDeviation < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(standardDeviation), standardDeviation, "Can't be negative.");
            }

            return mean + standardDeviation * NextGaussian();
        }

        /// <summary>
        /// Poisson count with the given mean: exact by Knuth's method for small means, a rounded
        /// normal above, where the exact method gets slow and the two agree closely.
        /// </summary>
        public int NextPoisson(double mean)
        {
            if (mean < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(mean), mean, "Can't be negative.");
            }
            if (mean > 30)
            {
                return Math.Max(0, (int)Math.Round(NextGaussian(mean, Math.Sqrt(mean))));
            }

            var limit = Math.Exp(-mean);
            var count = 0;
            var product = NextDouble();
            while (product > limit)
            {
                count++;
                product *= NextDouble();
            }
            return count;
        }

        public double NextExponential(double mean)
        {
            if (mean <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(mean), mean, "Must be positive.");
            }

            return -mean * Math.Log(1.0 - NextDouble());
        }

        // Rejection sampling: a plain modulo would favour low values whenever the bound
        // doesn't divide 2^64.
        private ulong NextBounded(ulong bound)
        {
            var threshold = (0UL - bound) % bound;
            while (true)
            {
                var value = NextULong();
                if (value >= threshold)
                {
                    return value % bound;
                }
            }
        }

        private static ulong RotateLeft(ulong x, int k) => (x << k) | (x >> (64 - k));
    }
}
