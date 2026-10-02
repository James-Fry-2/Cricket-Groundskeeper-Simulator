using System.Collections.Generic;

namespace Groundsman.Core.Pressures
{
    public sealed class StakeholderView
    {
        public StakeholderView(Stakeholder stakeholder, double satisfaction, IReadOnlyList<SatisfactionChange> changes)
        {
            Stakeholder = stakeholder;
            Satisfaction = satisfaction;
            Changes = changes;
        }

        public Stakeholder Stakeholder { get; }

        /// <summary>0 to 100.</summary>
        public double Satisfaction { get; }

        /// <summary>Every change this season, oldest first.</summary>
        public IReadOnlyList<SatisfactionChange> Changes { get; }
    }
}
