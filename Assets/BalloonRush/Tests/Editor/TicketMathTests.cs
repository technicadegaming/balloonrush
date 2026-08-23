#if UNITY_EDITOR
using BalloonRush.Gameplay;
using NUnit.Framework;

namespace BalloonRush.Tests
{
    public sealed class TicketMathTests
    {
        [Test]
        public void PerfectComboAndX2IncreaseAward()
        {
            int award = TicketMath.CalculateAward(5, TimingRating.Perfect, 10, 2f);
            Assert.Greater(award, 10);
        }

        [Test]
        public void MissAwardsNothing()
        {
            Assert.AreEqual(0, TicketMath.CalculateAward(5, TimingRating.Miss, 30, 2f));
        }

        [TestCase(0, 1f)]
        [TestCase(5, 1.05f)]
        [TestCase(10, 1.10f)]
        [TestCase(15, 1.20f)]
        [TestCase(20, 1.35f)]
        [TestCase(30, 1.50f)]
        public void ComboMultiplierUsesConfiguredTiers(int combo, float expected)
        {
            Assert.AreEqual(expected, TicketMath.GetComboTicketMultiplier(combo));
        }

        [Test]
        public void OperatorComboMultiplierCanBeRebalancedWithoutChangingCode()
        {
            float multiplier = TicketMath.GetComboTicketMultiplier(10, 1.1f, 3.25f, 3.5f, 4f, 4.5f);
            Assert.AreEqual(3.25f, multiplier);
        }
    }
}
#endif
