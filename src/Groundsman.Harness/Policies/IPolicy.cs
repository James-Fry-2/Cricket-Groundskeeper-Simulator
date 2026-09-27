using Groundsman.Core;

namespace Groundsman.Harness.Policies;

/// <summary>
/// A scripted player. It gets <see cref="IGame"/>, which has no view of the truth, so it plays
/// from readings and the forecast just as a person would.
/// </summary>
public interface IPolicy
{
    string Name { get; }

    void PlayTurn(IGame game);
}
