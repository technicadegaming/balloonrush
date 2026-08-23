#if UNITY_EDITOR
using BalloonRush.Gameplay;
using BalloonRush.SaveSystem;
using NUnit.Framework;

namespace BalloonRush.Tests
{
    public sealed class OperatorSettingsTests
    {
        [Test]
        public void RedemptionCapsCannotExceedCommercialLimits()
        {
            OperatorSettings settings = new OperatorSettings
            {
                jackpotTickets = 900,
                maxTicketPayout = 5000
            };

            settings.Validate();

            Assert.AreEqual(500, settings.jackpotTickets);
            Assert.AreEqual(1000, settings.maxTicketPayout);
        }

        [Test]
        public void ComboMultipliersRemainMonotonicAfterValidation()
        {
            OperatorSettings settings = new OperatorSettings
            {
                combo5Multiplier = 2f,
                combo10Multiplier = 1f,
                combo15Multiplier = 4f,
                combo20Multiplier = 3f,
                combo30Multiplier = 2f
            };

            settings.Validate();

            Assert.GreaterOrEqual(settings.combo10Multiplier, settings.combo5Multiplier);
            Assert.GreaterOrEqual(settings.combo15Multiplier, settings.combo10Multiplier);
            Assert.GreaterOrEqual(settings.combo20Multiplier, settings.combo15Multiplier);
            Assert.GreaterOrEqual(settings.combo30Multiplier, settings.combo20Multiplier);
        }

        [Test]
        public void CommercialDefaultProfileUsesOneDollarAndControlledPayouts()
        {
            OperatorSettings settings = new OperatorSettings();
            settings.Validate();

            Assert.AreEqual(100, settings.pricePerPlayCents);
            Assert.AreEqual(1, settings.cardSwipeValue);
            Assert.AreEqual(1, settings.creditsPerPlay);
            Assert.AreEqual(500, settings.jackpotTickets);
            Assert.AreEqual(125, settings.regularTicketCap);
            Assert.AreEqual(625, settings.maxTicketPayout);
            Assert.AreEqual(30f, settings.gameDuration);
            Assert.Greater(settings.greenSpawnWeight, 0f);
            Assert.Greater(settings.blueSpawnWeight, 0f);
            Assert.LessOrEqual(settings.combo30Multiplier, 1.15f);
            Assert.LessOrEqual(settings.x2Duration, 2.5f);
        }

        [Test]
        public void EconomicsTargetUsesConfiguredPrizeCost()
        {
            OperatorSettings settings = new OperatorSettings
            {
                pricePerPlayCents = 100,
                targetPrizeCostPercent = 20f,
                estimatedPrizeCostPerTicketCents = 0.5f
            };
            settings.Validate();

            Assert.AreEqual(40, EconomyMath.CalculateTargetAverageTickets(settings));
        }

        [Test]
        public void InputDebouncesAreClampedToCabinetSafeRanges()
        {
            OperatorSettings settings = new OperatorSettings
            {
                inputDebounceMilliseconds = 999,
                cardSwipeDebounceMilliseconds = 99999
            };
            settings.Validate();
            Assert.AreEqual(250, settings.inputDebounceMilliseconds);
            Assert.AreEqual(5000, settings.cardSwipeDebounceMilliseconds);

            settings.inputDebounceMilliseconds = -10;
            settings.Validate();
            Assert.AreEqual(0, settings.inputDebounceMilliseconds);
        }

        [Test]
        public void TimingTicketMultipliersRemainMonotonicAfterValidation()
        {
            OperatorSettings settings = new OperatorSettings
            {
                goodTicketMultiplier = 1.5f,
                greatTicketMultiplier = 0.5f,
                perfectTicketMultiplier = 1.0f
            };

            settings.Validate();

            Assert.GreaterOrEqual(settings.greatTicketMultiplier, settings.goodTicketMultiplier);
            Assert.GreaterOrEqual(settings.perfectTicketMultiplier, settings.greatTicketMultiplier);
        }
    }
}
#endif
