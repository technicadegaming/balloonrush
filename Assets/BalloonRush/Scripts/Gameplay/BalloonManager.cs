using System.Collections.Generic;
using BalloonRush.Audio;
using BalloonRush.Core;
using BalloonRush.Effects;
using BalloonRush.SaveSystem;
using BalloonRush.UI;
using UnityEngine;

namespace BalloonRush.Gameplay
{
    public sealed class BalloonManager : MonoBehaviour
    {
        [SerializeField] private LaneManager laneManager;
        [SerializeField] private HitZone hitZone;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private ComboManager comboManager;
        [SerializeField] private DifficultyManager difficultyManager;
        [SerializeField] private GoldenRoundManager goldenRoundManager;
        [SerializeField] private EffectsManager effectsManager;
        [SerializeField] private UIManager uiManager;

        private readonly List<Balloon> activeBalloons = new List<Balloon>(64);
        private OperatorSettings settings;
        private bool gameplayActive;
        private float nextPopAllowedTime;

        public int ActiveBalloonCount => activeBalloons.Count;
        public bool GameplayActive => gameplayActive;

        public void Configure(
            LaneManager lanes,
            HitZone configuredHitZone,
            ScoreManager score,
            ComboManager combo,
            DifficultyManager difficulty,
            GoldenRoundManager golden,
            EffectsManager effects,
            UIManager ui,
            OperatorSettings operatorSettings)
        {
            if (comboManager != null)
            {
                comboManager.MilestoneReached -= HandleComboMilestone;
            }

            laneManager = lanes;
            hitZone = configuredHitZone;
            scoreManager = score;
            comboManager = combo;
            difficultyManager = difficulty;
            goldenRoundManager = golden;
            effectsManager = effects;
            uiManager = ui;
            settings = operatorSettings;

            if (comboManager != null)
            {
                comboManager.MilestoneReached += HandleComboMilestone;
            }
        }

        private void OnDestroy()
        {
            if (comboManager != null)
            {
                comboManager.MilestoneReached -= HandleComboMilestone;
            }
        }

        public void SetGameplayActive(bool active)
        {
            gameplayActive = active;
        }

        public void RegisterSpawnedBalloon(Balloon balloon)
        {
            if (balloon != null && !activeBalloons.Contains(balloon))
            {
                activeBalloons.Add(balloon);
                if (balloon.Definition != null && balloon.Definition.Kind == BalloonKind.GoldenTrigger)
                {
                    GameServices.Audio?.PlaySfx(AudioCue.GoldenBalloonAppear);
                    uiManager?.ShowMessage("GOLDEN BALLOON!", Color.yellow, 1f);
                }
            }
        }

        public void TryPopSelectedLane()
        {
            if (!gameplayActive || Time.unscaledTime < nextPopAllowedTime || laneManager == null || hitZone == null)
            {
                return;
            }

            nextPopAllowedTime = Time.unscaledTime + 0.07f;
            Balloon target = FindClosestBalloon(laneManager.SelectedLane, hitZone.CenterY, hitZone.HalfHeight);
            if (target == null)
            {
                HandleMissAt(laneManager.GetLanePosition(laneManager.SelectedLane, hitZone.CenterY));
                return;
            }

            float timingScale = difficultyManager != null ? difficultyManager.GetTimingWindowScale() : 1f;
            TimingRating rating = hitZone.Evaluate(target.transform.position.y, timingScale);
            if (rating == TimingRating.Miss)
            {
                HandleMissAt(target.transform.position);
                return;
            }

            activeBalloons.Remove(target);
            BalloonDefinition definition = target.Definition;
            if (definition == null)
            {
                target.ReleaseImmediately();
                return;
            }

            if (definition.IsDangerous)
            {
                HandleDangerousBalloon(target, definition);
                return;
            }

            int ticketAward = scoreManager != null ? scoreManager.RecordSuccessfulPop(definition, rating) : 0;
            hitZone.Flash(rating);
            uiManager?.ShowRating(rating);
            effectsManager?.PlaySuccessfulPop(target.transform.position, definition.VisualColor, rating, ticketAward);
            GameServices.Audio?.PlaySfx(AudioCue.BalloonPop, 1f, 0.45f);
            if (comboManager != null)
            {
                float comboPitch = 0.92f + Mathf.Min(0.38f, comboManager.CurrentCombo * 0.012f);
                GameServices.Audio?.PlaySfx(AudioCue.ComboIncrease, comboPitch, 0.28f);
            }
            PlayTimingAudio(rating);
            ApplySpecialBehavior(target, definition, rating);
            GameEvents.RaiseBalloonPopped(target, rating);
            target.PlayPopAnimation(rating);
        }

        public void HandleBalloonPassed(Balloon balloon)
        {
            if (balloon == null)
            {
                return;
            }

            activeBalloons.Remove(balloon);
            BalloonDefinition definition = balloon.Definition;
            if (definition != null && definition.Kind == BalloonKind.GoldenJackpot)
            {
                goldenRoundManager?.NotifyFinalBalloonPassed();
            }
            else if (gameplayActive && definition != null && !definition.IsDangerous && settings != null && settings.passedBalloonBreaksCombo)
            {
                scoreManager?.RecordMiss();
                uiManager?.ShowRating(TimingRating.Miss);
            }

            balloon.ReleaseImmediately();
        }

        public void ClearAll()
        {
            Balloon[] copy = activeBalloons.ToArray();
            activeBalloons.Clear();
            for (int i = 0; i < copy.Length; i++)
            {
                if (copy[i] != null && copy[i].IsActiveBalloon)
                {
                    copy[i].ReleaseImmediately();
                }
            }
        }

        private Balloon FindClosestBalloon(int lane, float centerY, float maxDistance)
        {
            Balloon best = null;
            float bestDistance = maxDistance;
            for (int i = 0; i < activeBalloons.Count; i++)
            {
                Balloon candidate = activeBalloons[i];
                if (candidate == null || !candidate.IsActiveBalloon || candidate.LaneIndex != lane)
                {
                    continue;
                }

                float distance = candidate.DistanceTo(centerY);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
            return best;
        }

        private void HandleMissAt(Vector3 position)
        {
            scoreManager?.RecordMiss();
            hitZone?.Flash(TimingRating.Miss);
            uiManager?.ShowRating(TimingRating.Miss);
            uiManager?.FlashScreen(new Color(1f, 0.08f, 0.12f, 0.24f), 0.12f);
            effectsManager?.PlayMiss(position);
            GameServices.Audio?.PlaySfx(AudioCue.Miss);
        }

        private void HandleDangerousBalloon(Balloon target, BalloonDefinition definition)
        {
            bool superBomb = definition.Kind == BalloonKind.SuperBomb;
            scoreManager?.RegisterBombHit();
            if (superBomb)
            {
                scoreManager?.CancelPayoutMultiplier();
            }
            GameServices.Save?.RecordBombHit();
            hitZone?.Flash(TimingRating.Miss);
            effectsManager?.PlayBomb(target.transform.position, superBomb);
            uiManager?.ShowMessage(superBomb ? "SUPER BOMB!  MULTIPLIER LOST!" : "DON'T POP BOMBS!", Color.red, 1.2f);
            uiManager?.FlashScreen(new Color(1f, 0.02f, 0.02f, 0.62f), 0.34f);
            GameServices.Audio?.PlaySfx(AudioCue.BombExplosion);
            GameEvents.RaiseBalloonPopped(target, TimingRating.Miss);
            target.PlayPopAnimation(TimingRating.Good);
        }

        private void ApplySpecialBehavior(Balloon target, BalloonDefinition definition, TimingRating rating)
        {
            switch (definition.SpecialBehavior)
            {
                case BalloonSpecialBehavior.DoublePayout:
                    scoreManager?.ActivatePayoutMultiplier(2f, settings != null ? settings.x2Duration : 3.5f);
                    uiManager?.ShowMessage("PAYOUT x2!", new Color(0.75f, 0.3f, 1f), 1.1f);
                    break;

                case BalloonSpecialBehavior.MysteryReward:
                    ResolveMystery(target.transform.position);
                    break;

                case BalloonSpecialBehavior.StartGoldenRound:
                    scoreManager?.MarkGoldenBalloon();
                    effectsManager?.PlayGoldenBalloon(target.transform.position);
                    GameServices.Audio?.PlaySfx(AudioCue.GoldenBalloonPop);
                    GameEvents.RaiseGoldenBalloonPopped();
                    if (goldenRoundManager != null && goldenRoundManager.StartGoldenRound())
                    {
                        GameServices.Audio?.PlaySfx(AudioCue.BonusStart);
                    }
                    break;

                case BalloonSpecialBehavior.ResolveJackpot:
                    goldenRoundManager?.ResolveFinalBalloon(rating);
                    if (rating == TimingRating.Perfect)
                    {
                        effectsManager?.PlayJackpot(target.transform.position);
                        GameServices.Audio?.PlaySfx(AudioCue.Jackpot);
                    }
                    break;
            }
        }

        private void ResolveMystery(Vector3 position)
        {
            int option = Random.Range(0, 7);
            int reward;
            int minimum = settings != null ? settings.mysteryMinimum : 1;
            int maximum = settings != null ? settings.mysteryMaximum : 5;
            switch (option)
            {
                case 0:
                    reward = scoreManager != null ? scoreManager.AddTickets(minimum) : minimum;
                    uiManager?.ShowMessage($"MYSTERY +{reward}!", Color.yellow);
                    break;
                case 1:
                    int midpoint = Mathf.RoundToInt((minimum + maximum) * 0.5f);
                    reward = scoreManager != null ? scoreManager.AddTickets(midpoint) : midpoint;
                    uiManager?.ShowMessage($"MYSTERY +{reward}!", Color.yellow);
                    break;
                case 2:
                    reward = Random.Range(minimum, maximum + 1);
                    reward = scoreManager != null ? scoreManager.AddTickets(reward) : reward;
                    uiManager?.ShowMessage($"MYSTERY +{reward}!", Color.yellow);
                    break;
                case 3:
                    scoreManager?.ActivatePayoutMultiplier(2f, settings != null ? settings.x2Duration : 3.5f);
                    uiManager?.ShowMessage("MYSTERY x2!", new Color(0.75f, 0.3f, 1f));
                    break;
                case 4:
                    difficultyManager?.ApplyTemporarySlowdown(0.62f, 2.25f);
                    uiManager?.ShowMessage("SLOW MOTION!", new Color(0.15f, 0.9f, 1f));
                    break;
                case 5:
                    scoreManager?.AddCombo(3);
                    uiManager?.ShowMessage("COMBO +3!", new Color(1f, 0.35f, 0.85f));
                    break;
                default:
                    float goldenChance = settings != null ? settings.mysteryGoldenChance : 0.01f;
                    if (Random.value < goldenChance && goldenRoundManager != null && !goldenRoundManager.IsActive)
                    {
                        scoreManager?.MarkGoldenBalloon();
                        if (goldenRoundManager.StartGoldenRound())
                        {
                            GameServices.Audio?.PlaySfx(AudioCue.BonusStart);
                        }
                        uiManager?.ShowMessage("GOLDEN CHANCE!", Color.yellow, 1.3f);
                    }
                    else
                    {
                        reward = scoreManager != null ? scoreManager.AddTickets(maximum) : maximum;
                        uiManager?.ShowMessage($"MYSTERY +{reward}!", Color.yellow);
                    }
                    break;
            }

            effectsManager?.PlaySuccessfulPop(position, Color.yellow, TimingRating.Great, 0);
        }

        private void HandleComboMilestone(int combo)
        {
            Vector3 position = laneManager != null
                ? laneManager.GetLanePosition(laneManager.SelectedLane, hitZone != null ? hitZone.CenterY - 1f : 2f)
                : Vector3.zero;
            effectsManager?.PlayComboMilestone(combo, position);
            uiManager?.ShowMessage($"COMBO x{combo}!", new Color(1f, 0.4f, 0.85f), 1.1f);
            GameServices.Audio?.PlaySfx(AudioCue.ComboMilestone);
        }

        private static void PlayTimingAudio(TimingRating rating)
        {
            switch (rating)
            {
                case TimingRating.Perfect:
                    GameServices.Audio?.PlaySfx(AudioCue.PerfectPop);
                    break;
                case TimingRating.Great:
                    GameServices.Audio?.PlaySfx(AudioCue.GreatPop);
                    break;
                case TimingRating.Good:
                    GameServices.Audio?.PlaySfx(AudioCue.GoodPop);
                    break;
            }
        }
    }
}
