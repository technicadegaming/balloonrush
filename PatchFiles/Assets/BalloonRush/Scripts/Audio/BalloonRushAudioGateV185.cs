using System.Collections.Generic;
using UnityEngine;

namespace BalloonRush.Audio
{
    /// <summary>
    /// v1.8.5 lightweight SFX burst limiter.
    ///
    /// It prevents several non-critical sounds from firing in the same tiny
    /// time window while allowing important arcade events through.
    /// </summary>
    public static class BalloonRushAudioGateV185
    {
        private static readonly Dictionary<AudioCue, float> LastCueTime =
            new Dictionary<AudioCue, float>();

        private static readonly Queue<float> RecentNonCritical =
            new Queue<float>();

        private static float lastPopFamilyTime = -100f;

        public static bool Allow(AudioCue cue)
        {
            float now = Time.unscaledTime;
            float cooldown = GetCooldown(cue);

            if (LastCueTime.TryGetValue(cue, out float last) &&
                now - last < cooldown)
            {
                return false;
            }

            if (IsPopFamily(cue) &&
                now - lastPopFamilyTime < 0.045f)
            {
                return false;
            }

            while (RecentNonCritical.Count > 0 &&
                   now - RecentNonCritical.Peek() > 0.12f)
            {
                RecentNonCritical.Dequeue();
            }

            bool critical = IsCritical(cue);

            if (!critical && RecentNonCritical.Count >= 4)
                return false;

            LastCueTime[cue] = now;

            if (IsPopFamily(cue))
                lastPopFamilyTime = now;

            if (!critical)
                RecentNonCritical.Enqueue(now);

            return true;
        }

        public static float GetVolumeScale(AudioCue cue)
        {
            switch (cue)
            {
                case AudioCue.Jackpot:
                    return 0.95f;

                case AudioCue.BombExplosion:
                    return 0.84f;

                case AudioCue.BonusStart:
                case AudioCue.GoldenBalloonPop:
                case AudioCue.ComboMilestone:
                    return 0.78f;

                case AudioCue.GoldenBalloonAppear:
                    return 0.68f;

                case AudioCue.PerfectPop:
                    return 0.72f;

                case AudioCue.GreatPop:
                    return 0.64f;

                case AudioCue.GoodPop:
                    return 0.58f;

                case AudioCue.BalloonPop:
                    return 0.52f;

                case AudioCue.Miss:
                    return 0.54f;

                case AudioCue.Countdown:
                case AudioCue.GameOver:
                    return 0.60f;

                case AudioCue.ButtonClick:
                    return 0.42f;

                case AudioCue.LaneMove:
                    return 0.36f;

                case AudioCue.TicketCount:
                    return 0.30f;

                case AudioCue.ComboIncrease:
                    return 0.34f;

                default:
                    return 0.62f;
            }
        }

        private static float GetCooldown(AudioCue cue)
        {
            switch (cue)
            {
                case AudioCue.BalloonPop:
                case AudioCue.PerfectPop:
                case AudioCue.GreatPop:
                case AudioCue.GoodPop:
                    return 0.045f;

                case AudioCue.Miss:
                    return 0.085f;

                case AudioCue.ButtonClick:
                    return 0.060f;

                case AudioCue.LaneMove:
                    return 0.055f;

                case AudioCue.ComboIncrease:
                    return 0.090f;

                case AudioCue.ComboMilestone:
                    return 0.180f;

                case AudioCue.GoldenBalloonAppear:
                case AudioCue.GoldenBalloonPop:
                case AudioCue.BonusStart:
                    return 0.120f;

                case AudioCue.Countdown:
                    return 0.100f;

                case AudioCue.TicketCount:
                    return 0.040f;

                case AudioCue.Jackpot:
                    return 0.250f;

                default:
                    return 0.030f;
            }
        }

        private static bool IsPopFamily(AudioCue cue)
        {
            return cue == AudioCue.BalloonPop ||
                   cue == AudioCue.PerfectPop ||
                   cue == AudioCue.GreatPop ||
                   cue == AudioCue.GoodPop;
        }

        private static bool IsCritical(AudioCue cue)
        {
            switch (cue)
            {
                case AudioCue.BombExplosion:
                case AudioCue.ComboMilestone:
                case AudioCue.GoldenBalloonAppear:
                case AudioCue.GoldenBalloonPop:
                case AudioCue.BonusStart:
                case AudioCue.GameOver:
                case AudioCue.Jackpot:
                    return true;

                default:
                    return false;
            }
        }
    }
}
