using System.Collections;
using BalloonRush.Audio;
using BalloonRush.Core;
using BalloonRush.Gameplay;
using BalloonRush.SaveSystem;
using TMPro;
using UnityEngine;

namespace BalloonRush.UI
{
    public sealed class AttractModeManager : MonoBehaviour
    {
        [SerializeField] private TMP_Text logoText;
        [SerializeField] private TMP_Text taglineText;
        [SerializeField] private TMP_Text creditsText;
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private TMP_Text jackpotText;
        [SerializeField] private TMP_Text startPromptText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private RectTransform[] demoBalloons;

        private static readonly string[] Taglines =
        {
            "SELECT A LANE - POP IN THE HIT ZONE",
            "BUILD THE COMBO - WIN MORE TICKETS",
            "AVOID BOMBS - POP THE GOLD BALLOON",
            "EASY TO PLAY - HARD TO MASTER"
        };

        private Vector2[] initialBalloonPositions;
        private float[] balloonSpeeds;
        private Coroutine messageRoutine;
        private bool subscribed;
        private int taglineIndex;
        private float nextTaglineChange;

        public void Configure(
            TMP_Text logo,
            TMP_Text tagline,
            TMP_Text credits,
            TMP_Text highScore,
            TMP_Text jackpot,
            TMP_Text startPrompt,
            TMP_Text message,
            RectTransform[] balloons)
        {
            logoText = logo;
            taglineText = tagline;
            creditsText = credits;
            highScoreText = highScore;
            jackpotText = jackpot;
            startPromptText = startPrompt;
            messageText = message;
            demoBalloons = balloons;
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

            SetupAnimationData();
            Subscribe();
            RefreshAll();
            GameServices.State?.ChangeState(GameState.Attract);
            GameServices.Audio?.PlayMusic(MusicCue.Attract);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (logoText != null)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.2f) * 0.035f;
                logoText.transform.localScale = Vector3.one * pulse;
            }

            if (startPromptText != null)
            {
                Color color = startPromptText.color;
                color.a = 0.62f + Mathf.Sin(Time.unscaledTime * 4.5f) * 0.30f;
                startPromptText.color = color;
            }

            if (taglineText != null && Time.unscaledTime >= nextTaglineChange)
            {
                taglineIndex = (taglineIndex + 1) % Taglines.Length;
                taglineText.text = Taglines[taglineIndex];
                nextTaglineChange = Time.unscaledTime + 3.2f;
            }

            AnimateDemoBalloons();
        }

        private void Subscribe()
        {
            if (subscribed || GameServices.Input == null)
            {
                return;
            }

            GameServices.Input.StartPressed += HandleStart;
            GameServices.Input.OperatorPressed += HandleOperator;
            if (GameServices.Credits != null)
            {
                GameServices.Credits.CreditsChanged += UpdateCredits;
            }
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (GameServices.Input != null)
            {
                GameServices.Input.StartPressed -= HandleStart;
                GameServices.Input.OperatorPressed -= HandleOperator;
            }

            if (GameServices.Credits != null)
            {
                GameServices.Credits.CreditsChanged -= UpdateCredits;
            }
            subscribed = false;
        }

        private void HandleStart()
        {
            if (GameServices.Credits != null && GameServices.Credits.TryConsumePlay())
            {
                GameServices.Audio?.PlayUi(AudioCue.ButtonClick);
                Unsubscribe();
                GameServices.Bootstrap?.GoToMainGame();
            }
            else
            {
                ShowMessage(GetSwipePrompt(), new Color(1f, 0.32f, 0.22f));
                GameServices.Audio?.PlaySfx(AudioCue.Miss);
            }
        }

        private void HandleOperator()
        {
            Unsubscribe();
            GameServices.Bootstrap?.GoToOperatorMenu();
        }

        private void RefreshAll()
        {
            taglineIndex = 0;
            nextTaglineChange = Time.unscaledTime + 3.2f;
            if (logoText != null)
            {
                logoText.text = "<color=#FFFFFF>BALLOON</color>\n<color=#FF4B32>RUSH</color>";
            }
            if (taglineText != null) taglineText.text = Taglines[taglineIndex];
            UpdateCredits(GameServices.Credits != null ? GameServices.Credits.Credits : 0);

            if (highScoreText != null)
            {
                int highScore = GameServices.Save != null && GameServices.Save.Data != null
                    ? GameServices.Save.Data.highScores.topScore
                    : 0;
                highScoreText.text = $"HIGH SCORE\n{highScore:N0}";
            }

            if (jackpotText != null)
            {
                int jackpot = GameServices.Settings != null && GameServices.Settings.Current != null
                    ? GameServices.Settings.Current.jackpotTickets
                    : 500;
                jackpotText.text = $"JACKPOT\n{jackpot} TICKETS";
            }

            if (messageText != null)
            {
                messageText.gameObject.SetActive(false);
            }
        }

        private void UpdateCredits(int credits)
        {
            bool freePlay = GameServices.Settings != null &&
                            GameServices.Settings.Current != null &&
                            GameServices.Settings.Current.freePlay;
            int creditsPerPlay = GameServices.Settings != null && GameServices.Settings.Current != null
                ? Mathf.Max(1, GameServices.Settings.Current.creditsPerPlay)
                : 1;

            if (creditsText != null)
            {
                creditsText.text = freePlay ? "FREE PLAY" : $"CREDITS\n{credits}";
            }

            if (startPromptText != null)
            {
                bool ready = freePlay || credits >= creditsPerPlay;
                startPromptText.text = ready ? "ENTER OR P TO START" : GetSwipePrompt();
                startPromptText.color = ready
                    ? new Color(0.35f, 1f, 0.55f, 1f)
                    : new Color(1f, 0.78f, 0.12f, 1f);
            }
        }


        private static string GetSwipePrompt()
        {
            OperatorSettings settings = GameServices.Settings != null ? GameServices.Settings.Current : null;
            int cents = settings != null ? Mathf.Max(0, settings.pricePerPlayCents) : 100;
            string paidPrompt = $"SWIPE CARD - ${cents / 100f:0.00}";
            return Application.isEditor || Debug.isDebugBuild
                ? paidPrompt + "   |   C TEST CREDIT"
                : paidPrompt;
        }

        private void SetupAnimationData()
        {
            if (demoBalloons == null)
            {
                demoBalloons = new RectTransform[0];
            }

            initialBalloonPositions = new Vector2[demoBalloons.Length];
            balloonSpeeds = new float[demoBalloons.Length];
            for (int i = 0; i < demoBalloons.Length; i++)
            {
                if (demoBalloons[i] == null) continue;
                initialBalloonPositions[i] = demoBalloons[i].anchoredPosition;
                balloonSpeeds[i] = 90f + (i % 4) * 22f;

                UnityEngine.UI.Image image = demoBalloons[i].GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    image.sprite = RuntimeSpriteLibrary.BalloonSprite;
                    image.type = UnityEngine.UI.Image.Type.Simple;
                    image.preserveAspect = true;
                }
            }
        }

        private void AnimateDemoBalloons()
        {
            if (demoBalloons == null || initialBalloonPositions == null)
            {
                return;
            }

            for (int i = 0; i < demoBalloons.Length; i++)
            {
                RectTransform balloon = demoBalloons[i];
                if (balloon == null) continue;

                RectTransform parent = balloon.parent as RectTransform;
                float parentHeight = parent != null ? Mathf.Max(500f, parent.rect.height) : 900f;
                float upperLimit = parentHeight * 0.56f + balloon.rect.height * 0.5f;
                float lowerLimit = -parentHeight * 0.56f - balloon.rect.height * 0.5f;

                Vector2 position = balloon.anchoredPosition;
                position.y += balloonSpeeds[i] * Time.unscaledDeltaTime;
                position.x = initialBalloonPositions[i].x +
                             Mathf.Sin(Time.unscaledTime * (1.1f + i * 0.08f) + i) * 24f;
                if (position.y > upperLimit)
                {
                    position.y = lowerLimit - (i % 4) * 92f;
                }
                balloon.anchoredPosition = position;
            }
        }

        private void ShowMessage(string message, Color color)
        {
            if (messageRoutine != null)
            {
                StopCoroutine(messageRoutine);
            }
            messageRoutine = StartCoroutine(MessageRoutine(message, color));
        }

        private IEnumerator MessageRoutine(string message, Color color)
        {
            if (messageText == null)
            {
                yield break;
            }

            messageText.gameObject.SetActive(true);
            messageText.text = message;
            messageText.color = color;
            yield return new WaitForSecondsRealtime(1.8f);
            messageText.gameObject.SetActive(false);
            messageRoutine = null;
        }
    }
}
