using System;
using Groundsman.Core.Content;

namespace Groundsman.Core.Weather
{
    /// <summary>
    /// Maps a day's cloudiness (a standard normal value) to its sunshine fraction through a
    /// logistic curve, with the curve's centre found per month and per wet or dry day so the
    /// long-run average hits the month's normal.
    /// </summary>
    internal sealed class SunshineCalibration
    {
        private const double GridLimit = 6.0;
        private const double GridStep = 0.05;
        private const int BisectionSteps = 50;

        private static readonly double[] GridPoints;
        private static readonly double[] GridWeights;

        private readonly double _spread;
        private readonly double[] _dryCentre = new double[12];
        private readonly double[] _wetCentre = new double[12];

        static SunshineCalibration()
        {
            var count = (int)Math.Round(2 * GridLimit / GridStep) + 1;
            GridPoints = new double[count];
            GridWeights = new double[count];
            var total = 0.0;
            for (var i = 0; i < count; i++)
            {
                var z = -GridLimit + i * GridStep;
                GridPoints[i] = z;
                GridWeights[i] = Math.Exp(-z * z / 2);
                total += GridWeights[i];
            }
            for (var i = 0; i < count; i++)
            {
                GridWeights[i] /= total;
            }
        }

        public SunshineCalibration(ClimateSettings climate)
        {
            _spread = climate.CloudVariability;
            foreach (var month in climate.Months)
            {
                var dry = month.DryDaySunshineFraction(climate.WetDaySunshineFactor);
                _dryCentre[month.Month - 1] = CentreFor(dry);
                _wetCentre[month.Month - 1] = CentreFor(dry * climate.WetDaySunshineFactor);
            }
        }

        public double Fraction(int month, bool wet, double cloudiness)
        {
            var centre = wet ? _wetCentre[month - 1] : _dryCentre[month - 1];
            return double.IsNegativeInfinity(centre) ? 0 : Logistic(centre - _spread * cloudiness);
        }

        private double CentreFor(double targetMean)
        {
            if (targetMean <= 0)
            {
                return double.NegativeInfinity;
            }

            var low = -30.0;
            var high = 30.0;
            for (var step = 0; step < BisectionSteps; step++)
            {
                var mid = (low + high) / 2;
                if (MeanAt(mid) < targetMean)
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }
            return (low + high) / 2;
        }

        private double MeanAt(double centre)
        {
            var mean = 0.0;
            for (var i = 0; i < GridPoints.Length; i++)
            {
                mean += GridWeights[i] * Logistic(centre + _spread * GridPoints[i]);
            }
            return mean;
        }

        private static double Logistic(double x) => 1 / (1 + Math.Exp(-x));
    }
}
