using System;
using Groundsman.Core.Content;
using Groundsman.Core.Pitch;
using Groundsman.Core.Randomness;

namespace Groundsman.Core.Match
{
    public readonly struct BlockResult
    {
        public BlockResult(double overs, double runs, int wickets)
        {
            Overs = overs;
            Runs = runs;
            Wickets = wickets;
        }

        public double Overs { get; }
        public double Runs { get; }
        public int Wickets { get; }
    }

    /// <summary>
    /// Resolves a block of overs from the pitch as it is, the bowling attack's make-up and the
    /// two sides' strengths. Seam helps seamers and spin helps spinners; uneven bounce helps
    /// everyone bowling and costs runs; a dead pitch is hard to score on and hard to take
    /// wickets on.
    /// </summary>
    internal sealed class MatchModel
    {
        private readonly MatchSettings _settings;
        private readonly RandomSource _random;

        public MatchModel(MatchSettings settings, RandomSource random)
        {
            _settings = settings;
            _random = random;
        }

        public BlockResult PlayBlock(PitchCharacteristics pitch, TeamSettings batting, TeamSettings bowling, FormatSettings format, double overs, int wicketsInHand)
        {
            if (overs <= 0 || wicketsInHand <= 0)
            {
                return new BlockResult(0, 0, 0);
            }

            var edge = _settings.StrengthScale * (bowling.Bowling - batting.Batting) / 50;
            var uneven = 1 - pitch.Consistency / 10;
            var dead = Math.Max(0, 0.5 - pitch.Carry / 10) * 2;
            var attack = bowling.Attack;

            var wicketsPerOver = format.WicketsPerOver * Math.Exp(edge)
                * (1
                    + _settings.SeamWickets * attack.Seam * pitch.Seam / 10
                    + _settings.SpinWickets * attack.Spin * pitch.Spin / 10
                    + _settings.UnevenWickets * uneven)
                * (1 - _settings.DeadWickets * dead);
            var runsPerOver = format.RunsPerOver * Math.Exp(-edge)
                * (1 + _settings.CarryRuns * (pitch.Carry / 10 - 0.5))
                * (1 - _settings.UnevenRuns * uneven)
                * (1 - _settings.DeadRuns * dead);

            // Draw order is fixed and part of the replay contract: wickets, then runs.
            var wickets = _random.NextPoisson(wicketsPerOver * overs);
            var expectedRuns = runsPerOver * overs;
            var runs = Math.Max(0, _random.NextGaussian(expectedRuns, _settings.RunsSpread * runsPerOver * Math.Sqrt(overs)));

            if (wickets >= wicketsInHand)
            {
                // Bowled out part-way. Wickets in a block fall at evenly spread random times,
                // so the k-th of n falls k / (n + 1) of the way through on average. Using k / n
                // would put the last wicket at the block's end whenever k equals n.
                var share = (double)wicketsInHand / (wickets + 1);
                return new BlockResult(overs * share, runs * share, wicketsInHand);
            }
            return new BlockResult(overs, runs, wickets);
        }
    }
}
