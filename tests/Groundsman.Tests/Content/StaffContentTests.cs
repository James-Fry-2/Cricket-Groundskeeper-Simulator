using Groundsman.Core.Content;
using Groundsman.Core.Staff;

namespace Groundsman.Tests.Content;

public class StaffContentTests
{
    private static string Shipped() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "content", "staff.json"));

    [Fact]
    public void Shipped_staff_content_parses_with_you_first()
    {
        var staff = ContentParser.ParseStaff(Shipped());

        Assert.Equal(new StaffId("you"), staff.Members[0].Id);
        Assert.Equal(3, staff.Members.Count);
        Assert.Equal(1.0, staff.WaterHours);
        Assert.Equal(0.25, staff.ProbeReadingHours);
    }

    [Theory]
    [InlineData("\"hoursPerDay\": 6", "\"hoursPerDay\": 25", "hoursPerDay")]
    [InlineData("\"readingSkill\": 1.4", "\"readingSkill\": 0", "readingSkill")]
    [InlineData("\"id\": \"jo\"", "\"id\": \"sam\"", "sam")]
    [InlineData("\"id\": \"jo\"", "\"id\": \"\"", "id")]
    [InlineData("\"water\": 1.0", "\"water\": -1", "water")]
    [InlineData("\"feelReading\": 0.05", "\"feelReading\": -0.1", "feelReading")]
    public void Rejects_invalid_values_naming_the_field(string original, string replacement, string field)
    {
        var json = Shipped();
        Assert.Contains(original, json);

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseStaff(json.Replace(original, replacement)));

        Assert.Contains(field, error.Message);
    }

    [Fact]
    public void Rejects_an_empty_staff_list()
    {
        var json = @"{ ""staff"": [], ""jobHours"": { ""water"": 1, ""cover"": 0.25, ""uncover"": 0.25, ""probeReading"": 0.25, ""feelReading"": 0.05 } }";

        var error = Assert.Throws<ContentException>(() => ContentParser.ParseStaff(json));

        Assert.Contains("staff", error.Message);
    }
}
