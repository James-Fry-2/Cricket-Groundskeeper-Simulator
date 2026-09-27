namespace Groundsman.Core
{
    public interface IGame
    {
        GameView View { get; }
        CommandResult Submit(IGameCommand command);
        AdvanceResult Advance();
    }
}
