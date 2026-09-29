using Groundsman.Core.Content;

namespace Groundsman.Tests.Content;

public class PitchContentTests
{
    private static string Shipped() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "pitch.json"));

    [Fact]
    public void Shipped_pitch_content_parses()
    {
        var pitch = ContentParser.ParsePitch(Shipped());

        Assert.Equal(10, pitch.GrassReferenceMm);
        Assert.Equal(0.6, pitch.ConsistencyDamageWeight);
        Assert.Equal(0.7, pitch.SeamWetWeight);
        Assert.Equal(2, pitch.CrackingDryExponent);
    }

    [Theory]
    [InlineData("\"grassReferenceMm\": 10", "\"grassReferenceMm\": 0", "grassReferenceMm")]
    [InlineData("\"depthBase\": 0.6", "\"depthBase\": 1.5", "depthBase")]
    [InlineData("\"clayReference\": 30", "\"clayReference\": 0", "clayReference")]
    [InlineData("\"damageWeight\": 0.6", "\"damageWeight\": -1", "damageWeight")]
    [InlineData("\"looseBelow\": 0.65", "\"looseBelow\": 2", "looseBelow")]
    [InlineData("\"grassCushion\": 0.4", "\"grassCushion\": 1.5", "grassCushion")]
    [InlineData("\"wetWeight\": 0.7", "\"wetWeight\": -1", "wetWeight")]
    [InlineData("\"dryExponent\": 2", "\"dryExponent\": 0", "dryExponent")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var error = Assert.Throws<ContentException>(() => ContentParser.ParsePitch(Shipped().Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }
}
