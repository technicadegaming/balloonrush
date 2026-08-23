using System;
using BalloonRush.Gameplay;
using BalloonRush.SaveSystem;
using UnityEngine;

namespace BalloonRush.Core
{
    public sealed class RoundManager : MonoBehaviour
    {
        [SerializeField] private BalloonSpawner spawner;
        [SerializeField] private DifficultyManager difficultyManager;
        [SerializeField] private GoldenRoundManager goldenRoundManager;

        private OperatorSettings settings;
        private bool waitingForGoldenRoundToFinish;
        private int lastDisplayedTenth = -1;

        public bool IsRunning { get; private set; }
        public bool IsRushMode { get; private set; }
        public float Duration { get; private set; } = 35f;
        public float RemainingTime { get; private set; }
        public float NormalizedProgress => Duration > 0f ? Mathf.Clamp01(1f - RemainingTime / Duration) : 1f;

        public event Action<float> TimeChanged;
        public event Action<bool> RushModeChanged;
        public event Action RoundFinished;

        public void Configure(
            BalloonSpawner configuredSpawner,
            DifficultyManager configuredDifficulty,
            GoldenRoundManager configuredGoldenRound,
            OperatorSettings operatorSettings)
        {
            spawner = configuredSpawner;
            difficultyManager = configuredDifficulty;
            settings = operatorSettings;

            if (goldenRoundManager != null)
            {
                goldenRoundManager.RoundEnded -= HandleGoldenRoundEnded;
            }
            goldenRoundManager = configuredGoldenRound;
            if (goldenRoundManager != null)
            {
                goldenRoundManager.RoundEnded += HandleGoldenRoundEnded;
            }
        }

        private void OnDestroy()
        {
            if (goldenRoundManager != null)
            {
                goldenRoundManager.RoundEnded -= HandleGoldenRoundEnded;
            }
        }

        public void BeginRound()
        {
            Duration = settings != null ? settings.gameDuration : 35f;
            Duration = Mathf.Clamp(Duration, 20f, 120f);
            RemainingTime = Duration;
            IsRunning = true;
            IsRushMode = false;
            waitingForGoldenRoundToFinish = false;
            lastDisplayedTenth = -1;
            difficultyManager?.SetProgress(0f);
            spawner?.BeginSpawning();
            GameServices.State?.ChangeState(GameState.Playing);
            RushModeChanged?.Invoke(false);
            NotifyTimeChanged(true);
        }

        public void EndRoundNow()
        {
            if (!IsRunning)
            {
                return;
            }

            RemainingTime = 0f;
            CompleteRound();
        }

        public void StopImmediate()
        {
            IsRunning = false;
            waitingForGoldenRoundToFinish = false;
            spawner?.StopSpawning();
        }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            if (RemainingTime > 0f)
            {
                RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);
                difficultyManager?.SetProgress(NormalizedProgress);
                NotifyTimeChanged(false);
            }

            if (!IsRushMode && RemainingTime <= 5f && RemainingTime > 0f)
            {
                IsRushMode = true;
                spawner?.SetRushMode(true);
                if (goldenRoundManager == null || !goldenRoundManager.IsActive)
                {
                    GameServices.State?.ChangeState(GameState.RushMode);
                }
                RushModeChanged?.Invoke(true);
            }

            if (RemainingTime > 0f)
            {
                return;
            }

            if (goldenRoundManager != null && goldenRoundManager.IsActive)
            {
                waitingForGoldenRoundToFinish = true;
                spawner?.StopSpawning();
                return;
            }

            CompleteRound();
        }

        private void HandleGoldenRoundEnded()
        {
            if (!IsRunning)
            {
                return;
            }

            if (waitingForGoldenRoundToFinish || RemainingTime <= 0f)
            {
                CompleteRound();
                return;
            }

            GameServices.State?.ChangeState(IsRushMode ? GameState.RushMode : GameState.Playing);
        }

        private void CompleteRound()
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            waitingForGoldenRoundToFinish = false;
            RemainingTime = 0f;
            spawner?.StopSpawning();
            GameServices.State?.ChangeState(GameState.GameOver);
            NotifyTimeChanged(true);
            RoundFinished?.Invoke();
        }

        private void NotifyTimeChanged(bool force)
        {
            int tenth = Mathf.CeilToInt(RemainingTime * 10f);
            if (!force && tenth == lastDisplayedTenth)
            {
                return;
            }

            lastDisplayedTenth = tenth;
            TimeChanged?.Invoke(RemainingTime);
        }
    }
}
