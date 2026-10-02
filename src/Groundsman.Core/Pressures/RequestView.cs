using System;

namespace Groundsman.Core.Pressures
{
    public enum RequestStatus
    {
        Open = 1,
        Accepted = 2,
        Declined = 3,

        /// <summary>Left unanswered until the strip locked.</summary>
        Ignored = 4,

        /// <summary>Accepted, and the pitch did what was asked.</summary>
        Delivered = 5,

        /// <summary>Accepted, and the pitch didn't.</summary>
        NotDelivered = 6,

        /// <summary>Accepted, but rain left too little play to judge it.</summary>
        Spoiled = 7,
    }

    /// <summary>Something a stakeholder has asked of a fixture's pitch.</summary>
    public sealed class RequestView
    {
        public RequestView(string id, Stakeholder stakeholder, RequestKind kind, Fixture fixture, DateTime issuedOn, DateTime answerBy, RequestStatus status)
        {
            Id = id;
            Stakeholder = stakeholder;
            Kind = kind;
            Fixture = fixture;
            IssuedOn = issuedOn;
            AnswerBy = answerBy;
            Status = status;
        }

        public string Id { get; }
        public Stakeholder Stakeholder { get; }
        public RequestKind Kind { get; }
        public Fixture Fixture { get; }
        public DateTime IssuedOn { get; }

        /// <summary>The day the fixture's strip locks; unanswered by then, the request is ignored.</summary>
        public DateTime AnswerBy { get; }

        public RequestStatus Status { get; }
    }
}
