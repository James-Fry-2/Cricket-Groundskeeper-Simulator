using System.Text;
using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Pitch;
using Groundsman.Core.Time;

namespace Groundsman.Harness;

/// <summary>
/// How an untouched strip would play each morning through the season, over many seeds, for
/// tuning pitch.json by eye.
/// </summary>
public sealed class PitchReport
{
    private PitchReport(string csv, IReadOnlyList<MonthAverage> months)
    {
        Csv = csv;
        Months = months;
    }

    public string Csv { get; }
    public IReadOnlyList<MonthAverage> Months { get; }

    public static PitchReport Run(GameContent content, SeasonSettings season, int seasons, DateTime end)
    {
        var morning = content.Calendar.MorningHour;
        var csv = new StringBuilder("seed,date,pace,bounce,consistency,carry,seam,spin,cracking\n");
        var samples = new List<(int Month, PitchCharacteristics Pitch)>();

        for (var seed = 1UL; seed <= (ulong)seasons; seed++)
        {
            var game = new Game(new GameSetup(content, GameTime.OnDate(season.Start, morning), Array.Empty<Fixture>(), seed));
            while (game.View.Now.Date <= end)
            {
                if (game.View.Now.Hour == morning && game.View.Now.Date >= new DateTime(season.Start.Year, 4, 1))
                {
                    var p = game.Inspect().Strips[0].Pitch;
                    samples.Add((game.View.Now.Date.Month, p));
                    csv.Append($"{seed},{game.View.Now.Date:yyyy-MM-dd},{N(p.Pace)},{N(p.Bounce)},{N(p.Consistency)},{N(p.Carry)},{N(p.Seam)},{N(p.Spin)},{N(p.Cracking)}\n");
                }
                game.Advance();
            }
        }

        var months = samples
            .GroupBy(s => s.Month)
            .OrderBy(g => g.Key)
            .Select(g => new MonthAverage(
                g.Key,
                g.Average(s => s.Pitch.Pace),
                g.Average(s => s.Pitch.Bounce),
                g.Average(s => s.Pitch.Consistency),
                g.Average(s => s.Pitch.Carry),
                g.Average(s => s.Pitch.Seam),
                g.Average(s => s.Pitch.Spin),
                g.Average(s => s.Pitch.Cracking)))
            .ToList();

        return new PitchReport(csv.ToString(), months);
    }

    public string SummaryTable()
    {
        var text = new StringBuilder("Month   Pace  Bounce  Consistency  Carry  Seam  Spin  Cracking\n");
        foreach (var m in Months)
        {
            text.Append($"{m.Month,5}  {m.Pace,5:0.0}  {m.Bounce,6:0.0}  {m.Consistency,11:0.0}  {m.Carry,5:0.0}  {m.Seam,4:0.0}  {m.Spin,4:0.0}  {m.Cracking,8:0.0}\n");
        }
        return text.ToString();
    }

    private static string N(double value) => Harness.Csv.Number(value);

    public sealed record MonthAverage(int Month, double Pace, double Bounce, double Consistency, double Carry, double Seam, double Spin, double Cracking);
}
