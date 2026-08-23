using System;
using System.Collections;
using BalloonRush.Core;
using BalloonRush.SaveSystem;
using UnityEngine;

namespace BalloonRush.Gameplay
{
    public sealed class GoldenRoundManager : MonoBehaviour
    {
        private const float FinalBalloonLeadTime = 3.5f;

        [SerializeField] private BalloonSpawner spawner;
        [SerializeField] private JackpotManager jackpotManager;

        private OperatorSettings settings;
        private bool finalSpawned;
        private bool finalResolved;
        private Coroutine endingRoutine;
        private int lastDisplayedTenth = -1;

        public bool IsActive { get; private set; }
        public float TimeRemaining { get; private set; }
        public event Action<float> TimeChanged;
        public event Action RoundStarted;
        public event Action<int, bool> RoundResolved;
        public event Action RoundEnded;

        public void Configure(BalloonSpawner configuredSpawner, JackpotManager configuredJackpot, OperatorSettings operatorSettings)
        {
            spawner = configuredSpawner;
            jackpotManager = configuredJackpot;
            settings = operatorSettings;
        }

        public bool StartGoldenRound()
        {
            if (IsActive)
            {
                return false;
            }

            if (endingRoutine != null)
            {
                StopCoroutine(endingRoutine);
                endingRoutine = null;
            }

            IsActive = true;
            finalSpawned = false;
            finalResolved = false;
            lastDisplayedTenth = -1;
            TimeRemaining = settings != null ? settings.goldenRoundDuration : 10f;
            TimeRemaining = Mathf.Clamp(TimeRemaining, 4f, 30f);
            spawner?.SetGoldenMode(true);
            GameServices.State?.ChangeState(GameState.GoldenRound);
            RoundStarted?.Invoke();
            NotifyTimeChanged(true);
            GameEvents.RaiseGoldenRoundStarted();
            return true;
        }

        public void ResolveFinalBalloon(TimingRating rating)
        {
            if (!IsActive || finalResolved)
            {
                return;
            }

            finalResolved = true;
            int reward = jackpotManager != null ? jackpotManager.ResolveFinalBalloon(rating) : 0;
            bool wonJackpot = rating == TimingRating.Perfect;
            RoundResolved?.Invoke(reward, wonJackpot);
            endingRoutine = StartCoroutine(EndAfterDelay(wonJackpot ? 4.5f : 1.2f));
        }

        public void NotifyFinalBalloonPassed()
        {
            ResolveFinalBalloon(TimingRating.Miss);
        }

        public void StopImmediate()
        {
            if (endingRoutine != null)
            {
                StopCoroutine(endingRoutine);
                endingRoutine = null;
            }

            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            spawner?.SetGoldenMode(false);
            RoundEnded?.Invoke();
            GameEvents.RaiseGoldenRoundEnded();
        }

        private void Update()
        {
            if (!IsActive || finalResolved)
            {
                return;
            }

            if (TimeRemaining > 0f)
            {
                TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
                NotifyTimeChanged(false);
            }

            if (!finalSpawned && TimeRemaining <= FinalBalloonLeadTime)
            {
                finalSpawned = spawner != null && spawner.SpawnFinalGoldenBalloon();
            }

            // Once the final balloon has spawned, let it physically reach and pass the
            // Hit Zone. BalloonManager will resolve a Miss if the player lets it go by.
            // This prevents low operator speed settings from timing out the bonus before
            // the crown balloon can become playable.
            if (TimeRemaining <= 0f && !finalSpawned)
            {
                ResolveFinalBalloon(TimingRating.Miss);
            }
        }

        private IEnumerator EndAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, delay));
            IsActive = false;
            spawner?.SetGoldenMode(false);
            endingRoutine = null;
            RoundEnded?.Invoke();
            GameEvents.RaiseGoldenRoundEnded();
        }

        private void NotifyTimeChanged(bool force)
        {
            int tenth = Mathf.CeilToInt(TimeRemaining * 10f);
            if (!force && tenth == lastDisplayedTenth)
            {
                return;
            }

            lastDisplayedTenth = tenth;
            TimeChanged?.Invoke(TimeRemaining);
        }
    }
}
