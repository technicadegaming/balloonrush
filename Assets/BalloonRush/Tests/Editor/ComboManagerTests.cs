#if UNITY_EDITOR
using BalloonRush.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace BalloonRush.Tests
{
    public sealed class ComboManagerTests
    {
        [Test]
        public void SuccessBuildsComboAndMissResetsIt()
        {
            GameObject gameObject = new GameObject("Combo Test");
            ComboManager combo = gameObject.AddComponent<ComboManager>();
            combo.Configure(3f);
            combo.ResetSession();

            combo.RegisterSuccessfulPop();
            combo.RegisterSuccessfulPop();
            Assert.AreEqual(2, combo.CurrentCombo);
            Assert.AreEqual(2, combo.HighestCombo);

            combo.RegisterMiss();
            Assert.AreEqual(0, combo.CurrentCombo);
            Assert.AreEqual(2, combo.HighestCombo);
            Object.DestroyImmediate(gameObject);
        }
    }
}
#endif
