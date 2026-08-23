using System.Collections;
using BalloonRush.SaveSystem;
using UnityEngine;

namespace BalloonRush.Effects
{
    public sealed class ScreenShake : MonoBehaviour
    {
        [SerializeField] private Transform shakeTarget;

        private Vector3 baseLocalPosition;
        private Coroutine shakeRoutine;
        private SettingsManager settingsManager;

        public void Configure(Transform target, SettingsManager settings)
        {
            shakeTarget = target != null ? target : transform;
            baseLocalPosition = shakeTarget.localPosition;
            settingsManager = settings;
        }

        public void Shake(float amplitude, float duration)
        {
            if (shakeTarget == null)
            {
                shakeTarget = transform;
                baseLocalPosition = shakeTarget.localPosition;
            }

            if (settingsManager != null && settingsManager.Current != null && settingsManager.Current.reducedScreenShake)
            {
                amplitude *= 0.3f;
                duration *= 0.6f;
            }

            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
            }
            shakeRoutine = StartCoroutine(ShakeRoutine(Mathf.Max(0f, amplitude), Mathf.Max(0.01f, duration)));
        }

        private IEnumerator ShakeRoutine(float amplitude, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float fade = 1f - Mathf.Clamp01(elapsed / duration);
                Vector2 offset = Random.insideUnitCircle * amplitude * fade;
                shakeTarget.localPosition = baseLocalPosition + new Vector3(offset.x, offset.y, 0f);
                yield return null;
            }

            shakeTarget.localPosition = baseLocalPosition;
            shakeRoutine = null;
        }
    }
}
