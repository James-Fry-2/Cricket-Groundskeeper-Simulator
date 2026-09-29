using Groundsman.Core;
using Groundsman.Core.Content;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;

namespace Groundsman.Tests.Match;

public class CommentaryTests
{
    private static readonly StripId MatchStrip = new StripId(6);

    private static Game Played(FormatSettings format, Action<StripState> setUp, ulong seed = 1, int days = 1)
    {
        var fixture = new Fixture(new DateTime(2027, 6, 10), format, MatchStrip, TestTeams.Opponent);
        var game = new Game(new GameSetup(TestContent.Content, new GameTime(2027, 6, 10, 8), new[] { fixture }, seed));
        var strip = game.Square.Get(MatchStrip);
        strip.Compaction = 0.8;
        strip.GrassHeightMm = 7;
        setUp(strip);
        while (game.View.Now.Date < new DateTime(2027, 6, 10).AddDays(days))
        {
            game.Advance();
        }
        return game;
    }

    private static IReadOnlyList<string> Lines(Game game) => game.View.LatestMatch!.Commentary.Select(c => c.Text).ToList();

    [Fact]
    public void A_green_damp_strip_brings_seam_talk_that_names_the_grass_or_the_damp()
    {
        var found = 0;
        for (ulong seed = 1; seed <= 10; seed++)
        {
            var lines = Lines(Played(TestFormats.FourDay, s => { s.GrassHeightMm = 12; s.GrassCover = 95; s.SurfaceMoisture = 27; }, seed));
            var seam = lines.FirstOrDefault(l => l.StartsWith("SEAM"));
            if (seam != null)
            {
                found++;
                Assert.True(seam.Contains("cause:grass 12mm") || seam.Contains("cause:damp") || seam.Contains("cause:grass"), seam);
                Assert.Contains("Test Visitors", seam + string.Join(" ", lines));
            }
        }

        Assert.True(found >= 5, $"Seam talk in only {found} of 10 matches");
    }

    [Fact]
    public void A_strip_rolled_when_too_wet_breaks_up_and_the_commentary_says_why()
    {
        var lines = Lines(Played(TestFormats.FourDay, s => s.StructureDamage = 0.8));

        var uneven = Assert.Single(lines, l => l.StartsWith("UNEVEN"));
        Assert.Contains("cause:structureDamage", uneven);
    }

    [Fact]
    public void Deep_footholes_are_named_at_an_end()
    {
        var lines = Lines(Played(TestFormats.FourDay, s => s.Footholes = 0.35));

        var footholes = Assert.Single(lines, l => l.StartsWith("FOOTHOLES"));
        Assert.True(footholes.Contains("North End") || footholes.Contains("South End"), footholes);
        Assert.Contains("cause:footholes", footholes);
    }

    [Fact]
    public void A_true_dry_hard_strip_is_praised_as_true()
    {
        var lines = Lines(Played(TestFormats.FourDay, s => { s.Compaction = 0.95; s.SurfaceMoisture = 16; s.GrassHeightMm = 5; s.GrassCover = 60; }));

        Assert.Contains(lines, l => l.StartsWith("TRUE") && l.Contains("cause:hard"));
    }

    [Fact]
    public void Every_break_gets_a_line_with_the_score_at_the_hour_the_break_starts()
    {
        var game = Played(TestFormats.FourDay, _ => { }, seed: 2);
        var breaks = game.View.LatestMatch!.Commentary.Where(c => c.EventId == null).ToList();

        for (var i = 0; i < TestFormats.FourDay.BreakNames.Count; i++)
        {
            var line = Assert.Single(breaks, b => b.Text.StartsWith(TestFormats.FourDay.BreakNames[i] + ": "));
            Assert.Equal(TestFormats.FourDay.Sessions[i].End, line.Hour.Hour);
            Assert.Contains(line.Text, l => char.IsDigit(l));
        }
    }

    [Fact]
    public void Break_lines_show_at_most_the_last_two_innings()
    {
        var game = Played(TestFormats.FourDay, _ => { }, seed: 3, days: 4);

        Assert.All(game.View.LatestMatch!.Commentary.Where(c => c.EventId == null), c => Assert.True(c.Text.Count(ch => ch == ';') <= 1, c.Text));
    }

    [Fact]
    public void A_collapse_to_a_seam_attack_is_put_down_to_seam_or_bounce_not_a_dry_surface()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var game = Played(TestFormats.OneDay, s => { s.GrassHeightMm = 14; s.GrassCover = 95; s.SurfaceMoisture = 26; s.StructureDamage = 0.2; }, seed);
            var collapse = game.View.LatestMatch!.Commentary.FirstOrDefault(c => c.EventId == "collapse");
            if (collapse != null)
            {
                Assert.NotEqual("dry", collapse.CauseId);
            }
        }
    }

    [Fact]
    public void Each_event_is_raised_at_most_once_a_match_and_rain_once_a_spell()
    {
        for (ulong seed = 1; seed <= 15; seed++)
        {
            var game = Played(TestFormats.FourDay, s => s.StructureDamage = 0.5, seed, days: 4);
            var commentary = game.View.LatestMatch!.Commentary;

            var events = commentary.Where(c => c.EventId != null && c.EventId != "rain").Select(c => c.EventId).ToList();
            Assert.Equal(events.Count, events.Distinct().Count());

            var rainHours = commentary.Where(c => c.EventId == "rain").Select(c => c.Hour).ToList();
            var hours = game.Matches.Latest!.Hours;
            foreach (var hour in rainHours)
            {
                var index = hours.FindIndex(h => h.Hour == hour);
                Assert.True(index == 0 || !hours[index - 1].Rained || hours[index - 1].Hour.AddHours(1) != hour);
            }
        }
    }

    [Fact]
    public void The_same_seed_gives_the_same_commentary()
    {
        Assert.Equal(Lines(Played(TestFormats.FourDay, _ => { }, 5, 4)), Lines(Played(TestFormats.FourDay, _ => { }, 5, 4)));
    }
}
