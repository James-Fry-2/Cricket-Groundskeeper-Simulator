using Groundsman.Core;

namespace Groundsman.Tests;

public class CommandResultTests
{
    [Fact]
    public void Rejected_result_carries_its_reason()
    {
        var result = CommandResult.Rejected("Strip is under covers");

        Assert.False(result.Accepted);
        Assert.Equal("Strip is under covers", result.Reason);
    }

    [Fact]
    public void Ok_result_is_accepted_without_a_reason()
    {
        var result = CommandResult.Ok();

        Assert.True(result.Accepted);
        Assert.Null(result.Reason);
    }
}
