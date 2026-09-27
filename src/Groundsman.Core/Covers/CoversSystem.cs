using Groundsman.Core.Simulation;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Core.Covers
{
    /// <summary>
    /// Which strips are under covers. Orders given during a turn take effect in the Covers
    /// step of the next hour, before that hour's moisture is worked out.
    /// </summary>
    internal sealed class CoversSystem : IHourlySystem
    {
        private readonly int _count;
        private readonly bool[] _covered;
        private readonly CoverOrder[] _orders;

        public CoversSystem(int count, int stripCount)
        {
            _count = count;
            _covered = new bool[stripCount];
            _orders = new CoverOrder[stripCount];
        }

        public TickStep Step => TickStep.Covers;

        /// <summary>Covers not on a strip and not already promised to one this turn.</summary>
        public int Free
        {
            get
            {
                var inUse = 0;
                for (var i = 0; i < _covered.Length; i++)
                {
                    var willBeCovered = _orders[i] == CoverOrder.Cover || (_covered[i] && _orders[i] != CoverOrder.Uncover);
                    if (willBeCovered)
                    {
                        inUse++;
                    }
                }
                return _count - inUse;
            }
        }

        public bool IsCovered(StripId strip) => _covered[strip.Number - 1];

        public CoverOrder OrderFor(StripId strip) => _orders[strip.Number - 1];

        public void Order(StripId strip, CoverOrder order) => _orders[strip.Number - 1] = order;

        public void RunHour(GameTime hour)
        {
            for (var i = 0; i < _orders.Length; i++)
            {
                if (_orders[i] == CoverOrder.Cover)
                {
                    _covered[i] = true;
                }
                else if (_orders[i] == CoverOrder.Uncover)
                {
                    _covered[i] = false;
                }
                _orders[i] = CoverOrder.None;
            }
        }
    }
}
