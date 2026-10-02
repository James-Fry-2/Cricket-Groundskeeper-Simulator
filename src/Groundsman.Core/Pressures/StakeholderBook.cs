using System;
using System.Collections.Generic;
using System.Linq;
using Groundsman.Core.Content;
using Groundsman.Core.Randomness;
using Groundsman.Core.Schedule;

namespace Groundsman.Core.Pressures
{
    /// <summary>
    /// The captain, the board and the referee: the requests they make, the answers the player
    /// gives, and their satisfaction. Requests for a fixture arrive a few days before its strip
    /// locks, so they can shape the choice of strip and the build-up, and must be answered by
    /// the lock.
    /// </summary>
    internal sealed class StakeholderBook
    {
        private static readonly Stakeholder[] Everyone = { Stakeholder.Captain, Stakeholder.Board, Stakeholder.Referee };

        private readonly StakeholderSettings _settings;
        private readonly IReadOnlyList<Fixture> _fixtures;
        private readonly StripBook _strips;
        private readonly RandomSource _random;
        private readonly HashSet<Fixture> _considered = new HashSet<Fixture>();
        private readonly List<Request> _requests = new List<Request>();
        private readonly double[] _satisfaction;
        private readonly List<SatisfactionChange> _changes = new List<SatisfactionChange>();
        private readonly List<Notice> _notices = new List<Notice>();

        public StakeholderBook(StakeholderSettings settings, IReadOnlyList<Fixture> fixtures, StripBook strips, RandomSource random)
        {
            _settings = settings;
            _fixtures = fixtures;
            _strips = strips;
            _random = random;
            _satisfaction = Everyone.Select(_ => settings.StartingSatisfaction).ToArray();
        }

        public IReadOnlyList<Notice> Notices => _notices;

        public void ClearNotices() => _notices.Clear();

        public IReadOnlyList<RequestView> Requests => _requests.Select(r => r.View()).ToArray();

        public IReadOnlyList<StakeholderView> Stakeholders =>
            Everyone.Select(s => new StakeholderView(s, Satisfaction(s), _changes.Where(c => c.Stakeholder == s).ToArray())).ToArray();

        public double Satisfaction(Stakeholder stakeholder) => _satisfaction[(int)stakeholder - 1];

        /// <summary>Requests unanswered at their lock are ignored; requests now due arrive.</summary>
        public void Update(DateTime today)
        {
            foreach (var request in _requests.Where(r => r.Status == RequestStatus.Open && r.AnswerBy <= today))
            {
                request.Status = RequestStatus.Ignored;
                Change(request.Stakeholder, _settings.AnswersFor(request.Stakeholder).Ignored, SatisfactionReason.RequestIgnored, request.Fixture, today, request.Kind);
            }

            foreach (var fixture in _fixtures)
            {
                var locksOn = _strips.LocksOn(fixture);
                if (_considered.Contains(fixture) || locksOn.AddDays(-_settings.RequestDaysBeforeLock) > today)
                {
                    continue;
                }
                _considered.Add(fixture);
                if (today >= locksOn)
                {
                    continue;
                }

                // Draw order is fixed and part of the replay contract: captain, then the
                // captain's character, then the board.
                if (_settings.CaptainCharacters.TryGetValue(fixture.Format.Id, out var characters) && _random.Chance(_settings.CaptainRequestChance))
                {
                    Ask(Stakeholder.Captain, Choose(characters), fixture, today, locksOn);
                }
                if (fixture.Days > 1 && _random.Chance(_settings.BoardRequestChance))
                {
                    Ask(Stakeholder.Board, RequestKind.LastsFourDays, fixture, today, locksOn);
                }
            }
        }

        public CommandResult Answer(string id, bool accept, DateTime today)
        {
            var request = _requests.FirstOrDefault(r => r.Id == id);
            if (request == null)
            {
                return CommandResult.Rejected($"There's no request {id}.");
            }
            if (request.Status != RequestStatus.Open)
            {
                return CommandResult.Rejected("That request has already been answered.");
            }
            if (today >= request.AnswerBy)
            {
                return CommandResult.Rejected("It's too late to answer that request: the strip has locked.");
            }

            if (accept)
            {
                request.Status = RequestStatus.Accepted;
            }
            else
            {
                request.Status = RequestStatus.Declined;
                Change(request.Stakeholder, _settings.AnswersFor(request.Stakeholder).Declined, SatisfactionReason.RequestDeclined, request.Fixture, today, request.Kind);
            }
            return CommandResult.Ok();
        }

        /// <summary>Moves a stakeholder's satisfaction within 0 to 100, logging why.</summary>
        public void Change(Stakeholder stakeholder, double delta, SatisfactionReason reason, Fixture fixture, DateTime date, RequestKind? request = null, Match.PitchGrade? grade = null, int demerits = 0)
        {
            var index = (int)stakeholder - 1;
            var before = _satisfaction[index];
            _satisfaction[index] = Math.Max(0, Math.Min(100, before + delta));
            var change = new SatisfactionChange(stakeholder, _satisfaction[index] - before, reason, fixture, date, request, grade, demerits);
            _changes.Add(change);
            _notices.Add(new SatisfactionNotice(change));
        }

        internal IEnumerable<Request> AcceptedFor(Fixture fixture) => _requests.Where(r => r.Fixture == fixture && r.Status == RequestStatus.Accepted);

        private void Ask(Stakeholder stakeholder, RequestKind kind, Fixture fixture, DateTime today, DateTime answerBy)
        {
            var request = new Request($"{fixture.Id}-{stakeholder.ToString().ToLowerInvariant()}", stakeholder, kind, fixture, today, answerBy);
            _requests.Add(request);
            _notices.Add(new RequestNotice(request.View()));
        }

        private RequestKind Choose(IReadOnlyDictionary<RequestKind, double> weights)
        {
            var kinds = weights.Keys.OrderBy(k => k).ToArray();
            var pick = _random.NextDouble() * kinds.Sum(k => weights[k]);
            foreach (var kind in kinds)
            {
                pick -= weights[kind];
                if (pick < 0)
                {
                    return kind;
                }
            }
            return kinds[kinds.Length - 1];
        }

        internal sealed class Request
        {
            public Request(string id, Stakeholder stakeholder, RequestKind kind, Fixture fixture, DateTime issuedOn, DateTime answerBy)
            {
                Id = id;
                Stakeholder = stakeholder;
                Kind = kind;
                Fixture = fixture;
                IssuedOn = issuedOn;
                AnswerBy = answerBy;
            }

            public string Id { get; }
            public Stakeholder Stakeholder { get; }
            public RequestKind Kind { get; }
            public Fixture Fixture { get; }
            public DateTime IssuedOn { get; }
            public DateTime AnswerBy { get; }
            public RequestStatus Status { get; set; } = RequestStatus.Open;

            public RequestView View() => new RequestView(Id, Stakeholder, Kind, Fixture, IssuedOn, AnswerBy, Status);
        }
    }
}
