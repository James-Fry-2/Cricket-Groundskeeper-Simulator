using System;
using System.Collections.Generic;

namespace Groundsman.Core.Randomness
{
    public sealed class RandomStreams
    {
        public static IReadOnlyList<RandomStream> All { get; } = new[]
        {
            RandomStream.Weather,
            RandomStream.Forecast,
            RandomStream.Readings,
            RandomStream.Match,
            RandomStream.Events,
        };

        private readonly Dictionary<RandomStream, RandomSource> _sources = new Dictionary<RandomStream, RandomSource>();

        public RandomStreams(ulong seed)
        {
            foreach (var stream in All)
            {
                _sources[stream] = new RandomSource(DeriveState(seed, stream));
            }
        }

        private RandomStreams(IReadOnlyList<RandomState> states)
        {
            if (states.Count != All.Count)
            {
                throw new ArgumentException($"Expected {All.Count} stream states, got {states.Count}.", nameof(states));
            }

            for (var i = 0; i < All.Count; i++)
            {
                _sources[All[i]] = new RandomSource(states[i]);
            }
        }

        public RandomSource Get(RandomStream stream) => _sources[stream];

        /// <summary>States in the order of <see cref="All"/>, for saves.</summary>
        public RandomState[] CaptureState()
        {
            var states = new RandomState[All.Count];
            for (var i = 0; i < All.Count; i++)
            {
                states[i] = _sources[All[i]].State;
            }
            return states;
        }

        public static RandomStreams FromState(IReadOnlyList<RandomState> states) => new RandomStreams(states);

        // Each stream's seed comes from the master seed and the stream's own id, not from its
        // position or from draws on other streams, so adding a stream leaves the rest unchanged.
        private static RandomState DeriveState(ulong seed, RandomStream stream)
        {
            var x = SplitMix64(ref seed) ^ SplitMix64Mix((ulong)stream);
            return new RandomState(SplitMix64(ref x), SplitMix64(ref x), SplitMix64(ref x), SplitMix64(ref x));
        }

        private static ulong SplitMix64(ref ulong state)
        {
            state += 0x9E3779B97F4A7C15UL;
            return SplitMix64Mix(state);
        }

        private static ulong SplitMix64Mix(ulong z)
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
