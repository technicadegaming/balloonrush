#if UNITY_EDITOR
using BalloonRush.Core;
using BalloonRush.Gameplay;
using BalloonRush.SaveSystem;
using NUnit.Framework;
using UnityEngine;

namespace BalloonRush.Tests
{
    public sealed class BalloonRewardLogicTests
    {
        [Test]
        public void NormalAwardsClampToRegularTicketCap()
        {
            GameObject root = new GameObject("Score Test");
            ComboManager combo = root.AddComponent<ComboManager>();
            ScoreManager score = root.AddComponent<ScoreManager>();
            PayoutConfig payout = ScriptableObject.CreateInstance<PayoutConfig>();
            OperatorSettings settings = new OperatorSettings { regularTicketCap = 125, maxTicketPayout = 625 };
            settings.Validate();

            combo.Configure(3f);
            combo.ResetSession();
            score.Configure(combo, payout);
            score.ResetSession(settings);
            score.AddTickets(5000);

            Assert.AreEqual(125, score.RegularTickets);
            Assert.AreEqual(125, score.Tickets);
            Object.DestroyImmediate(payout);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void GreenBalloonAwardsAtLeastOneTicketOnGoodHit()
        {
            GameObject root = new GameObject("Reward Test");
            ComboManager combo = root.AddComponent<ComboManager>();
            ScoreManager score = root.AddComponent<ScoreManager>();
            PayoutConfig payout = ScriptableObject.CreateInstance<PayoutConfig>();
            BalloonDefinition definition = ScriptableObject.CreateInstance<BalloonDefinition>();
            definition.Configure("green", "Green", BalloonKind.Green, null, Color.green, 100, 1, 1f, false, BalloonSpecialBehavior.None);
            OperatorSettings settings = new OperatorSettings();
            settings.Validate();

            combo.Configure(3f);
            combo.ResetSession();
            score.Configure(combo, payout);
            score.ResetSession(settings);
            int award = score.RecordSuccessfulPop(definition, TimingRating.Good);

            Assert.GreaterOrEqual(award, 1);
            Assert.AreEqual(award, score.Tickets);
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(payout);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void FractionalPerfectBonusAccumulatesInsteadOfRoundingEveryGreenBalloonUp()
        {
            GameObject root = new GameObject("Fraction Test");
            ComboManager combo = root.AddComponent<ComboManager>();
            ScoreManager score = root.AddComponent<ScoreManager>();
            BalloonDefinition definition = ScriptableObject.CreateInstance<BalloonDefinition>();
            definition.Configure("green", "Green", BalloonKind.Green, null, Color.green, 100, 1, 1f, false, BalloonSpecialBehavior.None);
            OperatorSettings settings = new OperatorSettings
            {
                perfectTicketMultiplier = 1.10f,
                combo5Multiplier = 1f,
                combo10Multiplier = 1f,
                combo15Multiplier = 1f,
                combo20Multiplier = 1f,
                combo30Multiplier = 1f
            };
            settings.Validate();

            combo.Configure(30f);
            combo.ResetSession();
            score.Configure(combo, null);
            score.ResetSession(settings);
            for (int i = 0; i < 10; i++)
            {
                score.RecordSuccessfulPop(definition, TimingRating.Perfect);
            }

            Assert.AreEqual(11, score.Tickets);
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void SuperBombCanCancelActivePayoutMultiplier()
        {
            GameObject root = new GameObject("Multiplier Test");
            ComboManager combo = root.AddComponent<ComboManager>();
            ScoreManager score = root.AddComponent<ScoreManager>();
            OperatorSettings settings = new OperatorSettings();
            settings.Validate();

            combo.Configure(3f);
            combo.ResetSession();
            score.Configure(combo, null);
            score.ResetSession(settings);
            score.ActivatePayoutMultiplier(2f, 5f);
            score.CancelPayoutMultiplier();

            Assert.AreEqual(1f, score.ActivePayoutMultiplier);
            Assert.AreEqual(0f, score.PayoutMultiplierRemaining);
            Object.DestroyImmediate(root);
        }
    }
}
#endif
