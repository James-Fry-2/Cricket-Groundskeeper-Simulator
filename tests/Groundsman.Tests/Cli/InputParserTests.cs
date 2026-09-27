using Groundsman.Cli;
using Groundsman.Core.Readings;
using Groundsman.Core.Staff;
using Groundsman.Core.Strips;

namespace Groundsman.Tests.Cli;

public class InputParserTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a")]
    [InlineData("advance")]
    public void Blank_or_a_advances(string input)
    {
        Assert.IsType<AdvanceInput>(InputParser.Parse(input));
    }

    [Theory]
    [InlineData("q")]
    [InlineData("quit")]
    public void Q_quits(string input)
    {
        Assert.IsType<QuitInput>(InputParser.Parse(input));
    }

    [Theory]
    [InlineData("h")]
    [InlineData("help")]
    [InlineData("?")]
    public void H_shows_help(string input)
    {
        Assert.IsType<HelpInput>(InputParser.Parse(input));
    }

    [Theory]
    [InlineData("s")]
    [InlineData("status")]
    public void S_shows_the_status(string input)
    {
        Assert.IsType<StatusInput>(InputParser.Parse(input));
    }

    [Theory]
    [InlineData("r 3")]
    [InlineData("R 3")]
    [InlineData("read 3")]
    [InlineData("  r   3  ")]
    public void R_with_a_number_reads_that_strip(string input)
    {
        var read = Assert.IsType<ReadInput>(InputParser.Parse(input));

        Assert.Equal(new StripId(3), read.Strip);
    }

    [Fact]
    public void R_all_reads_every_strip()
    {
        var read = Assert.IsType<ReadInput>(InputParser.Parse("r all"));

        Assert.Null(read.Strip);
    }

    [Theory]
    [InlineData("w 12")]
    [InlineData("water 12")]
    public void W_with_a_number_waters_that_strip(string input)
    {
        var water = Assert.IsType<WaterInput>(InputParser.Parse(input));

        Assert.Equal(new StripId(12), water.Strip);
    }

    [Theory]
    [InlineData("c 4", true)]
    [InlineData("cover 4", true)]
    [InlineData("u 4", false)]
    [InlineData("uncover 4", false)]
    public void C_and_u_cover_and_uncover_a_strip(string input, bool cover)
    {
        var parsed = InputParser.Parse(input);

        if (cover)
        {
            Assert.Equal(new StripId(4), Assert.IsType<CoverInput>(parsed).Strip);
        }
        else
        {
            Assert.Equal(new StripId(4), Assert.IsType<UncoverInput>(parsed).Strip);
        }
    }

    [Fact]
    public void F_takes_a_feel_reading()
    {
        var feel = Assert.IsType<ReadInput>(InputParser.Parse("f 3 jo"));

        Assert.Equal(new StripId(3), feel.Strip);
        Assert.Equal(new StaffId("jo"), feel.By);
        Assert.Equal(ReadingSource.Feel, feel.Tool);
        Assert.Equal(ReadingSource.MoistureProbe, Assert.IsType<ReadInput>(InputParser.Parse("r 3")).Tool);
        Assert.Equal(ReadingSource.Feel, Assert.IsType<ReadInput>(InputParser.Parse("feel all")).Tool);
    }

    [Fact]
    public void A_name_after_the_strip_says_who_does_the_job()
    {
        Assert.Equal(new StaffId("sam"), Assert.IsType<ReadInput>(InputParser.Parse("r 3 Sam")).By);
        Assert.Equal(new StaffId("jo"), Assert.IsType<ReadInput>(InputParser.Parse("r all jo")).By);
        Assert.Equal(new StaffId("sam"), Assert.IsType<WaterInput>(InputParser.Parse("w 3 sam")).By);
        Assert.Equal(new StaffId("sam"), Assert.IsType<CoverInput>(InputParser.Parse("c 3 sam")).By);
        Assert.Equal(new StaffId("sam"), Assert.IsType<UncoverInput>(InputParser.Parse("u 3 sam")).By);
        Assert.Null(Assert.IsType<WaterInput>(InputParser.Parse("w 3")).By);
    }

    [Theory]
    [InlineData("c all")]
    [InlineData("u")]
    [InlineData("x")]
    [InlineData("r")]
    [InlineData("w")]
    [InlineData("w all")]
    [InlineData("r three")]
    [InlineData("w 3 4 5")]
    public void Anything_else_is_invalid_with_a_hint(string input)
    {
        var invalid = Assert.IsType<InvalidInput>(InputParser.Parse(input));

        Assert.Contains("h", invalid.Message);
    }
}
