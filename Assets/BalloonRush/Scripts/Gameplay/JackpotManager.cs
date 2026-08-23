using System;
using BalloonRush.Core;
using BalloonRush.SaveSystem;
using UnityEngine;

namespace BalloonRush.Gameplay
{
    public sealed class JackpotManager : MonoBehaviour
    {
        [SerializeField] private ScoreManager scoreManager;

        private OperatorSettings settings;

        public bool WasWon { get; private set; }
        public event Action<int, bool> JackpotResolved;

        public void Configure(ScoreManager score, OperatorSettings operatorSettings)
        {
            scoreManager = score;
            settings = operatorSettings;
        }

        public void ResetSession()
        {
            WasWon = false;
        }

        public int ResolveFinalBalloon(TimingRating rating)
        {
            bool jackpot = rating == TimingRating.Perfect;
            int configuredReward;
            int applied;

            if (jackpot)
            {
                configuredReward = settings != null ? settings.jackpotTickets : 500;
                configuredReward = Mathf.Clamp(configuredReward, 1, 500);
                applied = scoreManager != null ? scoreManager.AddJackpotTickets(configuredReward) : configuredReward;
                WasWon = true;
                scoreManager?.MarkJackpotWon();
                GameEvents.RaiseJackpotWon(applied);
            }
            else
            {
                if (rating == TimingRating.Great)
                {
                    configuredReward = settings != null ? settings.goldenGreatReward : 25;
                }
                else if (rating == TimingRating.Good)
                {
                    configuredReward = settings != null ? settings.goldenGoodReward : 10;
                }
                else
                {
                    configuredReward = settings != null ? settings.goldenMissReward : 3;
                }

                applied = scoreManager != null ? scoreManager.AddBonusTickets(configuredReward) : configuredReward;
            }

            JackpotResolved?.Invoke(applied, jackpot);
            return applied;
        }
    }
}
