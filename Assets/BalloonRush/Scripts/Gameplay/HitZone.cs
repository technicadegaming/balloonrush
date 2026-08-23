using System.Collections;
using BalloonRush.SaveSystem;
using UnityEngine;

namespace BalloonRush.Gameplay
{
    public sealed class HitZone : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float halfHeight = 0.82f;
        [SerializeField] private SpriteRenderer[] borderRenderers;
        [SerializeField] private Color idleColor = new Color(0.1f, 0.9f, 1f, 0.95f);
        [SerializeField] private Color perfectColor = new Color(1f, 0.85f, 0.1f, 1f);
        [SerializeField] private Color missColor = new Color(1f, 0.12f, 0.18f, 1f);

        private OperatorSettings settings;
        private Coroutine flashRoutine;

        public float CenterY => transform.position.y;
        public float HalfHeight => halfHeight;

        public void Configure(float configuredHalfHeight, SpriteRenderer[] borders)
        {
            halfHeight = Mathf.Max(0.1f, configuredHalfHeight);
            borderRenderers = borders;
        }

        public void ApplySettings(OperatorSettings operatorSettings)
        {
            settings = operatorSettings;
        }

        public TimingRating Evaluate(float balloonY, float difficultyScale)
        {
            float perfect = settings != null ? settings.perfectWindow : 0.20f;
            float great = settings != null ? settings.greatWindow : 0.45f;
            float good = settings != null ? settings.goodWindow : 0.75f;
            return TimingEvaluator.Evaluate(balloonY, CenterY, halfHeight, perfect, great, good, difficultyScale);
        }

        public bool IsInside(float balloonY)
        {
            return Mathf.Abs(balloonY - CenterY) <= halfHeight;
        }

        public void Flash(TimingRating rating)
        {
            Color target = rating == TimingRating.Miss ? missColor : (rating == TimingRating.Perfect ? perfectColor : Color.green);
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }
            flashRoutine = StartCoroutine(FlashRoutine(target));
        }

        private void Update()
        {
            if (flashRoutine != null || borderRenderers == null)
            {
                return;
            }

            float pulse = 0.78f + Mathf.Sin(Time.unscaledTime * 4f) * 0.18f;
            Color pulsed = idleColor;
            pulsed.a *= pulse;
            SetBorderColor(pulsed);
        }

        private IEnumerator FlashRoutine(Color target)
        {
            SetBorderColor(target);
            yield return new WaitForSecondsRealtime(0.12f);
            float elapsed = 0f;
            const float duration = 0.22f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetBorderColor(Color.Lerp(target, idleColor, elapsed / duration));
                yield return null;
            }
            flashRoutine = null;
        }

        private void SetBorderColor(Color color)
        {
            if (borderRenderers == null)
            {
                return;
            }

            for (int i = 0; i < borderRenderers.Length; i++)
            {
                if (borderRenderers[i] != null)
                {
                    borderRenderers[i].color = color;
                }
            }
        }
    }
}
