using System;
using System.Collections.Generic;
using System.Linq;
using Groundsman.Core.Content;

namespace Groundsman.Core.Match
{
    /// <summary>Demerits over a rolling window of years, as the ICC keeps them.</summary>
    internal sealed class DemeritLedger
    {
        private readonly RatingSettings _settings;
        private readonly List<RatingView> _ratings = new List<RatingView>();

        public DemeritLedger(RatingSettings settings)
        {
            _settings = settings;
        }

        public IReadOnlyList<RatingView> Ratings => _ratings;

        public void Add(RatingView rating) => _ratings.Add(rating);

        public int Active(DateTime today) =>
            _ratings.Where(r => r.RatedOn > today.AddYears(-_settings.WindowYears)).Sum(r => r.Demerits);

        public bool Banned(DateTime today) => Active(today) >= _settings.BanAt;
    }
}
