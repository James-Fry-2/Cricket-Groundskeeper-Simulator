using Groundsman.Core;

namespace Groundsman.Harness.Policies;

/// <summary>Does nothing: the baseline that weather alone produces.</summary>
public sealed class NeglectPolicy : IPolicy
{
    public string Name => "neglect";

    public void PlayTurn(IGame game)
    {
    }
}
