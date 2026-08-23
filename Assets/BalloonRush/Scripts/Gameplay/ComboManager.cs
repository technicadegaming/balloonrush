using System;
using BalloonRush.Core;
using UnityEngine;

namespace BalloonRush.Gameplay
{
    public sealed class ComboManager : MonoBehaviour
    {
        private static readonly int[] DefaultMilestones = { 5, 10, 15, 20, 30 };

        [SerializeField, Min(0.25f)] private float comboTimeout = 2.75f;
        [SerializeField] private int[] milestones = { 5, 10, 15, 20, 30 };

        private float lastSuccessfulHitTime;
        private bool running;

        public int CurrentCombo { get; private set; }
        public int HighestCombo { get; private set; }
        public float NormalizedTimeoutRemaining
        {
            get
            {
                if (CurrentCombo <= 0 || comboTimeout <= 0f) return 0f;
                float elapsed = Time.time - lastSuccessfulHitTime;
                return Mathf.Clamp01(1f - elapsed / comboTimeout);
            }
        }

        public event Action<int> ComboChanged;
        public event Action<int> MilestoneReached;

        public void Configure(float timeout, int[] configuredMilestones = null)
        {
            comboTimeout = Mathf.Max(0.25f, timeout);
            milestones = configuredMilestones != null && configuredMilestones.Length > 0
                ? configuredMilestones
                : DefaultMilestones;
        }

        public void ResetSession()
        {
            CurrentCombo = 0;
            HighestCombo = 0;
            lastSuccessfulHitTime = 0f;
            running = false;
            NotifyChanged();
        }

        public void SetRunning(bool shouldRun)
        {
            running = shouldRun;
            if (running && CurrentCombo > 0)
            {
                lastSuccessfulHitTime = Time.time;
            }
        }

        public int RegisterSuccessfulPop()
        {
            CurrentCombo++;
            HighestCombo = Mathf.Max(HighestCombo, CurrentCombo);
            lastSuccessfulHitTime = Time.time;
            NotifyChanged();

            if (IsMilestone(CurrentCombo))
            {
                MilestoneReached?.Invoke(CurrentCombo);
            }

            return CurrentCombo;
        }

        public int AddCombo(int amount)
        {
            if (amount <= 0)
            {
                return CurrentCombo;
            }

            int oldCombo = CurrentCombo;
            CurrentCombo += amount;
            HighestCombo = Mathf.Max(HighestCombo, CurrentCombo);
            lastSuccessfulHitTime = Time.time;
            NotifyChanged();

            for (int value = oldCombo + 1; value <= CurrentCombo; value++)
            {
                if (IsMilestone(value))
                {
                    MilestoneReached?.Invoke(value);
                }
            }

            return CurrentCombo;
        }

        public void RegisterMiss()
        {
            ResetCurrentCombo();
        }

        public void ResetCurrentCombo()
        {
            if (CurrentCombo == 0)
            {
                return;
            }

            CurrentCombo = 0;
            NotifyChanged();
        }

        private void Update()
        {
            if (!running || CurrentCombo <= 0 || comboTimeout <= 0f)
            {
                return;
            }

            if (Time.time - lastSuccessfulHitTime >= comboTimeout)
            {
                ResetCurrentCombo();
            }
        }

        private bool IsMilestone(int combo)
        {
            if (milestones == null)
            {
                return false;
            }

            for (int i = 0; i < milestones.Length; i++)
            {
                if (milestones[i] == combo)
                {
                    return true;
                }
            }

            return false;
        }

        private void NotifyChanged()
        {
            ComboChanged?.Invoke(CurrentCombo);
            GameEvents.RaiseComboChanged(CurrentCombo);
        }
    }
}
