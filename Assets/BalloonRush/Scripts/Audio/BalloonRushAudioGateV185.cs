using System.Collections.Generic;
using UnityEngine;

namespace BalloonRush.Audio
{
    /// <summary>
    /// Balloon Rush v1.8.5 audio de-clutter gate.
    ///
    /// Designed to work even if BalloonManager still contains the older
    /// generic-pop + combo-chirp + timing-cue sequence.
    /// </summary>
    public static class BalloonRushAudioGateV185
    {
        private static readonly Dictionary<AudioCue, float> LastCueTime =
            new Dictionary<AudioCue, float>();

        private static readonly Queue<float> RecentNonCritical =
            new Queue<float>();

        private static float lastNormalPopAttempt = -100f;
        private static float lastTimingCue = -100f;
        private static float lastGoldenEvent = -100f;

        public static bool Allow(AudioCue cue)
        {
            float now = Time.unscaledTime;

            // The gameplay code historically plays:
            // BalloonPop -> ComboIncrease -> Perfect/Great/Good.
            // We intentionally discard the generic pop so the timing-quality
            // sound becomes the one clear successful-pop sound.
            if (cue == AudioCue.BalloonPop)
            {
                lastNormalPopAttempt = now;
                return false;
            }

            // Combo chirps directly after a normal pop are redundant and are
            // the biggest source of "too many sounds at once".
            if (cue == AudioCue.ComboIncrease &&
                now - lastNormalPopAttempt < 0.16f)
            {
                return false;
            }

            if (IsTimingCue(cue))
            {
                lastTimingCue = now;
            }

            // Golden transitions can generate several cues in the same frame.
            // AudioManager will replace the previous event instance; the gate
            // also prevents a rapid duplicate of the same golden transition.
            if (cue == AudioCue.GoldenBalloonPop ||
                cue == AudioCue.BonusStart)
            {
                if (now - lastGoldenEvent < 0.035f &&
                    cue == AudioCue.GoldenBalloonPop)
                {
                    return false;
                }

                lastGoldenEvent = now;
            }

            float cooldown = GetCooldown(cue);

            if (LastCueTime.TryGetValue(cue, out float last) &&
                now - last < cooldown)
            {
                return false;
            }

            while (RecentNonCritical.Count > 0 &&
                   now - RecentNonCritical.Peek() > 0.14f)
            {
                RecentNonCritical.Dequeue();
            }

            bool critical = IsCritical(cue);

            if (!critical && RecentNonCritical.Count >= 3)
            {
                return false;
            }

            LastCueTime[cue] = now;

            if (!critical)
            {
                RecentNonCritical.Enqueue(now);
            }

            return true;
        }

        public static float GetVolumeScale(AudioCue cue)
        {
            switch (cue)
            {
                case AudioCue.Jackpot:
                    return 0.95f;

                case AudioCue.BombExplosion:
                    return 0.82f;

                case AudioCue.BonusStart:
                case AudioCue.GoldenBalloonPop:
                    return 0.78f;

                case AudioCue.ComboMilestone:
                    return 0.74f;

                case AudioCue.GoldenBalloonAppear:
                    return 0.66f;

                case AudioCue.PerfectPop:
                    return 0.72f;

                case AudioCue.GreatPop:
                    return 0.63f;

                case AudioCue.GoodPop:
                    return 0.56f;

                case AudioCue.Miss:
                    return 0.50f;

                case AudioCue.GameOver:
                case AudioCue.Countdown:
                    return 0.58f;

                case AudioCue.ButtonClick:
                    return 0.38f;

                case AudioCue.LaneMove:
                    return 0.32f;

                case AudioCue.TicketCount:
                    return 0.26f;

                case AudioCue.ComboIncrease:
                    return 0.28f;

                default:
                    return 0.60f;
            }
        }

        private static bool IsTimingCue(AudioCue cue)
        {
            return cue == AudioCue.PerfectPop ||
                   cue == AudioCue.GreatPop ||
                   cue == AudioCue.GoodPop;
        }

        private static float GetCooldown(AudioCue cue)
        {
            switch (cue)
            {
                case AudioCue.PerfectPop:
                case AudioCue.GreatPop:
                case AudioCue.GoodPop:
                    return 0.045f;

                case AudioCue.Miss:
                    return 0.085f;

                case AudioCue.ButtonClick:
                    return 0.070f;

                case AudioCue.LaneMove:
                    return 0.060f;

                case AudioCue.ComboIncrease:
                    return 0.120f;

                case AudioCue.ComboMilestone:
                    return 0.180f;

                case AudioCue.GoldenBalloonAppear:
                case AudioCue.GoldenBalloonPop:
                case AudioCue.BonusStart:
                    return 0.100f;

                case AudioCue.Countdown:
                    return 0.100f;

                case AudioCue.TicketCount:
                    return 0.045f;

                case AudioCue.Jackpot:
                    return 0.300f;

                default:
                    return 0.035f;
            }
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
