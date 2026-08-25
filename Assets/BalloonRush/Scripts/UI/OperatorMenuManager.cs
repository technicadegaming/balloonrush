using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using BalloonRush.Audio;
using BalloonRush.Core;
using BalloonRush.Gameplay;
using BalloonRush.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    public sealed class OperatorMenuManager : MonoBehaviour
    {
        [SerializeField] private RectTransform settingsContent;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text statisticsText;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button resetDefaultsButton;
        [SerializeField] private Button testInputsButton;
        [SerializeField] private Button testTicketsButton;
        [SerializeField] private Button resetStatisticsButton;
        [SerializeField] private Button backButton;

        private readonly List<Action> rowRefreshers = new List<Action>();
        private OperatorSettings editable;
        private bool built;
        private bool subscribed;
        private bool inputTestMode;
        private float resetStatisticsArmedUntil;
        private float discardTicketFaultArmedUntil;
        private TMP_FontAsset operatorFont;

        public void Configure(
            RectTransform content,
            TMP_Text status,
            TMP_Text statistics,
            Button save,
            Button resetDefaults,
            Button testInputs,
            Button testTickets,
            Button resetStatistics,
            Button back)
        {
            settingsContent = content;
            statusText = status;
            statisticsText = statistics;
            saveButton = save;
            resetDefaultsButton = resetDefaults;
            testInputsButton = testInputs;
            testTicketsButton = testTickets;
            resetStatisticsButton = resetStatistics;
            backButton = back;
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

            editable = GameServices.Settings.CreateEditableCopy();
            operatorFont = statusText != null && statusText.font != null ? statusText.font : TMP_Settings.defaultFontAsset;

            // Cabinet fail-safe: make M / JoystickButton4 and BACK available before
            // any dynamic settings control is created. A broken field must never trap
            // an operator inside the service menu.
            BindButtons();
            SubscribeInput();
            GameServices.State?.ChangeState(GameState.OperatorMenu);

            try
            {
                BuildSettingsUI();
            }
            catch (Exception exception)
            {
                Debug.LogError("Operator Menu UI build failed, but service exit remains active: " + exception);
                SetStatus("MENU BUILD ERROR - use M / key switch to exit.", Color.red);
            }

            RefreshAllRows();
            RefreshStatistics();
            SetStatus("Operator settings loaded.", new Color(0.35f, 1f, 0.55f));
            GameServices.Audio?.PlayMusic(MusicCue.Attract, 0.3f);
        }

        private void OnDestroy()
        {
            UnbindButtons();
            UnsubscribeInput();
        }

        private void BindButtons()
        {
            if (saveButton != null) saveButton.onClick.AddListener(SaveSettings);
            if (resetDefaultsButton != null) resetDefaultsButton.onClick.AddListener(ResetDefaults);
            if (testInputsButton != null) testInputsButton.onClick.AddListener(ToggleInputTest);
            if (testTicketsButton != null) testTicketsButton.onClick.AddListener(TestTickets);
            if (resetStatisticsButton != null) resetStatisticsButton.onClick.AddListener(ResetStatistics);
            if (backButton != null) backButton.onClick.AddListener(BackToAttract);
        }

        private void UnbindButtons()
        {
            if (saveButton != null) saveButton.onClick.RemoveListener(SaveSettings);
            if (resetDefaultsButton != null) resetDefaultsButton.onClick.RemoveListener(ResetDefaults);
            if (testInputsButton != null) testInputsButton.onClick.RemoveListener(ToggleInputTest);
            if (testTicketsButton != null) testTicketsButton.onClick.RemoveListener(TestTickets);
            if (resetStatisticsButton != null) resetStatisticsButton.onClick.RemoveListener(ResetStatistics);
            if (backButton != null) backButton.onClick.RemoveListener(BackToAttract);
        }

        private void SubscribeInput()
        {
            if (subscribed || GameServices.Input == null)
            {
                return;
            }

            GameServices.Input.LeftPressed += HandleLeftTest;
            GameServices.Input.RightPressed += HandleRightTest;
            GameServices.Input.PopPressed += HandlePopTest;
            GameServices.Input.StartPressed += HandleStartTest;
            GameServices.Input.CreditPulse += HandleCreditTest;
            GameServices.Input.OperatorPressed += HandleOperatorButton;
            GameServices.Input.BackPressed += HandleBackButton;
            subscribed = true;
        }

        private void UnsubscribeInput()
        {
            if (!subscribed || GameServices.Input == null)
            {
                return;
            }

            GameServices.Input.LeftPressed -= HandleLeftTest;
            GameServices.Input.RightPressed -= HandleRightTest;
            GameServices.Input.PopPressed -= HandlePopTest;
            GameServices.Input.StartPressed -= HandleStartTest;
            GameServices.Input.CreditPulse -= HandleCreditTest;
            GameServices.Input.OperatorPressed -= HandleOperatorButton;
            GameServices.Input.BackPressed -= HandleBackButton;
            subscribed = false;
        }

        private void BuildSettingsUI()
        {
            if (built || settingsContent == null)
            {
                return;
            }

            built = true;
            AddHeader("GAME AND CREDIT SETTINGS");
            AddFloatField("Game duration (20-120 sec)", () => editable.gameDuration, value => editable.gameDuration = value);
            AddIntField("Credits per play", () => editable.creditsPerPlay, value => editable.creditsPerPlay = value);
            AddToggleField("Free play", () => editable.freePlay, value => editable.freePlay = value);
            AddIntField("Coin pulse value", () => editable.coinValue, value => editable.coinValue = value);
            AddIntField("Card swipe value (default 1 credit)", () => editable.cardSwipeValue, value => editable.cardSwipeValue = value);
            AddIntField("Price per play in cents ($1 = 100)", () => editable.pricePerPlayCents, value => editable.pricePerPlayCents = value);

            AddHeader("PAYOUT ECONOMICS");
            AddFloatField("Estimated prize cost per ticket (cents)", () => editable.estimatedPrizeCostPerTicketCents, value => editable.estimatedPrizeCostPerTicketCents = value);
            AddFloatField("Target prize-cost percent", () => editable.targetPrizeCostPercent, value => editable.targetPrizeCostPercent = value);
            AddIntField("Minimum ticket payout", () => editable.minimumTicketPayout, value => editable.minimumTicketPayout = value);
            AddIntField("Regular ticket cap", () => editable.regularTicketCap, value => editable.regularTicketCap = value);

            AddHeader("PAYOUT AND REDEMPTION");
            AddIntField("Jackpot tickets (max 500)", () => editable.jackpotTickets, value => editable.jackpotTickets = value);
            AddIntField("Maximum tickets per game (max 1000)", () => editable.maxTicketPayout, value => editable.maxTicketPayout = value);
            AddIntField("Green balloon tickets", () => editable.greenTickets, value => editable.greenTickets = value);
            AddIntField("Blue balloon tickets", () => editable.blueTickets, value => editable.blueTickets = value);
            AddIntField("Golden trigger tickets", () => editable.goldenTriggerTickets, value => editable.goldenTriggerTickets = value);
            AddIntField("Mystery minimum", () => editable.mysteryMinimum, value => editable.mysteryMinimum = value);
            AddIntField("Mystery maximum", () => editable.mysteryMaximum, value => editable.mysteryMaximum = value);
            AddFloatField("Mystery-to-Golden chance (0-0.25)", () => editable.mysteryGoldenChance, value => editable.mysteryGoldenChance = value);
            AddIntField("Golden GREAT reward", () => editable.goldenGreatReward, value => editable.goldenGreatReward = value);
            AddIntField("Golden GOOD reward", () => editable.goldenGoodReward, value => editable.goldenGoodReward = value);
            AddIntField("Golden MISS reward", () => editable.goldenMissReward, value => editable.goldenMissReward = value);
            AddIntField("Bomb ticket penalty", () => editable.bombTicketPenalty, value => editable.bombTicketPenalty = value);
            AddFloatField("GOOD ticket multiplier", () => editable.goodTicketMultiplier, value => editable.goodTicketMultiplier = value);
            AddFloatField("GREAT ticket multiplier", () => editable.greatTicketMultiplier, value => editable.greatTicketMultiplier = value);
            AddFloatField("PERFECT ticket multiplier", () => editable.perfectTicketMultiplier, value => editable.perfectTicketMultiplier = value);

            AddHeader("GAMEPLAY AND DIFFICULTY");
            AddFloatField("Base balloon speed", () => editable.balloonBaseSpeed, value => editable.balloonBaseSpeed = value);
            AddFloatField("Base spawn interval", () => editable.spawnInterval, value => editable.spawnInterval = value);
            AddFloatField("Green balloon spawn weight", () => editable.greenSpawnWeight, value => editable.greenSpawnWeight = value);
            AddFloatField("Blue balloon spawn weight", () => editable.blueSpawnWeight, value => editable.blueSpawnWeight = value);
            AddFloatField("Bomb spawn weight", () => editable.bombSpawnWeight, value => editable.bombSpawnWeight = value);
            AddFloatField("Super bomb spawn weight", () => editable.superBombSpawnWeight, value => editable.superBombSpawnWeight = value);
            AddFloatField("Golden balloon spawn weight", () => editable.goldenSpawnWeight, value => editable.goldenSpawnWeight = value);
            AddFloatField("Mystery spawn weight", () => editable.mysterySpawnWeight, value => editable.mysterySpawnWeight = value);
            AddFloatField("x2 spawn weight", () => editable.multiplierSpawnWeight, value => editable.multiplierSpawnWeight = value);
            AddFloatField("Combo timeout", () => editable.comboTimeout, value => editable.comboTimeout = value);
            AddFloatField("Combo x5 ticket multiplier", () => editable.combo5Multiplier, value => editable.combo5Multiplier = value);
            AddFloatField("Combo x10 ticket multiplier", () => editable.combo10Multiplier, value => editable.combo10Multiplier = value);
            AddFloatField("Combo x15 ticket multiplier", () => editable.combo15Multiplier, value => editable.combo15Multiplier = value);
            AddFloatField("Combo x20 ticket multiplier", () => editable.combo20Multiplier, value => editable.combo20Multiplier = value);
            AddFloatField("Combo x30 ticket multiplier", () => editable.combo30Multiplier, value => editable.combo30Multiplier = value);
            AddFloatField("Perfect timing window", () => editable.perfectWindow, value => editable.perfectWindow = value);
            AddFloatField("Great timing window", () => editable.greatWindow, value => editable.greatWindow = value);
            AddFloatField("Good timing window", () => editable.goodWindow, value => editable.goodWindow = value);
            AddFloatField("x2 duration", () => editable.x2Duration, value => editable.x2Duration = value);
            AddFloatField("Golden round duration", () => editable.goldenRoundDuration, value => editable.goldenRoundDuration = value);
            AddToggleField("Passed reward balloons break combo", () => editable.passedBalloonBreaksCombo, value => editable.passedBalloonBreaksCombo = value);

            AddHeader("AUDIO AND ACCESSIBILITY");
            AddFloatField("Master volume (0-1)", () => editable.masterVolume, value => editable.masterVolume = value);
            AddFloatField("Music volume (0-1)", () => editable.musicVolume, value => editable.musicVolume = value);
            AddFloatField("SFX volume (0-1)", () => editable.sfxVolume, value => editable.sfxVolume = value);
            AddToggleField("Different gameplay song each round", () => editable.gameplayMusicRotationEnabled, value => editable.gameplayMusicRotationEnabled = value);
            AddFloatField("Gameplay music START pitch", () => editable.gameplayMusicStartPitch, value => editable.gameplayMusicStartPitch = value);
            AddFloatField("Gameplay music END pitch", () => editable.gameplayMusicEndPitch, value => editable.gameplayMusicEndPitch = value);
            AddToggleField("Cabinet edge lights", () => editable.cabinetEdgeLightsEnabled, value => editable.cabinetEdgeLightsEnabled = value);
            AddFloatField("Attract edge flicker intensity (0-1)", () => editable.attractEdgeFlickerIntensity, value => editable.attractEdgeFlickerIntensity = value);
            AddFloatField("Gameplay edge pulse MIN Hz", () => editable.gameplayEdgePulseMinHz, value => editable.gameplayEdgePulseMinHz = value);
            AddFloatField("Gameplay edge pulse MAX Hz", () => editable.gameplayEdgePulseMaxHz, value => editable.gameplayEdgePulseMaxHz = value);
            AddToggleField("Reduced screen shake", () => editable.reducedScreenShake, value => editable.reducedScreenShake = value);
            AddToggleField("Reduced flashes", () => editable.reducedFlashes, value => editable.reducedFlashes = value);

            AddHeader("ARCADE HARDWARE");
            AddToggleField("Serial hardware enabled", () => editable.hardwareEnabled, value => editable.hardwareEnabled = value);
            AddTextField("Serial port", () => editable.serialPort, value => editable.serialPort = value);
            AddIntField("Baud rate", () => editable.baudRate, value => editable.baudRate = value);
            AddIntField("Button debounce (milliseconds)", () => editable.inputDebounceMilliseconds, value => editable.inputDebounceMilliseconds = value);
            AddIntField("Coin debounce (milliseconds)", () => editable.coinDebounceMilliseconds, value => editable.coinDebounceMilliseconds = value);
            AddIntField("Card swipe debounce (milliseconds)", () => editable.cardSwipeDebounceMilliseconds, value => editable.cardSwipeDebounceMilliseconds = value);
            AddFloatField("Wait for ticket hardware (seconds)", () => editable.ticketHardwareWaitTimeoutSeconds, value => editable.ticketHardwareWaitTimeoutSeconds = value);
            AddFloatField("Wait for PAID acknowledgement (seconds)", () => editable.ticketPaidAckTimeoutSeconds, value => editable.ticketPaidAckTimeoutSeconds = value);
        }

        private void SaveSettings()
        {
            editable.Validate();
            GameServices.Settings.Apply(editable);
            editable = GameServices.Settings.CreateEditableCopy();
            RefreshAllRows();
            int targetAverage = EconomyMath.CalculateTargetAverageTickets(editable);
            float priceDollars = editable.pricePerPlayCents / 100f;
            SetStatus($"Settings saved. ${priceDollars:0.00} play target: about {targetAverage} average tickets at the entered prize cost.", new Color(0.35f, 1f, 0.55f));
            GameServices.Audio?.PlayUi(AudioCue.ButtonClick);
        }

        private void ResetDefaults()
        {
            GameServices.Settings.ResetDefaults();
            editable = GameServices.Settings.CreateEditableCopy();
            RefreshAllRows();
            SetStatus("Default settings restored and saved.", Color.yellow);
            GameServices.Audio?.PlayUi(AudioCue.ButtonClick);
        }

        private void ToggleInputTest()
        {
            inputTestMode = !inputTestMode;
            SetStatus(inputTestMode ? "Input test active: press cabinet controls." : "Input test stopped.", inputTestMode ? Color.cyan : Color.white);
        }

        private void TestTickets()
        {
            if (GameServices.Tickets != null && GameServices.Tickets.HasPayoutFault)
            {
                if (GameServices.Tickets.CanRetryFailedPayout)
                {
                    GameServices.Tickets.RetryFailedPayout();
                    SetStatus("Retrying the unsent payout now that hardware can be checked.", Color.yellow);
                    return;
                }

                if (Time.unscaledTime > discardTicketFaultArmedUntil)
                {
                    discardTicketFaultArmedUntil = Time.unscaledTime + 5f;
                    SetStatus("Payout may already have dispensed. Verify the physical tickets, then press TEST TICKETS again within 5 seconds to clear the fault without retrying.", new Color(1f, 0.3f, 0.2f));
                    return;
                }

                GameServices.Tickets.DiscardFailedPayout();
                discardTicketFaultArmedUntil = 0f;
                SetStatus("Reviewed ticket fault cleared. No duplicate payout was sent.", Color.yellow);
                return;
            }

            string transactionId = "operator-test-" + Guid.NewGuid().ToString("N");
            GameServices.Tickets?.DispenseTickets(5, transactionId);
            SetStatus("Queued a verified 5-ticket hardware test.", Color.yellow);
            GameServices.Audio?.PlaySfx(AudioCue.TicketCount);
        }

        private void ResetStatistics()
        {
            if (Time.unscaledTime > resetStatisticsArmedUntil)
            {
                resetStatisticsArmedUntil = Time.unscaledTime + 4f;
                SetStatus("Press RESET STATISTICS again within 4 seconds to confirm.", new Color(1f, 0.35f, 0.25f));
                return;
            }

            GameServices.Save?.ResetStatistics();
            resetStatisticsArmedUntil = 0f;
            RefreshStatistics();
            SetStatus("Lifetime machine statistics reset.", Color.yellow);
        }

        private void HandleOperatorButton()
        {
            if (inputTestMode)
            {
                ReportInput("OPERATOR / M");
                return;
            }

            BackToAttract();
        }

        private void HandleBackButton()
        {
            if (inputTestMode)
            {
                ReportInput("BACK / ESC");
                return;
            }

            BackToAttract();
        }

        private void BackToAttract()
        {
            UnsubscribeInput();
            GameServices.Bootstrap?.GoToAttractMode();
        }

        private void HandleLeftTest() => ReportInput("LEFT");
        private void HandleRightTest() => ReportInput("RIGHT");
        private void HandlePopTest() => ReportInput("POP");
        private void HandleStartTest() => ReportInput("START");
        private void HandleCreditTest(BalloonRush.Input.CreditPulseType type) => ReportInput(type == BalloonRush.Input.CreditPulseType.Coin ? "COIN" : "CARD SWIPE");

        private void ReportInput(string inputName)
        {
            if (inputTestMode)
            {
                SetStatus($"INPUT RECEIVED: {inputName}", new Color(0.25f, 1f, 0.45f));
                GameServices.Audio?.PlayUi(AudioCue.ButtonClick);
            }
        }

        private void RefreshAllRows()
        {
            for (int i = 0; i < rowRefreshers.Count; i++)
            {
                rowRefreshers[i]?.Invoke();
            }
        }

        private void RefreshStatistics()
        {
            if (statisticsText == null || GameServices.Save == null || GameServices.Save.Data == null)
            {
                return;
            }

            MachineStatistics stats = GameServices.Save.Data.statistics;
            OperatorSettings current = GameServices.Settings != null ? GameServices.Settings.Current : editable;
            float estimatedCostPercent = EconomyMath.EstimatePrizeCostPercent(stats.AverageTicketsPerGame, current);
            float estimatedCostDollars = EconomyMath.EstimatePrizeCostCents(stats.AverageTicketsPerGame, current) / 100f;
            statisticsText.text =
                $"GAMES {stats.gamesPlayed:N0}     SWIPES {stats.cardSwipes:N0}     CREDITS {stats.totalCredits:N0}     REVENUE ${stats.totalRevenueCents / 100f:N2}\n" +
                $"AWARDED {stats.totalTicketsAwarded:N0}     CONFIRMED PAID {stats.totalTicketsPaid:N0}     AVG {stats.AverageTicketsPerGame:0.0}     EST COST ${estimatedCostDollars:0.000} ({estimatedCostPercent:0.0}%)\n" +
                $"JACKPOTS {stats.jackpotsWon:N0}     FAILURES {stats.ticketPayoutFailures:N0}     MISMATCHES {stats.ticketPayoutMismatches:N0}     PERFECTS {stats.perfectPops:N0}";
        }

        private void AddInfoRow(string label, string value)
        {
            GameObject row = CreateRow("Info - " + label, 94f);
            Image image = row.GetComponent<Image>();
            image.color = new Color(0.03f, 0.12f, 0.25f, 0.95f);

            TMP_Text heading = CreateText(row.transform, label, 22f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            RectTransform headingRect = heading.rectTransform;
            headingRect.anchorMin = new Vector2(0f, 0f);
            headingRect.anchorMax = new Vector2(0.19f, 1f);
            headingRect.offsetMin = new Vector2(18f, 4f);
            headingRect.offsetMax = new Vector2(-8f, -4f);
            heading.color = new Color(0.25f, 0.9f, 1f);

            TMP_Text valueText = CreateText(row.transform, value, 21f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            RectTransform valueRect = valueText.rectTransform;
            valueRect.anchorMin = new Vector2(0.19f, 0f);
            valueRect.anchorMax = Vector2.one;
            valueRect.offsetMin = new Vector2(8f, 4f);
            valueRect.offsetMax = new Vector2(-18f, -4f);
            valueText.enableAutoSizing = true;
            valueText.fontSizeMin = 14f;
            valueText.fontSizeMax = 21f;
            valueText.textWrappingMode = TextWrappingModes.Normal;
            valueText.overflowMode = TextOverflowModes.Overflow;
        }

        private void AddHeader(string label)
        {
            GameObject row = CreateRow("Header - " + label, 58f);
            Image image = row.GetComponent<Image>();
            image.sprite = RuntimeSpriteLibrary.SolidSprite;
            image.color = new Color(0.08f, 0.35f, 0.62f, 0.75f);
            TMP_Text text = CreateText(row.transform, label, 30f, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 18f, 18f, 4f, 4f);
        }

        private void AddIntField(string label, Func<int> getter, Action<int> setter)
        {
            AddTextFieldInternal(label, () => getter().ToString(CultureInfo.InvariantCulture), value =>
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                {
                    setter(parsed);
                }
            }, TMP_InputField.ContentType.IntegerNumber);
        }

        private void AddFloatField(string label, Func<float> getter, Action<float> setter)
        {
            AddTextFieldInternal(label, () => getter().ToString("0.###", CultureInfo.InvariantCulture), value =>
            {
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                {
                    setter(parsed);
                }
            }, TMP_InputField.ContentType.DecimalNumber);
        }

        private void AddTextField(string label, Func<string> getter, Action<string> setter)
        {
            AddTextFieldInternal(label, getter, setter, TMP_InputField.ContentType.Standard);
        }

        private void AddTextFieldInternal(string label, Func<string> getter, Action<string> setter, TMP_InputField.ContentType contentType)
        {
            GameObject row = CreateRow(label, 108f);

            TMP_Text labelText = CreateText(
                row.transform,
                label,
                23f,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);

            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = new Vector2(0.025f, 0.46f);
            labelRect.anchorMax = new Vector2(0.975f, 0.965f);
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = new Vector2(-10f, 0f);

            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 14f;
            labelText.fontSizeMax = 23f;
            labelText.textWrappingMode = TextWrappingModes.Normal;
            labelText.overflowMode = TextOverflowModes.Overflow;
            labelText.lineSpacing = -4f;

            TMP_InputField input =
                CreateInputField(row.transform, contentType);

            RectTransform inputRect =
                (RectTransform)input.transform;

            inputRect.anchorMin =
                new Vector2(0.54f, 0.075f);

            inputRect.anchorMax =
                new Vector2(0.965f, 0.405f);

            inputRect.offsetMin = Vector2.zero;
            inputRect.offsetMax = Vector2.zero;
            input.pointSize = 22f;

            input.onEndEdit.AddListener(
                value => setter(value));

            Action refresh =
                () => input.SetTextWithoutNotify(getter());

            rowRefreshers.Add(refresh);
            refresh();
        }

        private void AddToggleField(string label, Func<bool> getter, Action<bool> setter)
        {
            GameObject row = CreateRow(label, 108f);

            TMP_Text labelText = CreateText(
                row.transform,
                label,
                23f,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);

            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = new Vector2(0.025f, 0.46f);
            labelRect.anchorMax = new Vector2(0.975f, 0.965f);
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = new Vector2(-10f, 0f);

            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 14f;
            labelText.fontSizeMax = 23f;
            labelText.textWrappingMode = TextWrappingModes.Normal;
            labelText.overflowMode = TextOverflowModes.Overflow;
            labelText.lineSpacing = -4f;

            Toggle toggle =
                CreateToggle(row.transform);

            RectTransform toggleRect =
                (RectTransform)toggle.transform;

            toggleRect.anchorMin =
                new Vector2(0.78f, 0.055f);

            toggleRect.anchorMax =
                new Vector2(0.965f, 0.415f);

            toggleRect.offsetMin = Vector2.zero;
            toggleRect.offsetMax = Vector2.zero;

            toggle.onValueChanged.AddListener(
                value => setter(value));

            Action refresh =
                () => toggle.SetIsOnWithoutNotify(getter());

            rowRefreshers.Add(refresh);
            refresh();
        }

        private GameObject CreateRow(string name, float height)
        {
            GameObject row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(settingsContent, false);
            LayoutElement element = row.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            Image background = row.GetComponent<Image>();
            if (background == null)
            {
                background = row.AddComponent<Image>();
                background.sprite = RuntimeSpriteLibrary.SolidSprite;
                background.color = new Color(0.03f, 0.06f, 0.15f, 0.78f);
            }

            Outline outline = row.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.58f, 0.90f, 0.24f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;
            return row;
        }

        private TMP_Text CreateText(Transform parent, string value, float fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = operatorFont;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private TMP_InputField CreateInputField(Transform parent, TMP_InputField.ContentType contentType)
        {
            GameObject inputObject = new GameObject("Input", typeof(RectTransform));
            inputObject.transform.SetParent(parent, false);
            Image background = inputObject.AddComponent<Image>();
            background.sprite = RuntimeSpriteLibrary.SolidSprite;
            background.color = new Color(0.05f, 0.12f, 0.26f, 1f);
            Outline inputOutline = inputObject.AddComponent<Outline>();
            inputOutline.effectColor = new Color(0.10f, 0.75f, 1f, 0.65f);
            inputOutline.effectDistance = new Vector2(2f, -2f);
            inputOutline.useGraphicAlpha = false;

            TMP_InputField input = inputObject.AddComponent<TMP_InputField>();
            input.contentType = contentType;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.caretColor = Color.white;
            input.selectionColor = new Color(0.15f, 0.65f, 1f, 0.55f);

            GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(inputObject.transform, false);
            RectTransform areaRect = (RectTransform)textArea.transform;
            Stretch(areaRect, 12f, 12f, 4f, 4f);

            TextMeshProUGUI text = (TextMeshProUGUI)CreateText(textArea.transform, string.Empty, 26f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
            text.color = Color.white;

            TextMeshProUGUI placeholder = (TextMeshProUGUI)CreateText(textArea.transform, "value", 26f, FontStyles.Italic, TextAlignmentOptions.MidlineLeft);
            Stretch(placeholder.rectTransform, 0f, 0f, 0f, 0f);
            placeholder.color = new Color(1f, 1f, 1f, 0.25f);

            input.textViewport = areaRect;
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        private static Toggle CreateToggle(Transform parent)
        {
            GameObject toggleObject = new GameObject("Toggle", typeof(RectTransform));
            toggleObject.transform.SetParent(parent, false);
            Toggle toggle = toggleObject.AddComponent<Toggle>();

            GameObject backgroundObject = new GameObject("Background", typeof(RectTransform));
            backgroundObject.transform.SetParent(toggleObject.transform, false);
            RectTransform backgroundRect = (RectTransform)backgroundObject.transform;
            backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(56f, 38f);
            Image background = backgroundObject.AddComponent<Image>();
            background.sprite = RuntimeSpriteLibrary.SolidSprite;
            background.color = new Color(0.05f, 0.12f, 0.26f, 1f);
            Outline toggleOutline = backgroundObject.AddComponent<Outline>();
            toggleOutline.effectColor = new Color(0.10f, 0.75f, 1f, 0.65f);
            toggleOutline.effectDistance = new Vector2(2f, -2f);
            toggleOutline.useGraphicAlpha = false;

            GameObject checkObject = new GameObject("Checkmark", typeof(RectTransform));
            checkObject.transform.SetParent(backgroundObject.transform, false);
            RectTransform checkRect = (RectTransform)checkObject.transform;
            Stretch(checkRect, 7f, 7f, 7f, 7f);
            Image check = checkObject.AddComponent<Image>();
            check.sprite = RuntimeSpriteLibrary.SolidSprite;
            check.color = new Color(0.15f, 1f, 0.45f, 1f);

            toggle.targetGraphic = background;
            toggle.graphic = check;
            return toggle;
        }

        private void SetStatus(string value, Color color)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = value;
            statusText.color = color;
        }

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
