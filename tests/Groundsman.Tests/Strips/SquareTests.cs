using Groundsman.Core.Content;
using Groundsman.Core.Strips;

namespace Groundsman.Tests.Strips;

public class SquareTests
{
    [Fact]
    public void Starts_each_strip_from_its_content()
    {
        var square = new Square(TestContent.Content);

        Assert.Equal(TestGround.Settings.Strips.Count, square.Strips.Count);
        Assert.Equal(25, square.Get(new StripId(3)).SurfaceMoisture);
        Assert.Equal(29, square.Get(new StripId(3)).SubsurfaceMoisture);
        Assert.Same(TestLoams.Standard, square.Get(new StripId(3)).Loam);
    }

    [Fact]
    public void Knows_which_strips_exist()
    {
        var square = new Square(TestContent.Content);

        Assert.True(square.Contains(new StripId(1)));
        Assert.True(square.Contains(new StripId(12)));
        Assert.False(square.Contains(new StripId(0)));
        Assert.False(square.Contains(new StripId(13)));
    }

    [Fact]
    public void Strip_ids_display_as_strip_numbers()
    {
        Assert.Equal("Strip 7", new StripId(7).ToString());
    }
}
