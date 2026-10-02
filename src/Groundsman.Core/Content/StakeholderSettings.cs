using System.Collections.Generic;
using System.Linq;
using Groundsman.Core.Pressures;

namespace Groundsman.Core.Content
{
    /// <summary>
    /// The captain, the board and the referee: when they ask for things, how a pitch is judged
    /// to have delivered them, and how much each outcome moves their satisfaction (0 to 100).
    /// </summary>
    public sealed class StakeholderSettings
    {
        public StakeholderSettings(
            double startingSatisfaction,
            int requestDaysBeforeLock,
            double captainRequestChance,
            IReadOnlyDictionary<string, IReadOnlyDictionary<RequestKind, double>> captainCharacters,
            double boardRequestChance,
            AnswerEffects captainAnswers,
            AnswerEffects boardAnswers,
            double homeWin,
            double homeLoss,
            double dayFourReached,
            double shortFourDay,
            double noResult,
            double televisedCentre,
            double televisedOffCentre,
            double perDemerit,
            double veryGood,
            double satisfactory,
            double unsatisfactory,
            double unfit,
            double greenSeamDayOne,
            double turningSpinLastDay,
            double paceCarry,
            double trueConsistency,
            double flatMovementBelow)
        {
            CheckRange("startingSatisfaction", startingSatisfaction, 0, 100);
            if (requestDaysBeforeLock < 1)
            {
                throw new ContentException($"stakeholders.requestDaysBeforeLock ({requestDaysBeforeLock}) must be at least 1.");
            }
            CheckRange("captain.requestChance", captainRequestChance, 0, 1);
            CheckRange("board.requestChance", boardRequestChance, 0, 1);
            foreach (var format in captainCharacters)
            {
                if (format.Value.Count == 0 || format.Value.Values.Any(w => w < 0) || format.Value.Values.Sum() <= 0)
                {
                    throw new ContentException($"stakeholders.captain.characters.{format.Key} needs at least one character with a positive weight.");
                }
                if (format.Value.ContainsKey(RequestKind.LastsFourDays))
                {
                    throw new ContentException($"stakeholders.captain.characters.{format.Key}: lasting four days is the board's request, not a pitch character.");
                }
            }

            StartingSatisfaction = startingSatisfaction;
            RequestDaysBeforeLock = requestDaysBeforeLock;
            CaptainRequestChance = captainRequestChance;
            CaptainCharacters = captainCharacters;
            BoardRequestChance = boardRequestChance;
            CaptainAnswers = captainAnswers;
            BoardAnswers = boardAnswers;
            HomeWin = homeWin;
            HomeLoss = homeLoss;
            DayFourReached = dayFourReached;
            ShortFourDay = shortFourDay;
            NoResult = noResult;
            TelevisedCentre = televisedCentre;
            TelevisedOffCentre = televisedOffCentre;
            PerDemerit = perDemerit;
            VeryGood = veryGood;
            Satisfactory = satisfactory;
            Unsatisfactory = unsatisfactory;
            Unfit = unfit;
            GreenSeamDayOne = greenSeamDayOne;
            TurningSpinLastDay = turningSpinLastDay;
            PaceCarry = paceCarry;
            TrueConsistency = trueConsistency;
            FlatMovementBelow = flatMovementBelow;
        }

        public double StartingSatisfaction { get; }

        /// <summary>Days before a strip locks that requests for its fixture arrive, so they can shape the choice.</summary>
        public int RequestDaysBeforeLock { get; }

        public double CaptainRequestChance { get; }

        /// <summary>By format id, the pitch characters the captain asks for and their weights.</summary>
        public IReadOnlyDictionary<string, IReadOnlyDictionary<RequestKind, double>> CaptainCharacters { get; }

        /// <summary>Chance the board asks a four-day match to last into day four.</summary>
        public double BoardRequestChance { get; }

        public AnswerEffects CaptainAnswers { get; }
        public AnswerEffects BoardAnswers { get; }

        // Captain.
        public double HomeWin { get; }
        public double HomeLoss { get; }

        // Board.
        public double DayFourReached { get; }

        /// <summary>A four-day match over before day four: gate and hospitality lost.</summary>
        public double ShortFourDay { get; }

        public double NoResult { get; }
        public double TelevisedCentre { get; }
        public double TelevisedOffCentre { get; }
        public double PerDemerit { get; }

        // Referee, by grade.
        public double VeryGood { get; }
        public double Satisfactory { get; }
        public double Unsatisfactory { get; }
        public double Unfit { get; }

        // Whether a pitch delivered what was asked, judged on its hours of dry play (0 to 10).
        public double GreenSeamDayOne { get; }
        public double TurningSpinLastDay { get; }
        public double PaceCarry { get; }

        /// <summary>Average consistency a pace or flat pitch also needs.</summary>
        public double TrueConsistency { get; }

        /// <summary>Average of the larger of seam and spin a flat pitch stays under.</summary>
        public double FlatMovementBelow { get; }

        public AnswerEffects AnswersFor(Stakeholder stakeholder) => stakeholder == Stakeholder.Board ? BoardAnswers : CaptainAnswers;

        private static void CheckRange(string field, double value, double min, double max)
        {
            if (value < min || value > max)
            {
                throw new ContentException($"stakeholders.{field} ({value}) must be from {min} to {max}.");
            }
        }
    }

    /// <summary>How satisfaction moves with the answer to a request and what came of it.</summary>
    public sealed class AnswerEffects
    {
        public AnswerEffects(double delivered, double notDelivered, double declined, double ignored)
        {
            Delivered = delivered;
            NotDelivered = notDelivered;
            Declined = declined;
            Ignored = ignored;
        }

        public double Delivered { get; }
        public double NotDelivered { get; }
        public double Declined { get; }

        /// <summary>Left unanswered until the strip locked.</summary>
        public double Ignored { get; }
    }
}
