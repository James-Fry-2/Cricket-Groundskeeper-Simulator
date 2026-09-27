namespace Groundsman.Core.Content
{
    /// <summary>A word for how a strip feels, covering surface moisture from From up to To, in %.</summary>
    public sealed class FeelBand
    {
        public FeelBand(string word, double from, double to)
        {
            Word = word;
            From = from;
            To = to;
        }

        public string Word { get; }
        public double From { get; }
        public double To { get; }
    }
}
