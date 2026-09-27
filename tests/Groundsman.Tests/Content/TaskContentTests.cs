using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class TaskContentTests
{
    [Fact]
    public void Shipped_task_content_parses()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "tasks.json"));

        var tasks = ContentParser.ParseTasks(json);

        Assert.True(tasks.WaterSurfaceGain > 0);
    }

    [Theory]
    [InlineData("{ \"waterSurfaceGain\": 0 }")]
    [InlineData("{ }")]
    public void Rejects_a_missing_or_non_positive_water_gain(string json)
    {
        var error = Assert.Throws<ContentException>(() => ContentParser.ParseTasks(json));

        Assert.Contains("waterSurfaceGain", error.Message);
    }
}
