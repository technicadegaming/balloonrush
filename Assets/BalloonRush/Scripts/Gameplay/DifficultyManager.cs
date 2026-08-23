using System.Collections;
using BalloonRush.Core;
using BalloonRush.SaveSystem;
using UnityEngine;

namespace BalloonRush.Gameplay
{
    public sealed class DifficultyManager : MonoBehaviour
    {
        [SerializeField] private DifficultyConfig config;

        private OperatorSettings settings;
        private float normalizedProgress;
        private float temporarySpeedScale = 1f;
        private Coroutine slowdownRoutine;

        public float NormalizedProgress => normalizedProgress;
        public string CurrentDifficultyLabel
        {
            get
            {
                if (normalizedProgress < 0.25f) return "Easy";
                if (normalizedProgress < 0.5f) return "Medium";
                if (normalizedProgress < 0.75f) return "Fast";
                return "High Intensity";
            }
        }

        public void Configure(DifficultyConfig difficultyConfig, OperatorSettings operatorSettings)
        {
            config = difficultyConfig;
            settings = operatorSettings;
            normalizedProgress = 0f;
            temporarySpeedScale = 1f;
        }

        public void SetProgress(float progress)
        {
            normalizedProgress = Mathf.Clamp01(progress);
        }

        public float GetBalloonSpeed(bool rushMode, bool goldenMode)
        {
            float baseSpeed = settings != null ? settings.balloonBaseSpeed : 2.65f;
            float curve = config != null && config.speedMultiplier != null
                ? config.speedMultiplier.Evaluate(normalizedProgress)
                : Mathf.Lerp(1f, 1.85f, normalizedProgress);
            float modeScale = rushMode ? 1.18f : 1f;
            if (goldenMode) modeScale *= 1.10f;
            return baseSpeed * curve * modeScale * temporarySpeedScale;
        }

        public float GetSpawnInterval(bool rushMode, bool goldenMode)
        {
            float baseInterval = settings != null ? settings.spawnInterval : 1.0f;
            float curve = config != null && config.spawnIntervalMultiplier != null
                ? config.spawnIntervalMultiplier.Evaluate(normalizedProgress)
                : Mathf.Lerp(1f, 0.55f, normalizedProgress);
            float modeScale = rushMode ? 0.72f : 1f;
            if (goldenMode) modeScale *= 0.78f;
            return Mathf.Clamp(baseInterval * curve * modeScale, 0.18f, 3f);
        }

        public float GetDangerMultiplier()
        {
            return config != null && config.dangerMultiplier != null
                ? config.dangerMultiplier.Evaluate(normalizedProgress)
                : Mathf.Lerp(0.5f, 1.6f, normalizedProgress);
        }

        public float GetTimingWindowScale()
        {
            return config != null && config.timingWindowScale != null
                ? config.timingWindowScale.Evaluate(normalizedProgress)
                : Mathf.Lerp(1.15f, 0.85f, normalizedProgress);
        }

        public void ApplyTemporarySlowdown(float speedScale, float duration)
        {
            if (slowdownRoutine != null)
            {
                StopCoroutine(slowdownRoutine);
            }
            slowdownRoutine = StartCoroutine(SlowdownRoutine(speedScale, duration));
        }

        private IEnumerator SlowdownRoutine(float speedScale, float duration)
        {
            temporarySpeedScale = Mathf.Clamp(speedScale, 0.25f, 1f);
            yield return new WaitForSeconds(Mathf.Max(0.1f, duration));
            temporarySpeedScale = 1f;
            slowdownRoutine = null;
        }
    }
}
