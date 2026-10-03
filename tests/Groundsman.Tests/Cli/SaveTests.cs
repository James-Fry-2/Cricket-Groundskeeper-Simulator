using Groundsman.Cli;
using Groundsman.Core;
using Groundsman.Core.Commands;
using Groundsman.Core.Randomness;
using Groundsman.Core.Readings;
using Groundsman.Core.Staff;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;
using Newtonsoft.Json;

namespace Groundsman.Tests.Cli;

public class SaveTests
{
    private const string Hash = "test-content";
    private static readonly GameTime Start = new GameTime(2027, 5, 1, 7);

    private static readonly Fixture[] Fixtures =
    {
        new Fixture(new DateTime(2027, 5, 20), TestFormats.FourDay, null, TestTeams.Opponent, televised: true),
        new Fixture(new DateTime(2027, 6, 10), TestFormats.OneDay, null, TestTeams.Opponent),
        new Fixture(new DateTime(2027, 6, 20), TestFormats.T20, new StripId(9), TestTeams.Opponent),
    };

    private static readonly Groundsman.Core.Content.GameContent Content = TestContent.WithStakeholders(TestStakeholders.With(1, 1));

    private static string TempFile() => Path.Combine(Path.GetTempPath(), $"groundsman-save-{Guid.NewGuid():N}.json");

    private static RecordingGame NewGame(ulong seed, string? autosave = null) =>
        RecordingGame.Start(Content, Fixtures, Start, seed, Hash, autosave);

    /// <summary>A messy session: up to three random commands a turn, of every kind, legal or not.</summary>
    private static void Play(RecordingGame game, ulong seed, int turns)
    {
        var random = new RandomStreams(seed + 1000).Get(RandomStream.Events);
        for (var turn = 0; turn < turns; turn++)
        {
            for (var i = random.NextInt(4); i > 0; i--)
            {
                var view = game.View;
                var strip = new StripId(random.NextInt(1, 13));
                var by = view.Staff[random.NextInt(view.Staff.Count)].Id;
                IGameCommand command = random.NextInt(11) switch
                {
                    0 => new WaterStrip(strip, by),
                    1 => new TakeReading(strip, by, (ReadingSource)random.NextInt(1, 4)),
                    2 => new CoverStrip(strip, by),
                    3 => new UncoverStrip(strip, by),
                    4 => new MowStrip(strip, random.NextInt(5, 20), by),
                    5 => new RollStrip(strip, view.Rollers[random.NextInt(view.Rollers.Count)].Id, random.NextInt(10, 50), by),
                    6 => new RepairEnds(strip, by),
                    7 => new CleanFootholes(strip, by),
                    8 => new FillFootholes(strip, by),
                    9 => new AssignStrip(view.Fixtures[random.NextInt(view.Fixtures.Count)].Id, strip),
                    _ => view.Requests.Count == 0 ? new WaterStrip(strip, by) : new AnswerRequest(view.Requests[random.NextInt(view.Requests.Count)].Id, random.Chance(0.5)),
                };
                game.Submit(command);
            }
            game.Advance();
        }
    }

    /// <summary>Everything a player could see, and the truth behind it.</summary>
    private static string Digest(RecordingGame game) =>
        JsonConvert.SerializeObject(new { game.Inner.View, Truth = game.Inner.Inspect() }, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

    [Fact]
    public void Every_kind_of_command_survives_the_round_trip()
    {
        var samples = new IGameCommand[]
        {
            new WaterStrip(new StripId(3), new StaffId("sam")),
            new TakeReading(new StripId(4), null, ReadingSource.SoilCore),
            new CoverStrip(new StripId(5)),
            new UncoverStrip(new StripId(5), new StaffId("jo")),
            new MowStrip(new StripId(6), 7.5),
            new RollStrip(new StripId(7), "heavy", 25, new StaffId("sam")),
            new RepairEnds(new StripId(8)),
            new CleanFootholes(new StripId(9)),
            new FillFootholes(new StripId(10)),
            new AssignStrip("2027-05-20", new StripId(11)),
            new AnswerRequest("2027-05-20-captain", accept: true),
        };
        var allTypes = typeof(IGameCommand).Assembly.GetTypes().Where(t => typeof(IGameCommand).IsAssignableFrom(t) && !t.IsInterface);
        Assert.Equal(allTypes.OrderBy(t => t.Name), samples.Select(c => c.GetType()).OrderBy(t => t.Name));

        foreach (var command in samples)
        {
            var saved = JsonConvert.DeserializeObject<SavedCommand>(JsonConvert.SerializeObject(CommandCodec.Encode(command, 4)))!;
            var back = CommandCodec.Decode(saved);
            Assert.Equal(4, saved.Turn);
            Assert.Equal(JsonConvert.SerializeObject(command), JsonConvert.SerializeObject(back));
        }
    }

    [Fact]
    public void A_loaded_save_plays_exactly_as_the_original()
    {
        for (ulong seed = 1; seed <= 12; seed++)
        {
            var original = NewGame(seed);
            Play(original, seed, turns: 70);
            var path = TempFile();
            SaveFile.Write(path, original.Data);

            var restored = SaveFile.Restore(SaveFile.Read(path), Content, Fixtures, Hash);

            Assert.Null(restored.Error);
            Assert.Equal(Digest(original), Digest(restored.Game!));

            original.Advance();
            restored.Game!.Advance();
            Assert.Equal(Digest(original), Digest(restored.Game));
            File.Delete(path);
        }
    }

    [Fact]
    public void Only_accepted_commands_are_recorded_each_with_its_turn()
    {
        var game = NewGame(1);

        game.Submit(new WaterStrip(new StripId(3)));
        game.Submit(new WaterStrip(new StripId(3)));
        game.Advance();
        game.Submit(new MowStrip(new StripId(2), 10));

        Assert.Equal(new[] { (0, "water"), (1, "mow") }, game.Data.Commands.Select(c => (c.Turn, c.Type)));
        Assert.Equal(1, game.Data.Turns);
    }

    [Fact]
    public void It_saves_itself_after_every_command_and_turn()
    {
        var path = TempFile();
        var game = NewGame(2, path);

        game.Submit(new WaterStrip(new StripId(3)));
        Assert.Single(SaveFile.Read(path).Commands);
        game.Advance();
        Assert.Equal(1, SaveFile.Read(path).Turns);
        File.Delete(path);
    }

    [Fact]
    public void A_save_from_other_content_or_an_unknown_version_is_refused()
    {
        var game = NewGame(3);
        Play(game, 3, turns: 10);

        Assert.Contains("content", SaveFile.Restore(game.Data, Content, Fixtures, "other-content").Error);
        var future = game.Data with { SchemaVersion = SaveFile.SchemaVersion + 1 };
        Assert.Contains("version", SaveFile.Restore(future, Content, Fixtures, Hash).Error);
    }

    [Fact]
    public void A_save_that_doesnt_replay_says_where_it_went_wrong()
    {
        var game = NewGame(4);
        game.Advance();
        var tampered = game.Data with { Commands = new List<SavedCommand> { CommandCodec.Encode(new MowStrip(new StripId(1), 99), 0) } };

        var restored = SaveFile.Restore(tampered, Content, Fixtures, Hash);

        Assert.Null(restored.Game);
        Assert.Contains("turn 0", restored.Error);
    }
}
