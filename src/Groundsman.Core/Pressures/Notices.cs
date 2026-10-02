namespace Groundsman.Core.Pressures
{
    /// <summary>A stakeholder has asked for something.</summary>
    public sealed class RequestNotice : Notice
    {
        public RequestNotice(RequestView request)
        {
            Request = request;
        }

        public RequestView Request { get; }
    }

    /// <summary>A stakeholder's satisfaction moved.</summary>
    public sealed class SatisfactionNotice : Notice
    {
        public SatisfactionNotice(SatisfactionChange change)
        {
            Change = change;
        }

        public SatisfactionChange Change { get; }
    }
}
