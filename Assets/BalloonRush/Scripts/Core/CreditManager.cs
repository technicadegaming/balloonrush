using System;
using BalloonRush.Input;
using BalloonRush.SaveSystem;
using UnityEngine;

namespace BalloonRush.Core
{
    public sealed class CreditManager : MonoBehaviour
    {
        public int Credits { get; private set; }
        public event Action<int> CreditsChanged;

        private SettingsManager settingsManager;
        private SaveManager saveManager;
        private ArcadeInputManager inputManager;

        public void Initialize(SettingsManager settings, SaveManager save, ArcadeInputManager input)
        {
            settingsManager = settings;
            saveManager = save;
            inputManager = input;

            if (inputManager != null)
            {
                inputManager.CreditPulse -= HandleCreditPulse;
                inputManager.CreditPulse += HandleCreditPulse;
            }
        }

        private void OnDestroy()
        {
            if (inputManager != null)
            {
                inputManager.CreditPulse -= HandleCreditPulse;
            }
        }

        public bool CanStartGame()
        {
            OperatorSettings settings = settingsManager != null ? settingsManager.Current : null;
            return settings == null || settings.freePlay || Credits >= Mathf.Max(1, settings.creditsPerPlay);
        }

        public bool TryConsumePlay()
        {
            OperatorSettings settings = settingsManager != null ? settingsManager.Current : null;
            if (settings == null || settings.freePlay)
            {
                return true;
            }

            int cost = Mathf.Max(1, settings.creditsPerPlay);
            if (Credits < cost)
            {
                return false;
            }

            Credits -= cost;
            CreditsChanged?.Invoke(Credits);
            return true;
        }

        public void AddCredits(int amount)
        {
            AddCredits(amount, CreditPulseType.Coin, false);
        }

        public void AddCredits(int amount, CreditPulseType source, bool recordRevenue)
        {
            if (amount <= 0)
            {
                return;
            }

            Credits = Mathf.Clamp(Credits + amount, 0, 9999);
            CreditsChanged?.Invoke(Credits);

            if (recordRevenue)
            {
                OperatorSettings settings = settingsManager != null ? settingsManager.Current : null;
                int transactionRevenueCents = settings != null
                    ? Mathf.Max(0, settings.pricePerPlayCents)
                    : 100;
                saveManager?.RecordCredit(source, amount, transactionRevenueCents);
            }
        }

        private void HandleCreditPulse(CreditPulseType pulseType)
        {
            OperatorSettings settings = settingsManager != null ? settingsManager.Current : null;
            int creditsAdded = pulseType == CreditPulseType.CardSwipe
                ? (settings != null ? settings.cardSwipeValue : 1)
                : (settings != null ? settings.coinValue : 1);
            creditsAdded = Mathf.Max(1, creditsAdded);

            // One reader pulse represents one paid swipe. Record the transaction once
            // even if an operator later configures that swipe to grant bonus credits.
            AddCredits(creditsAdded, pulseType, false);
            int transactionRevenueCents = settings != null
                ? Mathf.Max(0, settings.pricePerPlayCents)
                : 100;
            saveManager?.RecordCredit(pulseType, creditsAdded, transactionRevenueCents);
        }
    }
}
