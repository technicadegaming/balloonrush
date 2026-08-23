using System.Collections;
using BalloonRush.Core;
using BalloonRush.Gameplay;
using BalloonRush.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    public sealed class UIManager : MonoBehaviour
    {
        [Header("Primary HUD")]
        [SerializeField] private TMP_Text ticketsText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text multiplierText;
        [SerializeField] private TMP_Text jackpotText;
        [SerializeField] private Image comboFill;
        [SerializeField] private Image[] laneIndicators;

        [Header("Feedback")]
        [SerializeField] private TMP_Text ratingText;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Image flashOverlay;

        [Header("Golden round")]
        [SerializeField] private GameObject goldenRoundBanner;
        [SerializeField] private TMP_Text goldenRoundTimerText;

        [Header("Debug")]
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private TMP_Text debugText;

        private ScoreManager scoreManager;
        private ComboManager comboManager;
        private LaneManager laneManager;
        private RoundManager roundManager;
        private GoldenRoundManager goldenRoundManager;
        private OperatorSettings settings;
        private Coroutine ratingRoutine;
        private Coroutine messageRoutine;
        private Coroutine flashRoutine;
        private Coroutine countdownRoutine;

        public void Configure(
            TMP_Text tickets,
            TMP_Text score,
            TMP_Text timer,
            TMP_Text combo,
            TMP_Text multiplier,
            TMP_Text jackpot,
            Image comboMeterFill,
            Image[] laneButtonIndicators,
            TMP_Text rating,
            TMP_Text countdown,
            TMP_Text message,
            Image flash,
            GameObject goldenBanner,
            TMP_Text goldenTimer,
            GameObject configuredDebugPanel,
            TMP_Text configuredDebugText)
        {
            ticketsText = tickets;
            scoreText = score;
            timerText = timer;
            comboText = combo;
            multiplierText = multiplier;
            jackpotText = jackpot;
            comboFill = comboMeterFill;
            laneIndicators = laneButtonIndicators;
            ratingText = rating;
            countdownText = countdown;
            messageText = message;
            flashOverlay = flash;
            goldenRoundBanner = goldenBanner;
            goldenRoundTimerText = goldenTimer;
            debugPanel = configuredDebugPanel;
            debugText = configuredDebugText;
        }

        public void Bind(
            ScoreManager score,
            ComboManager combo,
            LaneManager lanes,
            RoundManager round,
            GoldenRoundManager golden,
            OperatorSettings operatorSettings)
        {
            Unbind();
            scoreManager = score;
            comboManager = combo;
            laneManager = lanes;
            roundManager = round;
            goldenRoundManager = golden;
            settings = operatorSettings;

            if (scoreManager != null)
            {
                scoreManager.ScoreChanged += UpdateScore;
                scoreManager.TicketsChanged += UpdateTickets;
                scoreManager.PayoutMultiplierChanged += UpdateMultiplier;
                UpdateScore(scoreManager.Score);
                UpdateTickets(scoreManager.Tickets);
                UpdateMultiplier(scoreManager.ActivePayoutMultiplier, scoreManager.PayoutMultiplierRemaining);
            }

            if (comboManager != null)
            {
                comboManager.ComboChanged += UpdateCombo;
                UpdateCombo(comboManager.CurrentCombo);
            }

            if (laneManager != null)
            {
                laneManager.SelectedLaneChanged += UpdateSelectedLane;
                UpdateSelectedLane(laneManager.SelectedLane);
            }

            if (roundManager != null)
            {
                roundManager.TimeChanged += UpdateTimer;
                roundManager.RushModeChanged += SetRushMode;
                float initialTime = roundManager.RemainingTime > 0f
                    ? roundManager.RemainingTime
                    : (settings != null ? settings.gameDuration : 35f);
                UpdateTimer(initialTime);
            }

            if (goldenRoundManager != null)
            {
                goldenRoundManager.RoundStarted += ShowGoldenRound;
                goldenRoundManager.TimeChanged += UpdateGoldenTimer;
                goldenRoundManager.RoundResolved += HandleGoldenResolved;
                goldenRoundManager.RoundEnded += HideGoldenRound;
            }

            SetJackpot(settings != null ? settings.jackpotTickets : 500);
            HideGoldenRound();
            SetDebugVisible(false);
            ClearTransientText();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void SetJackpot(int jackpot)
        {
            if (jackpotText != null)
            {
                jackpotText.text = $"JACKPOT\n{Mathf.Clamp(jackpot, 1, 500)} TICKETS";
            }
        }

        public void ShowCountdown(string value, float displaySeconds = 0.65f)
        {
            if (countdownRoutine != null)
            {
                StopCoroutine(countdownRoutine);
            }
            countdownRoutine = StartCoroutine(CountdownRoutine(value, displaySeconds));
        }

        public void ShowRating(TimingRating rating)
        {
            if (ratingRoutine != null)
            {
                StopCoroutine(ratingRoutine);
            }

            string text;
            Color color;
            switch (rating)
            {
                case TimingRating.Perfect:
                    text = "PERFECT POP!";
                    color = new Color(1f, 0.85f, 0.08f);
                    break;
                case TimingRating.Great:
                    text = "GREAT!";
                    color = new Color(0.25f, 1f, 0.4f);
                    break;
                case TimingRating.Good:
                    text = "GOOD!";
                    color = new Color(0.2f, 0.75f, 1f);
                    break;
                default:
                    text = "MISS!";
                    color = new Color(1f, 0.18f, 0.22f);
                    break;
            }

            ratingRoutine = StartCoroutine(TransientTextRoutine(ratingText, text, color, 0.65f));
        }

        public void ShowMessage(string message, Color color, float duration = 1.1f)
        {
            if (messageRoutine != null)
            {
                StopCoroutine(messageRoutine);
            }
            messageRoutine = StartCoroutine(TransientTextRoutine(messageText, message, color, duration));
        }

        public void FlashScreen(Color color, float duration = 0.18f)
        {
            if (flashOverlay == null)
            {
                return;
            }

            if (settings != null && settings.reducedFlashes)
            {
                color.a *= 0.2f;
                duration *= 0.5f;
            }

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }
            flashRoutine = StartCoroutine(FlashRoutine(color, duration));
        }

        public void SetDebugVisible(bool visible)
        {
            if (debugPanel != null)
            {
                debugPanel.SetActive(visible);
            }
        }

        public bool IsDebugVisible => debugPanel != null && debugPanel.activeSelf;

        public void SetDebugText(string value)
        {
            if (debugText != null)
            {
                debugText.text = value;
            }
        }

        private void Unbind()
        {
            if (scoreManager != null)
            {
                scoreManager.ScoreChanged -= UpdateScore;
                scoreManager.TicketsChanged -= UpdateTickets;
                scoreManager.PayoutMultiplierChanged -= UpdateMultiplier;
            }

            if (comboManager != null)
            {
                comboManager.ComboChanged -= UpdateCombo;
            }

            if (laneManager != null)
            {
                laneManager.SelectedLaneChanged -= UpdateSelectedLane;
            }

            if (roundManager != null)
            {
                roundManager.TimeChanged -= UpdateTimer;
                roundManager.RushModeChanged -= SetRushMode;
            }

            if (goldenRoundManager != null)
            {
                goldenRoundManager.RoundStarted -= ShowGoldenRound;
                goldenRoundManager.TimeChanged -= UpdateGoldenTimer;
                goldenRoundManager.RoundResolved -= HandleGoldenResolved;
                goldenRoundManager.RoundEnded -= HideGoldenRound;
            }
        }

        private void UpdateTickets(int tickets)
        {
            if (ticketsText != null)
            {
                ticketsText.text = $"TICKETS\n{tickets}";
            }
        }

        private void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE  {score:N0}";
            }
        }

        private void UpdateTimer(float time)
        {
            if (timerText != null)
            {
                timerText.text = time > 9.95f ? Mathf.CeilToInt(time).ToString("00") : time.ToString("0.0");
            }
        }

        private void UpdateCombo(int combo)
        {
            if (comboText != null)
            {
                comboText.text = $"COMBO\nx{Mathf.Max(0, combo)}";
            }

            if (comboFill != null)
            {
                comboFill.fillAmount = Mathf.Clamp01(combo / 30f);
                comboFill.color = Color.Lerp(new Color(0.1f, 0.55f, 1f), new Color(1f, 0.72f, 0.05f), comboFill.fillAmount);
            }
        }

        private void UpdateMultiplier(float multiplier, float remaining)
        {
            if (multiplierText == null)
            {
                return;
            }

            multiplierText.gameObject.SetActive(multiplier > 1.01f);
            multiplierText.text = multiplier > 1.01f ? $"PAYOUT x{multiplier:0.#}\n{remaining:0.0}s" : string.Empty;
        }

        private void UpdateSelectedLane(int selectedLane)
        {
            if (laneIndicators == null)
            {
                return;
            }

            for (int i = 0; i < laneIndicators.Length; i++)
            {
                if (laneIndicators[i] == null)
                {
                    continue;
                }

                laneIndicators[i].color = i == selectedLane
                    ? new Color(1f, 0.85f, 0.1f, 1f)
                    : new Color(0.08f, 0.55f, 1f, 0.72f);
                laneIndicators[i].transform.localScale = i == selectedLane ? Vector3.one * 1.08f : Vector3.one;
            }
        }

        private void SetRushMode(bool enabled)
        {
            if (enabled)
            {
                ShowMessage("BALLOON RUSH!", new Color(1f, 0.3f, 0.08f), 1.4f);
                FlashScreen(new Color(1f, 0.15f, 0.05f, 0.42f), 0.25f);
            }
        }

        private void ShowGoldenRound()
        {
            if (goldenRoundBanner != null)
            {
                goldenRoundBanner.SetActive(true);
            }
            ShowMessage("GOLDEN BALLOON ROUND!", Color.yellow, 1.5f);
            FlashScreen(new Color(1f, 0.72f, 0.05f, 0.55f), 0.30f);
        }

        private void HideGoldenRound()
        {
            if (goldenRoundBanner != null)
            {
                goldenRoundBanner.SetActive(false);
            }
        }

        private void UpdateGoldenTimer(float time)
        {
            if (goldenRoundTimerText != null)
            {
                goldenRoundTimerText.text = time > 0.05f
                    ? $"GOLDEN ROUND  {time:0.0}"
                    : "FINAL GOLDEN BALLOON!";
            }
        }

        private void HandleGoldenResolved(int reward, bool jackpot)
        {
            if (jackpot)
            {
                ShowMessage($"JACKPOT!  {reward} TICKETS!", Color.yellow, 3.2f);
                FlashScreen(new Color(1f, 0.78f, 0.08f, 0.75f), 0.65f);
            }
            else
            {
                ShowMessage($"GOLDEN BONUS +{reward}", new Color(1f, 0.7f, 0.1f), 1.7f);
            }
        }

        private IEnumerator CountdownRoutine(string value, float duration)
        {
            if (countdownText == null)
            {
                yield break;
            }

            countdownText.gameObject.SetActive(true);
            countdownText.text = value;
            countdownText.color = Color.white;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float scale = Mathf.Lerp(1.6f, 0.75f, t);
                countdownText.transform.localScale = Vector3.one * scale;
                countdownText.alpha = 1f - Mathf.Max(0f, (t - 0.65f) / 0.35f);
                yield return null;
            }

            countdownText.gameObject.SetActive(false);
            countdownText.alpha = 1f;
            countdownText.transform.localScale = Vector3.one;
            countdownRoutine = null;
        }

        private IEnumerator TransientTextRoutine(TMP_Text target, string value, Color color, float duration)
        {
            if (target == null)
            {
                if (target == ratingText) ratingRoutine = null;
                if (target == messageText) messageRoutine = null;
                yield break;
            }

            target.gameObject.SetActive(true);
            target.text = value;
            target.color = color;
            target.alpha = 1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = 1f + Mathf.Sin(t * Mathf.PI) * 0.28f;
                target.transform.localScale = Vector3.one * pulse;
                target.alpha = 1f - Mathf.Max(0f, (t - 0.68f) / 0.32f);
                yield return null;
            }

            target.gameObject.SetActive(false);
            target.alpha = 1f;
            target.transform.localScale = Vector3.one;
            if (target == ratingText) ratingRoutine = null;
            if (target == messageText) messageRoutine = null;
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            flashOverlay.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Color current = color;
                current.a = color.a * (1f - t);
                flashOverlay.color = current;
                yield return null;
            }

            flashOverlay.color = Color.clear;
            flashOverlay.gameObject.SetActive(false);
            flashRoutine = null;
        }

        private void ClearTransientText()
        {
            if (ratingText != null) ratingText.gameObject.SetActive(false);
            if (countdownText != null) countdownText.gameObject.SetActive(false);
            if (messageText != null) messageText.gameObject.SetActive(false);
            if (flashOverlay != null) flashOverlay.gameObject.SetActive(false);
        }
    }
}
