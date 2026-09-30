using Groundsman.Core;
using Groundsman.Core.Readings;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;
using Groundsman.Harness.Policies;

namespace Groundsman.Tests.Harness;

public class ByTheBookPolicyTests
{
    private static readonly Fixture Match = new Fixture(new DateTime(2027, 6, 20), TestFormats.FourDay, new StripId(6), TestTeams.Opponent);

    private static Game GameAt(GameTime start, ulong seed = 1) =>
        new Game(new GameSetup(TestContent.Content, start, new[] { Match }, seed));

    private static double Midpoint(ValueRange range) => (range.Low + range.High) / 2;

    [Fact]
    public void Does_nothing_more_than_ten_days_out()
    {
        var game = GameAt(new GameTime(2027, 6, 5, 7));

        new ByTheBookPolicy().PlayTurn(game);

        Assert.All(game.View.Staff, s => Assert.Equal(s.HoursPerDay, s.HoursLeft));
    }

    [Fact]
    public void The_deputy_cores_the_match_strip_each_morning_from_a_week_out()
    {
        var game = GameAt(new GameTime(2027, 6, 13, 7));

        new ByTheBookPolicy().PlayTurn(game);

        var core = game.View.Strips[5].SubsurfaceMoisture;
        Assert.NotNull(core);
        Assert.Equal(ReadingSource.SoilCore, core!.Source);
        Assert.Equal("sam", core.TakenBy.Value);
        Assert.Equal(ReadingSource.Feel, game.View.Strips[5].SurfaceMoisture?.Source ?? ReadingSource.Feel);
    }

    [Fact]
    public void Also_probes_the_surface_in_the_final_days()
    {
        var game = GameAt(new GameTime(2027, 6, 17, 7));

        new ByTheBookPolicy().PlayTurn(game);

        Assert.NotNull(game.View.Strips[5].SurfaceMoisture);
        Assert.NotNull(game.View.Strips[5].SubsurfaceMoisture);
    }

    [Fact]
    public void Waters_when_the_core_reads_dry_and_rain_is_unlikely_and_not_otherwise()
    {
        var watered = 0;
        var held = 0;
        for (ulong seed = 1; seed <= 40; seed++)
        {
            var game = GameAt(new GameTime(2027, 6, 15, 7), seed);
            if (seed % 2 == 0)
            {
                game.Square.Get(Match.PresetStrip!.Value).SubsurfaceMoisture = 18;
            }
            new ByTheBookPolicy().PlayTurn(game);

            var view = game.View;
            var depth = Midpoint(view.Strips[5].SubsurfaceMoistureNow!.Value);
            var shouldWater = depth < ByTheBookPolicy.WaterBelowDepth && view.Forecast[0].ChanceOfRain < ByTheBookPolicy.RainLikely;
            Assert.Equal(shouldWater, view.Strips[5].WateringQueued);
            watered += shouldWater ? 1 : 0;
            held += shouldWater ? 0 : 1;
        }

        Assert.True(watered >= 5 && held >= 5, $"watered {watered}, held {held}");
    }

    [Fact]
    public void Covers_in_the_final_days_when_rain_threatens_and_uncovers_when_it_doesnt()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var game = GameAt(new GameTime(2027, 6, 18, 7), seed);
            new ByTheBookPolicy().PlayTurn(game);

            var view = game.View;
            var threat = view.Forecast[0].ChanceOfRain >= ByTheBookPolicy.CoverAtChance;
            Assert.Equal(threat, view.Strips[5].CoverOrder == CoverOrder.Cover);
        }
    }

    [Fact]
    public void Leaves_a_dry_strip_open_to_rain_early_in_the_build_up()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var game = GameAt(new GameTime(2027, 6, 14, 7), seed);
            game.Square.Get(Match.PresetStrip!.Value).SubsurfaceMoisture = 16;

            new ByTheBookPolicy().PlayTurn(game);

            var view = game.View;
            var heavy = view.Forecast[0].ChanceOfRain >= ByTheBookPolicy.RainLikely && view.Forecast[0].Rain.High >= ByTheBookPolicy.HeavyRainMm;
            Assert.Equal(heavy, view.Strips[5].CoverOrder == CoverOrder.Cover);
        }
    }

    [Fact]
    public void Only_ever_works_on_the_match_strip()
    {
        var game = GameAt(new GameTime(2027, 6, 10, 7));
        var policy = new ByTheBookPolicy();

        while (game.View.Now.Date < Match.Start)
        {
            policy.PlayTurn(game);
            Assert.All(game.View.Strips.Where(s => s.Id != Match.PresetStrip!.Value), s =>
            {
                Assert.False(s.WateringQueued);
                Assert.Equal(CoverOrder.None, s.CoverOrder);
                Assert.Null(s.SubsurfaceMoisture);
            });
            game.Advance();
        }
    }

    private static Game BuildUp(ulong seed, Action<Game>? eachTurn = null, params Fixture[] fixtures)
    {
        var list = fixtures.Length == 0 ? new[] { Match } : fixtures;
        var game = new Game(new GameSetup(TestContent.Content, new GameTime(2027, 6, 8, 7), list, seed));
        var policy = new ByTheBookPolicy();
        while (game.View.Now.Date < list[^1].Start)
        {
            policy.PlayTurn(game);
            eachTurn?.Invoke(game);
            game.Advance();
        }
        return game;
    }

    [Fact]
    public void Mows_the_strip_down_to_six_to_eight_mm_by_match_day_without_scalping()
    {
        for (ulong seed = 1; seed <= 10; seed++)
        {
            var coverBefore = 0.0;
            var cuts = new List<double>();
            var game = BuildUp(seed, g =>
            {
                var cut = g.View.Strips[5].LastMown;
                if (g.View.Strips[5].MowingQueued && (cuts.Count == 0 || cut!.HeightMm != cuts[^1]))
                {
                    cuts.Add(cut!.HeightMm);
                }
                coverBefore = Math.Max(coverBefore, g.Inspect().Strips[5].GrassCover);
            });

            var truth = game.Inspect().Strips[5];
            Assert.InRange(truth.GrassHeightMm, 6, 9);
            Assert.True(cuts.Count >= 4, $"seed {seed}: {cuts.Count} cuts");
            Assert.Equal(cuts.OrderByDescending(c => c), cuts);
            Assert.True(truth.GrassCover > coverBefore - 5, $"seed {seed}: cover {coverBefore:0} to {truth.GrassCover:0}");
        }
    }

    [Fact]
    public void Rolls_only_on_a_moist_surface_light_first_and_heavy_last_then_a_light_finish()
    {
        for (ulong seed = 1; seed <= 10; seed++)
        {
            var rolls = new List<(int DaysOut, string Roller)>();
            var game = BuildUp(seed, g =>
            {
                var strip = g.View.Strips[5];
                if (strip.RollingQueued)
                {
                    rolls.Add(((Match.Start - g.View.Now.Date).Days, strip.LastRolled!.RollerId));
                    Assert.False(strip.WateringQueued);
                    Assert.Equal(g.View.Now.Date, strip.SurfaceMoisture!.TakenAt.Date);
                    Assert.NotEqual("wet", strip.SurfaceMoisture.Word);
                    Assert.NotEqual("dry", strip.SurfaceMoisture.Word);
                }
            });

            Assert.All(rolls, r => Assert.Equal(ByTheBookPolicy.RollingFor(r.DaysOut).Roller, r.Roller));
            Assert.Equal(rolls.Count, rolls.Select(r => r.DaysOut).Distinct().Count());
        }
    }

    [Fact]
    public void Rolling_builds_compaction_without_damaging_the_structure()
    {
        var totalRolls = 0;
        for (ulong seed = 1; seed <= 10; seed++)
        {
            var rolls = 0;
            var truth = BuildUp(seed, g => rolls += g.View.Strips[5].RollingQueued ? 1 : 0).Inspect().Strips[5];

            totalRolls += rolls;
            if (rolls > 0)
            {
                Assert.True(truth.Compaction > TestRolling.Compaction.StartingCompaction, $"seed {seed}: {rolls} rolls left compaction at {truth.Compaction:0.000}");
            }
            Assert.True(truth.StructureDamage < 0.05, $"seed {seed}: damage {truth.StructureDamage:0.000}");
        }
        Assert.True(totalRolls >= 10, $"{totalRolls} rolls over 10 build-ups");
    }

    [Fact]
    public void Waters_a_surface_too_dry_to_roll_early_in_the_build_up_only()
    {
        var watered = 0;
        for (ulong seed = 1; seed <= 20; seed++)
        {
            foreach (var day in new[] { 14, 17 })
            {
                var game = GameAt(new GameTime(2027, 6, day, 7), seed);
                game.Square.Get(Match.PresetStrip!.Value).SurfaceMoisture = 5;
                game.Square.Get(Match.PresetStrip!.Value).SubsurfaceMoisture = 28;

                new ByTheBookPolicy().PlayTurn(game);

                var view = game.View;
                var strip = view.Strips[5];
                var daysOut = (Match.Start - view.Now.Date).Days;
                var dryFeel = strip.SurfaceMoisture?.Word == "dry";
                var expected = daysOut >= ByTheBookPolicy.WetToRollFromDaysOut && dryFeel && view.Forecast[0].ChanceOfRain < ByTheBookPolicy.RainLikely;
                Assert.Equal(expected, strip.WateringQueued);
                Assert.False(strip.RollingQueued);
                watered += expected ? 1 : 0;
            }
        }
        Assert.InRange(watered, 5, 20);
    }

    [Fact]
    public void Repairs_the_ends_once_the_day_after_a_match()
    {
        var oneDay = new Fixture(new DateTime(2027, 6, 12), TestFormats.OneDay, new StripId(3), TestTeams.Opponent);
        var later = new Fixture(new DateTime(2027, 6, 20), TestFormats.OneDay, new StripId(6), TestTeams.Opponent);
        var repairs = new List<DateTime>();

        BuildUp(1, g =>
        {
            if (g.View.Strips[2].RepairQueued)
            {
                repairs.Add(g.View.Now.Date);
            }
        }, oneDay, later);

        Assert.Equal(new[] { new DateTime(2027, 6, 13) }, repairs.Distinct());
    }

    [Fact]
    public void Prepares_the_next_fixture_while_another_is_being_played()
    {
        var fourDay = new Fixture(new DateTime(2027, 6, 10), TestFormats.FourDay, new StripId(3), TestTeams.Opponent);
        var next = new Fixture(new DateTime(2027, 6, 18), TestFormats.OneDay, new StripId(6), TestTeams.Opponent);
        var worked = false;

        BuildUp(1, g =>
        {
            var date = g.View.Now.Date;
            if (date >= fourDay.Start && date <= fourDay.End && (g.View.Strips[5].MowingQueued || g.View.Strips[5].RollingQueued))
            {
                worked = true;
            }
        }, fourDay, next);

        Assert.True(worked);
    }

    [Fact]
    public void Follows_its_strip_plan_until_each_lock()
    {
        var open = new Fixture(new DateTime(2027, 6, 20), TestFormats.OneDay, null, TestTeams.Opponent);
        var game = new Game(new GameSetup(TestContent.Content, new GameTime(2027, 6, 1, 7), new[] { open }, 1));
        var plan = new Dictionary<string, StripId> { [open.Id] = new StripId(11) };

        new ByTheBookPolicy(plan).PlayTurn(game);

        Assert.Equal(new StripId(11), game.View.Fixtures[0].Strip);
    }

    [Fact]
    public void The_original_plan_covers_every_shipped_fixture_without_clashes()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "content");
        var formats = Groundsman.Core.Content.ContentParser.ParseFormats(File.ReadAllText(Path.Combine(directory, "formats.json")));
        var teams = Groundsman.Core.Content.ContentParser.ParseTeams(File.ReadAllText(Path.Combine(directory, "teams.json")));
        var season = Groundsman.Core.Content.ContentParser.ParseSeason(File.ReadAllText(Path.Combine(directory, "season.json")), formats, teams);
        var game = new Game(new GameSetup(TestContent.Content, new GameTime(2027, 3, 25, 7), season.Fixtures, 1));

        foreach (var fixture in season.Fixtures)
        {
            Assert.True(game.Submit(new Groundsman.Core.Commands.AssignStrip(fixture.Id, SeasonPlans.Original[fixture.Id])).Accepted, fixture.Id);
        }
    }
}
