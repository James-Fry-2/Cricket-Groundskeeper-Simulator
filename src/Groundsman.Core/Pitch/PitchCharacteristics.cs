namespace Groundsman.Core.Pitch
{
    /// <summary>How a strip plays, each from 0 to 10. Truth: only the match engine and the harness see it.</summary>
    public sealed class PitchCharacteristics
    {
        public PitchCharacteristics(double pace, double bounce, double consistency, double carry, double seam, double spin, double cracking)
        {
            Pace = pace;
            Bounce = bounce;
            Consistency = consistency;
            Carry = carry;
            Seam = seam;
            Spin = spin;
            Cracking = cracking;
        }

        public double Pace { get; }
        public double Bounce { get; }

        /// <summary>10 is perfectly true; low values mean the ball keeps low or rears.</summary>
        public double Consistency { get; }

        public double Carry { get; }
        public double Seam { get; }
        public double Spin { get; }
        public double Cracking { get; }
    }
}
