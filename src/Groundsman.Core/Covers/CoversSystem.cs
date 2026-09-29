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

        private GameTime _hour;

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

        /// <summary>
        /// Playing conditions that take a strip out of the player's hands for the hour: true to
        /// cover it, false to leave it open, null for the player's own cover.
        /// </summary>
        public System.Func<StripId, GameTime, bool?>? Override { get; set; }

        public bool IsCovered(StripId strip) => Override?.Invoke(strip, _hour) ?? _covered[strip.Number - 1];

        /// <summary>Whether the player has a cover on the strip, regardless of any override.</summary>
        public bool HasPlayerCover(StripId strip) => _covered[strip.Number - 1];

        /// <summary>Sets the hour the override is judged at, for views between ticks.</summary>
        public void At(GameTime hour) => _hour = hour;

        public CoverOrder OrderFor(StripId strip) => _orders[strip.Number - 1];

        public void Order(StripId strip, CoverOrder order) => _orders[strip.Number - 1] = order;

        public void RunHour(GameTime hour)
        {
            _hour = hour;
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
