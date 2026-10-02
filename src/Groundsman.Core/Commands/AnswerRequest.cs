namespace Groundsman.Core.Commands
{
    /// <summary>Accept or turn down a stakeholder's request. Office work: it costs no hours.</summary>
    public sealed class AnswerRequest : IGameCommand
    {
        public AnswerRequest(string requestId, bool accept)
        {
            RequestId = requestId;
            Accept = accept;
        }

        public string RequestId { get; }
        public bool Accept { get; }
    }
}
