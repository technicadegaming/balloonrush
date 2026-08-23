using UnityEngine;

namespace BalloonRush.Core
{
    [CreateAssetMenu(menuName = "Balloon Rush/Difficulty Config", fileName = "DifficultyConfig")]
    public sealed class DifficultyConfig : ScriptableObject
    {
        [Tooltip("Multiplier applied to base balloon speed over normalized round progress.")]
        public AnimationCurve speedMultiplier = AnimationCurve.EaseInOut(0f, 1f, 1f, 1.85f);

        [Tooltip("Multiplier applied to spawn interval over normalized round progress.")]
        public AnimationCurve spawnIntervalMultiplier = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.55f);

        [Tooltip("Multiplier applied to dangerous balloon weights over normalized round progress.")]
        public AnimationCurve dangerMultiplier = AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.6f);

        [Tooltip("Scale applied to timing windows. Lower values are harder.")]
        public AnimationCurve timingWindowScale = AnimationCurve.EaseInOut(0f, 1.15f, 1f, 0.85f);
    }
}
