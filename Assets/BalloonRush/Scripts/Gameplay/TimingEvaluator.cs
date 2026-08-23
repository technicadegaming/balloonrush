using UnityEngine;

namespace BalloonRush.Gameplay
{
    public static class TimingEvaluator
    {
        public static TimingRating Evaluate(
            float balloonY,
            float hitZoneCenterY,
            float hitZoneHalfHeight,
            float perfectWindow,
            float greatWindow,
            float goodWindow,
            float difficultyScale = 1f)
        {
            float safeHalfHeight = Mathf.Max(0.001f, hitZoneHalfHeight);
            float normalizedDistance = Mathf.Abs(balloonY - hitZoneCenterY) / safeHalfHeight;
            float scale = Mathf.Clamp(difficultyScale, 0.25f, 2f);
            float perfect = Mathf.Clamp01(perfectWindow * scale);
            float great = Mathf.Clamp(greatWindow * scale, perfect, 1f);
            float good = Mathf.Clamp(goodWindow * scale, great, 1f);

            if (normalizedDistance <= perfect) return TimingRating.Perfect;
            if (normalizedDistance <= great) return TimingRating.Great;
            if (normalizedDistance <= good) return TimingRating.Good;
            return TimingRating.Miss;
        }

        public static float GetScoreMultiplier(TimingRating rating)
        {
            switch (rating)
            {
                case TimingRating.Perfect: return 2f;
                case TimingRating.Great: return 1.5f;
                case TimingRating.Good: return 1f;
                default: return 0f;
            }
        }
    }
}
