using Groundsman.Cli;
using Groundsman.Core;
using Groundsman.Core.Strips;
using Groundsman.Core.Time;
using Spectre.Console.Testing;

namespace Groundsman.Tests.Cli;

public class PlaytestFolderTests
{
    private static string TempRoot() => Path.Combine(Path.GetTempPath(), $"groundsman-folder-{Guid.NewGuid():N}");

    [Fact]
    public void Finds_the_latest_unfinished_season_and_skips_finished_or_broken_ones()
    {
        var folder = new PlaytestFolder(TempRoot());
        Assert.Null(folder.LatestUnfinished());

        var older = Path.Combine(folder.NewSeason(new DateTime(2026, 10, 1, 9, 0, 0), 1), PlaytestFolder.SaveName);
        SaveFile.Write(older, new SaveData { Seed = 1 });
        File.SetLastWriteTimeUtc(older, new DateTime(2026, 10, 1));
        var finished = Path.Combine(folder.NewSeason(new DateTime(2026, 10, 2, 9, 0, 0), 2), PlaytestFolder.SaveName);
        SaveFile.Write(finished, new SaveData { Seed = 2, Finished = true });
        var broken = Path.Combine(folder.NewSeason(new DateTime(2026, 10, 3, 9, 0, 0), 3), PlaytestFolder.SaveName);
        File.WriteAllText(broken, "not a save");

        Assert.Equal(older, folder.LatestUnfinished());
        Directory.Delete(folder.Root, recursive: true);
    }

    [Fact]
    public void The_content_hash_is_stable_and_changes_with_any_file()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"groundsman-content-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "a.json"), "{ \"x\": 1 }");
        File.WriteAllText(Path.Combine(directory, "b.json"), "{ \"y\": 2 }");

        var hash = ContentLoader.Hash(directory);
        Assert.Equal(hash, ContentLoader.Hash(directory));
        File.WriteAllText(Path.Combine(directory, "b.json"), "{ \"y\": 3 }");
        Assert.NotEqual(hash, ContentLoader.Hash(directory));
        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void Save_writes_a_named_copy_beside_the_autosave()
    {
        var root = TempRoot();
        var path = Path.Combine(root, PlaytestFolder.SaveName);
        var game = RecordingGame.Start(TestContent.Content, Array.Empty<Fixture>(), new GameTime(2027, 5, 1, 7), 1, "hash", path);
        var console = new TestConsole();

        new GameLoop(console, game, new StringReader("\n\nsave\n")).Run();

        Assert.True(File.Exists(Path.Combine(root, "save-turn-2.json")));
        Assert.Equal(2, SaveFile.Read(Path.Combine(root, "save-turn-2.json")).Turns);
        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void A_resumed_season_doesnt_repeat_old_commentary_or_verdicts()
    {
        var fixture = new Fixture(new DateTime(2027, 5, 20), TestFormats.OneDay, new StripId(6), TestTeams.Opponent);
        var game = new Game(new GameSetup(TestContent.Content, new GameTime(2027, 5, 19, 13), new[] { fixture }, 2));
        while (game.View.Now.Date <= fixture.Start)
        {
            game.Advance();
        }
        var console = new TestConsole();
        console.Profile.Width = 160;

        new GameLoop(console, game, new StringReader("s\n")).Run();

        Assert.DoesNotContain(game.View.LatestMatch!.Commentary[0].Text, console.Output);
        Assert.DoesNotContain("The referee's verdict", console.Output);
    }
}
