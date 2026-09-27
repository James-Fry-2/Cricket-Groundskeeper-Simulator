namespace Groundsman.Core.Randomness
{
    public readonly struct RandomState
    {
        public RandomState(ulong s0, ulong s1, ulong s2, ulong s3)
        {
            S0 = s0;
            S1 = s1;
            S2 = s2;
            S3 = s3;
        }

        public ulong S0 { get; }
        public ulong S1 { get; }
        public ulong S2 { get; }
        public ulong S3 { get; }

        public bool IsZero => (S0 | S1 | S2 | S3) == 0;
    }
}
