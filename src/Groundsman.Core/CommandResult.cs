namespace Groundsman.Core
{
    public sealed class CommandResult
    {
        private CommandResult(bool accepted, string? reason)
        {
            Accepted = accepted;
            Reason = reason;
        }

        public bool Accepted { get; }
        public string? Reason { get; }

        public static CommandResult Ok() => new CommandResult(true, null);

        public static CommandResult Rejected(string reason) => new CommandResult(false, reason);
    }
}
