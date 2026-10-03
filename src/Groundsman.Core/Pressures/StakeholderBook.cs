using System;
using System.Collections.Generic;
using System.Linq;
using Groundsman.Core.Content;
using Groundsman.Core.Match;
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
        private readonly HashSet<MatchState> _judged = new HashSet<MatchState>();
        private readonly RequestJudge _judge;

        public StakeholderBook(StakeholderSettings settings, IReadOnlyList<Fixture> fixtures, StripBook strips, RandomSource random)
        {
            _settings = settings;
            _fixtures = fixtures;
            _strips = strips;
            _random = random;
            _satisfaction = Everyone.Select(_ => settings.StartingSatisfaction).ToArray();
            _judge = new RequestJudge(settings);
        }

        public IReadOnlyList<Notice> Notices => _notices;

        public void ClearNotices() => _notices.Clear();

        public IReadOnlyList<RequestView> Requests => _requests.Select(r => r.View()).ToArray();

        public IReadOnlyList<StakeholderView> Stakeholders =>
            Everyone.Select(s => new StakeholderView(s, Satisfaction(s), _changes.Where(c => c.Stakeholder == s).ToArray())).ToArray();

        /// <summary>Matches settled with everyone so far.</summary>
        public int Judged => _judged.Count;

        public IReadOnlyList<StakeholderReview> Review() =>
            Everyone.Select(s => new StakeholderReview(
                s,
                Satisfaction(s),
                _settings.MoodFor(Satisfaction(s)),
                _changes
                    .Where(c => c.Stakeholder == s)
                    .GroupBy(c => (c.Reason, c.Request))
                    .Select(g => new ReasonSummary(g.Key.Reason, g.Key.Request, g.Sum(c => c.Delta), g.Count()))
                    .OrderByDescending(r => System.Math.Abs(r.Total))
                    .ThenBy(r => r.Reason)
                    .ThenBy(r => r.Request)
                    .Take(3)
                    .ToArray()))
            .ToArray();

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

        /// <summary>
        /// Settles each finished match once: accepted requests judged, the result and length of
        /// the match, where a televised match was played, and the referee's rating.
        /// </summary>
        public void Judge(IEnumerable<MatchState> played, Func<Strips.StripId, bool> isCentre, string homeTeam, DateTime today)
        {
            foreach (var match in played.Where(m => m.Finished && m.Settled && !_judged.Contains(m)))
            {
                _judged.Add(match);
                var fixture = match.Fixture;

                foreach (var request in _requests.Where(r => r.Fixture == fixture && r.Status == RequestStatus.Accepted))
                {
                    request.Status = _judge.Judge(request.Kind, match);
                    var answers = _settings.AnswersFor(request.Stakeholder);
                    if (request.Status == RequestStatus.Delivered)
                    {
                        Change(request.Stakeholder, answers.Delivered, SatisfactionReason.RequestDelivered, fixture, today, request.Kind);
                    }
                    else if (request.Status == RequestStatus.NotDelivered)
                    {
                        Change(request.Stakeholder, answers.NotDelivered, SatisfactionReason.RequestNotDelivered, fixture, today, request.Kind);
                    }
                }

                if (match.Result?.Kind == ResultKind.Win)
                {
                    var won = match.Result.Winner == homeTeam;
                    Change(Stakeholder.Captain, won ? _settings.HomeWin : _settings.HomeLoss, won ? SatisfactionReason.HomeWin : SatisfactionReason.HomeLoss, fixture, today);
                }

                if (fixture.Days > 1)
                {
                    var lasted = _judge.ReachedDayFour(match);
                    Change(Stakeholder.Board, lasted ? _settings.DayFourReached : _settings.ShortFourDay, lasted ? SatisfactionReason.DayFourReached : SatisfactionReason.ShortFourDay, fixture, today);
                }
                else if (match.Result?.Kind == ResultKind.NoResult)
                {
                    Change(Stakeholder.Board, _settings.NoResult, SatisfactionReason.NoResult, fixture, today);
                }
                if (fixture.Televised)
                {
                    var centre = isCentre(match.Strip);
                    Change(Stakeholder.Board, centre ? _settings.TelevisedCentre : _settings.TelevisedOffCentre, centre ? SatisfactionReason.TelevisedCentre : SatisfactionReason.TelevisedOffCentre, fixture, today);
                }

                if (match.Rating is { } rating)
                {
                    if (rating.Demerits > 0)
                    {
                        Change(Stakeholder.Board, _settings.PerDemerit * rating.Demerits, SatisfactionReason.Demerits, fixture, today, demerits: rating.Demerits);
                    }
                    var delta = rating.Grade switch
                    {
                        PitchGrade.VeryGood => _settings.VeryGood,
                        PitchGrade.Satisfactory => _settings.Satisfactory,
                        PitchGrade.Unsatisfactory => _settings.Unsatisfactory,
                        _ => _settings.Unfit,
                    };
                    Change(Stakeholder.Referee, delta, SatisfactionReason.Rated, fixture, today, grade: rating.Grade);
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
        private void Change(Stakeholder stakeholder, double delta, SatisfactionReason reason, Fixture fixture, DateTime date, RequestKind? request = null, PitchGrade? grade = null, int demerits = 0)
        {
            var index = (int)stakeholder - 1;
            var before = _satisfaction[index];
            _satisfaction[index] = Math.Max(0, Math.Min(100, before + delta));
            var change = new SatisfactionChange(stakeholder, _satisfaction[index] - before, reason, fixture, date, request, grade, demerits);
            _changes.Add(change);
            _notices.Add(new SatisfactionNotice(change));
        }

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
