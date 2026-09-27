using System.Globalization;

namespace Groundsman.Harness;

internal static class Csv
{
    public static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
