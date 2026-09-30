using System.Text;
using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Match;
using Groundsman.Core.Strips;
using Groundsman.Harness.Policies;

namespace Groundsman.Harness;

/// <summary>
/// How a strip plays when it's used again: a four-day match on a strip reused after a gap,
/// against the same match on a fresh strip, all prepared by the book. For tuning lasting wear
/// and the ends' regrowth against the phase 4 target: reused within about three weeks plays
/// noticeably worse, and a third use in a season is worse than the first.
/// </summary>
public sealed class ReuseReport
{
    public static readonly int[] GapDays = { 11, 14, 21, 28, 42 };

    private static readonly StripId Reused = new StripId(6);
    private static readonly StripId Fresh = new StripId(10);
    private static readonly StripId Elsewhere = new StripId(2);

    private ReuseReport(IReadOnlyList<Row> rows)
    {
        Rows = rows;
    }

    public IReadOnlyList<Row> Rows { get; }

    public static ReuseReport Run(GameContent content, DateTime seasonStart, DateTime firstMatch, int seasons)
    {
        var format = content.Formats.First(f => f.Days > 1);
        var opponent = content.Teams.Teams.First(t => t.Id != content.Teams.HomeId);
        Fixture At(DateTime start, StripId strip) => new Fixture(start, format, strip, opponent);

        var rows = new List<Row>();
        foreach (var gap in GapDays)
        {
            var second = firstMatch.AddDays(format.Days - 1 + gap);
            rows.Add(Compare(
                $"Reused after {gap} days",
                new[] { At(firstMatch, Reused), At(second, Reused) },
                new[] { At(firstMatch, Reused), At(second, Fresh) }));
        }

        var gapForThird = 21;
        var middle = firstMatch.AddDays(format.Days - 1 + gapForThird);
        var third = middle.AddDays(format.Days - 1 + gapForThird);
        rows.Add(Compare(
            $"Third use, {gapForThird} days apart",
            new[] { At(firstMatch, Reused), At(middle, Reused), At(third, Reused) },
            new[] { At(firstMatch, Elsewhere), At(middle, Fresh), At(third, Reused) }));
        return new ReuseReport(rows);

        Row Compare(string name, Fixture[] reused, Fixture[] fresh)
        {
            var a = Last(reused);
            var b = Last(fresh);
            return new Row(name, a.Consistency, b.Consistency, a.Carry, b.Carry, a.SatisfactoryOrBetter, b.SatisfactoryOrBetter, a.VeryGood, b.VeryGood);
        }

        (double Consistency, double Carry, double SatisfactoryOrBetter, double VeryGood) Last(Fixture[] fixtures)
        {
            var season = new SeasonSettings(seasonStart, fixtures);
            var plan = fixtures.ToDictionary(f => f.Id, f => f.PresetStrip!.Value);
            var results = new MatchResult[seasons];
            Parallel.For(0, seasons, i =>
            {
                var seed = (ulong)(i + 1);
                results[i] = SeasonRunner.Run(content, season, new ScoringSettings(24, 30, 22), new ByTheBookPolicy(plan), seed)[^1];
            });
            var rated = results.Where(r => r.Grade != null).ToList();
            return (
                results.Average(r => r.Consistency),
                results.Average(r => r.Carry),
                rated.Count == 0 ? 0 : rated.Count(r => r.Grade <= PitchGrade.Satisfactory) / (double)rated.Count,
                rated.Count == 0 ? 0 : rated.Count(r => r.Grade == PitchGrade.VeryGood) / (double)rated.Count);
        }
    }

    public string SummaryTable()
    {
        var text = new StringBuilder("Last match                  Consistency (fresh)  Carry (fresh)  Sat. or better (fresh)  Very good (fresh)\n");
        foreach (var r in Rows)
        {
            text.Append($"{r.Name,-26}  {r.Consistency,5:0.0} ({r.FreshConsistency,4:0.0})     {r.Carry,4:0.0} ({r.FreshCarry,4:0.0})   {r.SatisfactoryOrBetter,6:P0} ({r.FreshSatisfactoryOrBetter,4:P0})     {r.VeryGood,6:P0} ({r.FreshVeryGood,4:P0})\n");
        }
        return text.ToString();
    }

    public sealed record Row(
        string Name,
        double Consistency,
        double FreshConsistency,
        double Carry,
        double FreshCarry,
        double SatisfactoryOrBetter,
        double FreshSatisfactoryOrBetter,
        double VeryGood,
        double FreshVeryGood);
}
