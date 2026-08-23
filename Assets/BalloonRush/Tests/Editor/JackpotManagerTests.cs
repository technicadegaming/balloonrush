#if UNITY_EDITOR
using BalloonRush.Gameplay;
using BalloonRush.SaveSystem;
using NUnit.Framework;
using UnityEngine;

namespace BalloonRush.Tests
{
    public sealed class JackpotManagerTests
    {
        [Test]
        public void PerfectFinalBalloonAwardsConfiguredJackpot()
        {
            GameObject root = new GameObject("Jackpot Test");
            ComboManager combo = root.AddComponent<ComboManager>();
            ScoreManager score = root.AddComponent<ScoreManager>();
            JackpotManager jackpot = root.AddComponent<JackpotManager>();
            OperatorSettings settings = new OperatorSettings { jackpotTickets = 500, regularTicketCap = 125, maxTicketPayout = 625 };
            settings.Validate();

            combo.Configure(3f);
            combo.ResetSession();
            score.Configure(combo, null);
            score.ResetSession(settings);
            jackpot.Configure(score, settings);
            jackpot.ResetSession();

            int reward = jackpot.ResolveFinalBalloon(TimingRating.Perfect);
            Assert.AreEqual(500, reward);
            Assert.AreEqual(500, score.JackpotTickets);
            Assert.IsTrue(jackpot.WasWon);
            Assert.IsTrue(score.JackpotWon);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void JackpotCanStackWithRegularTicketsUpToCommercialCap()
        {
            GameObject root = new GameObject("Jackpot Cap Test");
            ComboManager combo = root.AddComponent<ComboManager>();
            ScoreManager score = root.AddComponent<ScoreManager>();
            JackpotManager jackpot = root.AddComponent<JackpotManager>();
            OperatorSettings settings = new OperatorSettings();
            settings.Validate();

            combo.Configure(3f);
            combo.ResetSession();
            score.Configure(combo, null);
            score.ResetSession(settings);
            score.AddRegularTickets(125);
            jackpot.Configure(score, settings);
            jackpot.ResetSession();
            jackpot.ResolveFinalBalloon(TimingRating.Perfect);

            Assert.AreEqual(625, score.Tickets);
            Object.DestroyImmediate(root);
        }
    }
}
#endif
