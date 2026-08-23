using System.Collections;
using BalloonRush.Audio;
using BalloonRush.Core;
using BalloonRush.SaveSystem;
using TMPro;
using UnityEngine;

namespace BalloonRush.UI
{
    public sealed class ResultsManager : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text ticketsText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text accuracyText;
        [SerializeField] private TMP_Text goldenText;
        [SerializeField] private TMP_Text jackpotText;
        [SerializeField] private TMP_Text replayPromptText;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private TMP_Text messageText;

        private GameSessionResult result;
        private bool subscribed;
        private bool leaving;
        private bool countComplete;
        private bool payoutQueued;
        private float autoReturnRemaining;

        public void Configure(
            TMP_Text title,
            TMP_Text tickets,
            TMP_Text score,
            TMP_Text combo,
            TMP_Text accuracy,
            TMP_Text golden,
            TMP_Text jackpot,
            TMP_Text replayPrompt,
            TMP_Text countdown,
            TMP_Text message)
        {
            titleText = title;
            ticketsText = tickets;
            scoreText = score;
            comboText = combo;
            accuracyText = accuracy;
            goldenText = golden;
            jackpotText = jackpot;
            replayPromptText = replayPrompt;
            countdownText = countdown;
            messageText = message;
        }

        private IEnumerator Start()
        {
            float timeout = 3f;
            while (!GameServices.IsReady && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (!GameServices.IsReady)
            {
                yield break;
            }

            result = GameSession.LastResult ?? new GameSessionResult();
            autoReturnRemaining = GameServices.Config != null ? GameServices.Config.resultsTimeout : 12f;
            PopulateStaticFields();
            Subscribe();
            GameServices.State?.ChangeState(GameState.Results);
            GameServices.Audio?.PlayMusic(MusicCue.Results);
            StartCoroutine(AnimateTicketCount());
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (leaving || !countComplete)
            {
                return;
            }

            autoReturnRemaining = Mathf.Max(0f, autoReturnRemaining - Time.unscaledDeltaTime);
            if (countdownText != null)
            {
                countdownText.text = $"RETURNING IN {Mathf.CeilToInt(autoReturnRemaining)}   |   ESC TO ATTRACT";
            }

            if (autoReturnRemaining <= 0f)
            {
                ReturnToAttract();
            }
        }

        private void Subscribe()
        {
            if (subscribed || GameServices.Input == null)
            {
                return;
            }

            GameServices.Input.StartPressed += HandleReplay;
            GameServices.Input.OperatorPressed += HandleOperator;
            GameServices.Input.BackPressed += ReturnToAttract;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || GameServices.Input == null)
            {
                return;
            }

            GameServices.Input.StartPressed -= HandleReplay;
            GameServices.Input.OperatorPressed -= HandleOperator;
            GameServices.Input.BackPressed -= ReturnToAttract;
            subscribed = false;
        }

        private void PopulateStaticFields()
        {
            if (titleText != null) titleText.text = result.jackpotWon ? "JACKPOT RESULTS!" : "BALLOON RUSH RESULTS";
            if (ticketsText != null) ticketsText.text = "0";
            if (scoreText != null) scoreText.text = $"FINAL SCORE  {result.score:N0}";
            if (comboText != null) comboText.text = $"HIGHEST COMBO  x{result.highestCombo}";
            if (accuracyText != null) accuracyText.text = $"PERFECT {result.perfectPops}   GREAT {result.greatPops}   GOOD {result.goodPops}   MISS {result.misses}";
            if (goldenText != null) goldenText.text = $"GOLDEN BALLOONS  {result.goldenBalloons}";
            if (jackpotText != null)
            {
                jackpotText.gameObject.SetActive(result.jackpotWon);
                jackpotText.text = result.jackpotWon ? "JACKPOT WON!" : string.Empty;
            }
            if (replayPromptText != null) replayPromptText.text = "ENTER OR P TO PLAY AGAIN";
            if (messageText != null)
            {
                messageText.gameObject.SetActive(true);
                if (result.newHighScore && result.newTicketRecord)
                {
                    messageText.text = "NEW HIGH SCORE + NEW TICKET RECORD!";
                    messageText.color = new Color(1f, 0.82f, 0.08f);
                }
                else if (result.newHighScore)
                {
                    messageText.text = "NEW HIGH SCORE!";
                    messageText.color = new Color(1f, 0.82f, 0.08f);
                }
                else if (result.newTicketRecord)
                {
                    messageText.text = "NEW TICKET RECORD!";
                    messageText.color = new Color(0.25f, 1f, 0.45f);
                }
                else if (result.previousTopScore > result.score)
                {
                    int scoreGap = result.previousTopScore - result.score;
                    messageText.text = $"ONLY {scoreGap:N0} POINTS FROM THE TOP SCORE";
                    messageText.color = new Color(0.25f, 0.85f, 1f);
                }
                else
                {
                    messageText.text = "PLAY AGAIN AND BUILD A BIGGER COMBO!";
                    messageText.color = new Color(0.25f, 0.85f, 1f);
                }
            }
        }

        private IEnumerator AnimateTicketCount()
        {
            int maximum = GameServices.Settings != null && GameServices.Settings.Current != null
                ? GameServices.Settings.Current.maxTicketPayout
                : 625;
            int target = Mathf.Clamp(result.tickets, 0, Mathf.Clamp(maximum, 1, 1000));
            if (!payoutQueued)
            {
                // Queue the physical payout immediately. The persistent TicketManager
                // keeps dispensing even if the player returns to Attract Mode or begins
                // another game before the visual count-up has finished.
                GameServices.Tickets?.DispenseTickets(target, result.sessionId);
                payoutQueued = true;
            }

            float duration = Mathf.Clamp(1.2f + target / 400f, 1.2f, 3.5f);
            float elapsed = 0f;
            int previous = -1;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                int displayed = Mathf.RoundToInt(Mathf.Lerp(0f, target, 1f - Mathf.Pow(1f - t, 3f)));
                if (displayed != previous)
                {
                    previous = displayed;
                    if (ticketsText != null)
                    {
                        ticketsText.text = $"{displayed}\nTICKETS";
                    }
                    if (displayed % 5 == 0)
                    {
                        GameServices.Audio?.PlaySfx(AudioCue.TicketCount, 0.92f + Mathf.Clamp01(t) * 0.35f, 0.45f);
                    }
                }
                yield return null;
            }

            if (ticketsText != null)
            {
                ticketsText.text = $"{target}\nTICKETS";
            }

            countComplete = true;
        }

        private void HandleReplay()
        {
            if (leaving)
            {
                return;
            }

            if (!countComplete)
            {
                ShowMessage("COUNTING TICKETS...");
                return;
            }

            if (GameServices.Credits != null && GameServices.Credits.TryConsumePlay())
            {
                leaving = true;
                Unsubscribe();
                GameServices.Audio?.PlayUi(AudioCue.ButtonClick);
                GameServices.Bootstrap?.GoToMainGame();
            }
            else
            {
                ShowMessage(GetSwipePrompt());
            }
        }


        private static string GetSwipePrompt()
        {
            OperatorSettings settings = GameServices.Settings != null ? GameServices.Settings.Current : null;
            int cents = settings != null ? Mathf.Max(0, settings.pricePerPlayCents) : 100;
            return $"SWIPE CARD - ${cents / 100f:0.00}";
        }

        private void HandleOperator()
        {
            if (leaving)
            {
                return;
            }

            leaving = true;
            Unsubscribe();
            GameServices.Bootstrap?.GoToOperatorMenu();
        }


        private void ReturnToAttract()
        {
            if (leaving)
            {
                return;
            }

            leaving = true;
            Unsubscribe();
            GameServices.Bootstrap?.GoToAttractMode();
        }

        private void ShowMessage(string message)
        {
            if (messageText == null)
            {
                return;
            }

            messageText.gameObject.SetActive(true);
            messageText.text = message;
            messageText.color = new Color(1f, 0.22f, 0.22f);
            CancelInvoke(nameof(HideMessage));
            Invoke(nameof(HideMessage), 1.8f);
        }

        private void HideMessage()
        {
            if (messageText != null)
            {
                messageText.gameObject.SetActive(false);
            }
        }
    }
}
