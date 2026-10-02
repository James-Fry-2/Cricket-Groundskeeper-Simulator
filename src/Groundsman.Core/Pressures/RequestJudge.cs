using System;
using System.Linq;
using Groundsman.Core.Content;
using Groundsman.Core.Match;

namespace Groundsman.Core.Pressures
{
    /// <summary>
    /// Whether a match's pitch did what was asked, judged on its hours of dry play the way the
    /// players would remember it: day one for the seamers, the last day for the spinners, the
    /// whole match for pace and a flat deck.
    /// </summary>
    internal sealed class RequestJudge
    {
        private readonly StakeholderSettings _settings;

        public RequestJudge(StakeholderSettings settings)
        {
            _settings = settings;
        }

        public RequestStatus Judge(RequestKind kind, MatchState match)
        {
            if (kind == RequestKind.LastsFourDays)
            {
                return ReachedDayFour(match) ? RequestStatus.Delivered : RequestStatus.NotDelivered;
            }

            var played = match.Hours.Where(h => !h.Rained).ToList();
            if (played.Count == 0)
            {
                return RequestStatus.Spoiled;
            }

            bool delivered;
            switch (kind)
            {
                case RequestKind.Green:
                    var dayOne = played.Where(h => h.Hour.Date == match.Fixture.Start).ToList();
                    if (dayOne.Count == 0)
                    {
                        return RequestStatus.Spoiled;
                    }
                    delivered = dayOne.Average(h => h.Pitch.Seam) >= _settings.GreenSeamDayOne;
                    break;
                case RequestKind.Turning:
                    var lastDay = played.Max(h => h.Hour.Date);
                    delivered = lastDay > match.Fixture.Start
                        && played.Where(h => h.Hour.Date == lastDay).Average(h => h.Pitch.Spin) >= _settings.TurningSpinLastDay;
                    break;
                case RequestKind.Pace:
                    delivered = played.Average(h => h.Pitch.Carry) >= _settings.PaceCarry
                        && played.Average(h => h.Pitch.Consistency) >= _settings.TrueConsistency;
                    break;
                default:
                    delivered = played.Average(h => Math.Max(h.Pitch.Seam, h.Pitch.Spin)) < _settings.FlatMovementBelow
                        && played.Average(h => h.Pitch.Consistency) >= _settings.TrueConsistency;
                    break;
            }
            return delivered ? RequestStatus.Delivered : RequestStatus.NotDelivered;
        }

        /// <summary>The match was still on when day four came, even if rain then took it.</summary>
        public bool ReachedDayFour(MatchState match) =>
            match.Hours.Any(h => h.Hour.Date >= match.Fixture.Start.AddDays(3));
    }
}
