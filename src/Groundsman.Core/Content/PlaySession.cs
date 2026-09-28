namespace Groundsman.Core.Content
{
    /// <summary>Hours of play in one session, from Start up to End.</summary>
    public sealed class PlaySession
    {
        public PlaySession(int start, int end)
        {
            Start = start;
            End = end;
        }

        public int Start { get; }
        public int End { get; }
    }
}
