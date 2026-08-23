using System;
using BalloonRush.Core;
using BalloonRush.SaveSystem;
using UnityEngine;

namespace BalloonRush.Gameplay
{
    public sealed class ScoreManager : MonoBehaviour
    {
        [SerializeField] private ComboManager comboManager;

        private OperatorSettings settings;
        private PayoutConfig payoutConfig;
        private float payoutMultiplier = 1f;
        private float payoutMultiplierRemaining;
        private int lastMultiplierTenth = -1;
        private float regularTicketBank;
        private int bonusTicketBank;
        private int jackpotTicketBank;

        public int Score { get; private set; }
        public int Tickets { get; private set; }
        public int RegularTickets => Mathf.Max(0, Mathf.FloorToInt(regularTicketBank + 0.0001f));
        public int BonusTickets => Mathf.Max(0, bonusTicketBank);
        public int JackpotTickets => Mathf.Max(0, jackpotTicketBank);
        public int PerfectPops { get; private set; }
        public int GreatPops { get; private set; }
        public int GoodPops { get; private set; }
        public int Misses { get; private set; }
        public int BalloonsPopped { get; private set; }
        public int GoldenBalloons { get; private set; }
        public int BombsHit { get; private set; }
        public bool JackpotWon { get; private set; }
        public float ActivePayoutMultiplier => payoutMultiplier;
        public float PayoutMultiplierRemaining => payoutMultiplierRemaining;

        public event Action<int> ScoreChanged;
        public event Action<int> TicketsChanged;
        public event Action<float, float> PayoutMultiplierChanged;

        public void Configure(ComboManager combo, PayoutConfig configuredPayout)
        {
            comboManager = combo;
            payoutConfig = configuredPayout;
        }

        public void ResetSession(OperatorSettings operatorSettings)
        {
            settings = operatorSettings;
            settings?.Validate();
            Score = 0;
            regularTicketBank = 0f;
            bonusTicketBank = 0;
            jackpotTicketBank = 0;
            Tickets = 0;
            PerfectPops = 0;
            GreatPops = 0;
            GoodPops = 0;
            Misses = 0;
            BalloonsPopped = 0;
            GoldenBalloons = 0;
            BombsHit = 0;
            JackpotWon = false;
            payoutMultiplier = 1f;
            payoutMultiplierRemaining = 0f;
            lastMultiplierTenth = -1;
            ScoreChanged?.Invoke(Score);
            TicketsChanged?.Invoke(Tickets);
            PayoutMultiplierChanged?.Invoke(payoutMultiplier, payoutMultiplierRemaining);
            GameEvents.RaiseScoreChanged(Score);
            GameEvents.RaiseTicketsChanged(Tickets);
        }

        public int RecordSuccessfulPop(BalloonDefinition definition, TimingRating rating)
        {
            if (definition == null || rating == TimingRating.Miss)
            {
                RecordMiss();
                return 0;
            }

            BalloonsPopped++;
            int combo = comboManager != null ? comboManager.RegisterSuccessfulPop() : 1;
            CountTimingRating(rating);

            int points = Mathf.RoundToInt(definition.BasePoints * TimingEvaluator.GetScoreMultiplier(rating) * GetComboScoreMultiplier(combo));
            AddScore(points);

            int baseTickets = GetConfiguredBaseTickets(definition);
            float goodMultiplier = settings != null ? settings.goodTicketMultiplier : GetPayoutValue(payoutConfig != null ? payoutConfig.goodTicketMultiplier : 1f, 1f);
            float greatMultiplier = settings != null ? settings.greatTicketMultiplier : GetPayoutValue(payoutConfig != null ? payoutConfig.greatTicketMultiplier : 1.05f, 1.05f);
            float perfectMultiplier = settings != null ? settings.perfectTicketMultiplier : GetPayoutValue(payoutConfig != null ? payoutConfig.perfectTicketMultiplier : 1.20f, 1.20f);
            float combo5Multiplier = settings != null ? settings.combo5Multiplier : GetPayoutValue(payoutConfig != null ? payoutConfig.combo5Multiplier : 1.05f, 1.05f);
            float combo10Multiplier = settings != null ? settings.combo10Multiplier : GetPayoutValue(payoutConfig != null ? payoutConfig.combo10Multiplier : 1.10f, 1.10f);
            float combo15Multiplier = settings != null ? settings.combo15Multiplier : GetPayoutValue(payoutConfig != null ? payoutConfig.combo15Multiplier : 1.20f, 1.20f);
            float combo20Multiplier = settings != null ? settings.combo20Multiplier : GetPayoutValue(payoutConfig != null ? payoutConfig.combo20Multiplier : 1.35f, 1.35f);
            float combo30Multiplier = settings != null ? settings.combo30Multiplier : GetPayoutValue(payoutConfig != null ? payoutConfig.combo30Multiplier : 1.50f, 1.50f);

            float rawAward = TicketMath.CalculateRawAward(
                baseTickets,
                rating,
                combo,
                payoutMultiplier,
                goodMultiplier,
                greatMultiplier,
                perfectMultiplier,
                combo5Multiplier,
                combo10Multiplier,
                combo15Multiplier,
                combo20Multiplier,
                combo30Multiplier);

            return AddRegularTicketValue(rawAward);
        }

        public void RecordMiss()
        {
            Misses++;
            comboManager?.RegisterMiss();
            GameEvents.RaiseTimingJudged(TimingRating.Miss);
        }

        public void RegisterBombHit()
        {
            BombsHit++;
            comboManager?.RegisterMiss();
            int penalty = settings != null ? settings.bombTicketPenalty : 0;
            ApplyTicketPenalty(penalty);
        }

        public void AddScore(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Score = Mathf.Max(0, Score + amount);
            ScoreChanged?.Invoke(Score);
            GameEvents.RaiseScoreChanged(Score);
        }

        /// <summary>Legacy alias: ordinary awards are subject to the regular-ticket cap.</summary>
        public int AddTickets(int amount)
        {
            return AddRegularTickets(amount);
        }

        public int AddRegularTickets(int amount)
        {
            return amount > 0 ? AddRegularTicketValue(amount) : 0;
        }

        public int AddBonusTickets(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int previous = Tickets;
            int remaining = Mathf.Max(0, GetTotalCap() - Tickets);
            bonusTicketBank += Mathf.Min(amount, remaining);
            RefreshTicketTotal();
            return Tickets - previous;
        }

        public int AddJackpotTickets(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int previous = Tickets;
            int remaining = Mathf.Max(0, GetTotalCap() - Tickets);
            jackpotTicketBank += Mathf.Min(amount, remaining);
            RefreshTicketTotal();
            return Tickets - previous;
        }

        public void ApplyTicketPenalty(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            regularTicketBank = Mathf.Max(0f, regularTicketBank - amount);
            RefreshTicketTotal();
        }

        public void ActivatePayoutMultiplier(float multiplier, float duration)
        {
            payoutMultiplier = Mathf.Max(payoutMultiplier, Mathf.Max(1f, multiplier));
            payoutMultiplierRemaining = Mathf.Max(payoutMultiplierRemaining, Mathf.Max(0f, duration));
            lastMultiplierTenth = Mathf.CeilToInt(payoutMultiplierRemaining * 10f);
            PayoutMultiplierChanged?.Invoke(payoutMultiplier, payoutMultiplierRemaining);
        }

        public void CancelPayoutMultiplier()
        {
            if (payoutMultiplier <= 1f && payoutMultiplierRemaining <= 0f)
            {
                return;
            }

            payoutMultiplier = 1f;
            payoutMultiplierRemaining = 0f;
            lastMultiplierTenth = 0;
            PayoutMultiplierChanged?.Invoke(payoutMultiplier, payoutMultiplierRemaining);
        }

        public void AddCombo(int amount)
        {
            comboManager?.AddCombo(amount);
        }

        public void MarkGoldenBalloon()
        {
            GoldenBalloons++;
        }

        public void MarkJackpotWon()
        {
            JackpotWon = true;
        }

        public GameSessionResult CreateResult(float gameDuration)
        {
            EnsureMinimumPayout();
            return new GameSessionResult
            {
                score = Score,
                tickets = Tickets,
                regularTickets = RegularTickets,
                bonusTickets = BonusTickets,
                jackpotTickets = JackpotTickets,
                pricePerPlayCents = settings != null ? settings.pricePerPlayCents : 100,
                highestCombo = comboManager != null ? comboManager.HighestCombo : 0,
                perfectPops = PerfectPops,
                greatPops = GreatPops,
                goodPops = GoodPops,
                misses = Misses,
                balloonsPopped = BalloonsPopped,
                bombsHit = BombsHit,
                goldenBalloons = GoldenBalloons,
                jackpotWon = JackpotWon,
                gameDuration = gameDuration
            };
        }

        private void Update()
        {
            if (payoutMultiplierRemaining <= 0f)
            {
                return;
            }

            payoutMultiplierRemaining -= Time.deltaTime;
            if (payoutMultiplierRemaining <= 0f)
            {
                payoutMultiplierRemaining = 0f;
                payoutMultiplier = 1f;
            }

            int tenth = Mathf.CeilToInt(payoutMultiplierRemaining * 10f);
            if (tenth != lastMultiplierTenth)
            {
                lastMultiplierTenth = tenth;
                PayoutMultiplierChanged?.Invoke(payoutMultiplier, payoutMultiplierRemaining);
            }
        }

        private int AddRegularTicketValue(float rawAmount)
        {
            if (rawAmount <= 0f)
            {
                return 0;
            }

            int previous = Tickets;
            regularTicketBank = Mathf.Min(GetRegularCap(), regularTicketBank + rawAmount);
            RefreshTicketTotal();
            return Tickets - previous;
        }

        private void EnsureMinimumPayout()
        {
            int minimum = settings != null ? settings.minimumTicketPayout : 0;
            minimum = Mathf.Clamp(minimum, 0, GetRegularCap());
            if (Tickets >= minimum)
            {
                return;
            }

            regularTicketBank = Mathf.Max(regularTicketBank, minimum - BonusTickets - JackpotTickets);
            regularTicketBank = Mathf.Clamp(regularTicketBank, 0f, GetRegularCap());
            RefreshTicketTotal();
        }

        private void RefreshTicketTotal()
        {
            int previous = Tickets;
            Tickets = Mathf.Clamp(RegularTickets + BonusTickets + JackpotTickets, 0, GetTotalCap());
            if (Tickets == previous)
            {
                return;
            }

            TicketsChanged?.Invoke(Tickets);
            GameEvents.RaiseTicketsChanged(Tickets);
        }

        private int GetRegularCap()
        {
            int cap = settings != null
                ? settings.regularTicketCap
                : (payoutConfig != null && payoutConfig.balanceVersion >= 2 ? payoutConfig.regularTicketsCap : 125);
            return Mathf.Clamp(cap, 1, GetTotalCap());
        }

        private int GetTotalCap()
        {
            int cap = settings != null
                ? settings.maxTicketPayout
                : (payoutConfig != null && payoutConfig.balanceVersion >= 2 ? payoutConfig.maximumTicketsPerGame : 625);
            return Mathf.Clamp(cap, 1, 1000);
        }

        private void CountTimingRating(TimingRating rating)
        {
            switch (rating)
            {
                case TimingRating.Perfect:
                    PerfectPops++;
                    break;
                case TimingRating.Great:
                    GreatPops++;
                    break;
                case TimingRating.Good:
                    GoodPops++;
                    break;
            }
            GameEvents.RaiseTimingJudged(rating);
        }

        private int GetConfiguredBaseTickets(BalloonDefinition definition)
        {
            if (settings == null)
            {
                return definition.BaseTickets;
            }

            switch (definition.Kind)
            {
                case BalloonKind.Green: return settings.greenTickets;
                case BalloonKind.Blue: return settings.blueTickets;
                case BalloonKind.GoldenTrigger: return settings.goldenTriggerTickets;
                default: return definition.BaseTickets;
            }
        }

        private static float GetComboScoreMultiplier(int combo)
        {
            return 1f + Mathf.Min(3f, Mathf.Max(0, combo - 1) * 0.06f);
        }

        private static float GetPayoutValue(float value, float fallback)
        {
            return value > 0f ? value : fallback;
        }
    }
}
