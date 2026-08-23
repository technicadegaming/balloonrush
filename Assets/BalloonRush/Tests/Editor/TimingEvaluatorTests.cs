#if UNITY_EDITOR
using BalloonRush.Gameplay;
using NUnit.Framework;

namespace BalloonRush.Tests
{
    public sealed class TimingEvaluatorTests
    {
        [Test]
        public void CenterHitIsPerfect()
        {
            TimingRating rating = TimingEvaluator.Evaluate(3f, 3f, 1f, 0.2f, 0.45f, 0.75f);
            Assert.AreEqual(TimingRating.Perfect, rating);
        }

        [Test]
        public void WindowBoundariesReturnExpectedRatings()
        {
            Assert.AreEqual(TimingRating.Perfect, TimingEvaluator.Evaluate(0.19f, 0f, 1f, 0.2f, 0.45f, 0.75f));
            Assert.AreEqual(TimingRating.Great, TimingEvaluator.Evaluate(0.44f, 0f, 1f, 0.2f, 0.45f, 0.75f));
            Assert.AreEqual(TimingRating.Good, TimingEvaluator.Evaluate(0.74f, 0f, 1f, 0.2f, 0.45f, 0.75f));
            Assert.AreEqual(TimingRating.Miss, TimingEvaluator.Evaluate(0.76f, 0f, 1f, 0.2f, 0.45f, 0.75f));
        }
    }
}
#endif
