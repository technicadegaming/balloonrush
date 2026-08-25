using System;
using System.Collections;
using System.IO;
using System.Reflection;
using BalloonRush.Audio;
using BalloonRush.Core;
using BalloonRush.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// v1.9.4 cabinet operations layer.
    ///
    /// Adds operator-maintenance counters/actions, attract-music duty cycling,
    /// and a player-only borderless-fullscreen watchdog without changing gameplay,
    /// ticket economics, or the cabinet joystick mapping.
    /// </summary>
    [DefaultExecutionOrder(1200)]
    public sealed class CabinetOperationsV194 : MonoBehaviour
    {
        [Serializable]
        private sealed class CabinetOperationsSettings
        {
            public int version = 1;
            public bool attractMusicCycleEnabled = true;
            public int attractMusicPlaySeconds = 15;
            public int attractMusicSilentSeconds = 45;

            public void Validate()
            {
                version = 1;
                attractMusicPlaySeconds = Mathf.Clamp(attractMusicPlaySeconds, 3, 180);
                attractMusicSilentSeconds = Mathf.Clamp(attractMusicSilentSeconds, 0, 900);
            }
        }

        private const string SettingsFileName = "BalloonRushCabinetOperations.json";
        private const float CounterRefreshSeconds = 0.35f;
        private const float DisplayWatchdogSeconds = 2f;

        private CabinetOperationsSettings operations = new CabinetOperationsSettings();
        private Coroutine attractCycleRoutine;
        private GameState lastState = GameState.Boot;
        private float nextCounterRefresh;
        private float nextDisplayWatchdog;
        private int injectedSceneHandle = -1;

        private TMP_Text currentCreditsValue;
        private TMP_Text pendingTicketsValue;
        private TMP_Text lifetimeCreditsValue;
        private TMP_Text lifetimeTicketsValue;
        private TMP_Text operatorStatusText;
        private OperatorMenuManager operatorMenu;

        private float clearCreditsArmedUntil;
        private float clearPendingTicketsArmedUntil;
        private float clearLifetimeCreditsArmedUntil;
        private float clearLifetimeTicketsArmedUntil;

        private string SettingsPath => Path.Combine(Application.persistentDataPath, SettingsFileName);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureRuntimeObject()
        {
            CabinetOperationsV194 existing = FindFirstObjectByType<CabinetOperationsV194>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return;
            }

            GameObject host = new GameObject("Balloon Rush Cabinet Operations v1.9.4");
            host.AddComponent<CabinetOperationsV194>();
            DontDestroyOnLoad(host);
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            LoadOperationsSettings();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            StartCoroutine(WaitForServices());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            StopAttractCycle();
        }

        private IEnumerator WaitForServices()
        {
            float timeout = 8f;
            while (!GameServices.IsReady && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (GameServices.State != null)
            {
                lastState = GameServices.State.CurrentState;
                HandleGameStateChanged(lastState);
            }
        }

        private void Update()
        {
            GameState state = GameServices.State != null ? GameServices.State.CurrentState : GameState.Boot;
            if (state != lastState)
            {
                lastState = state;
                HandleGameStateChanged(state);
            }

            if (Time.unscaledTime >= nextCounterRefresh)
            {
                nextCounterRefresh = Time.unscaledTime + CounterRefreshSeconds;
                RefreshCounterValues();
            }

#if !UNITY_EDITOR
            if (Time.unscaledTime >= nextDisplayWatchdog)
            {
                nextDisplayWatchdog = Time.unscaledTime + DisplayWatchdogSeconds;
                EnforceBorderlessCabinetDisplay();
            }
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
#if !UNITY_EDITOR
            if (hasFocus)
            {
                EnforceBorderlessCabinetDisplay();
            }
#endif
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.Attract)
            {
                RestartAttractCycle();
            }
            else
            {
                StopAttractCycle();
            }
        }

        private void RestartAttractCycle()
        {
            StopAttractCycle();
            if (GameServices.State == null || GameServices.State.CurrentState != GameState.Attract)
            {
                return;
            }

            attractCycleRoutine = StartCoroutine(AttractMusicCycle());
        }

        private void StopAttractCycle()
        {
            if (attractCycleRoutine != null)
            {
                StopCoroutine(attractCycleRoutine);
                attractCycleRoutine = null;
            }
        }

        private IEnumerator AttractMusicCycle()
        {
            yield return null;

            while (GameServices.State != null && GameServices.State.CurrentState == GameState.Attract)
            {
                GameServices.Audio?.PlayMusic(MusicCue.Attract, 0.35f);

                if (!operations.attractMusicCycleEnabled)
                {
                    attractCycleRoutine = null;
                    yield break;
                }

                float playUntil = Time.unscaledTime + Mathf.Max(3, operations.attractMusicPlaySeconds);
                while (Time.unscaledTime < playUntil &&
                       GameServices.State != null &&
                       GameServices.State.CurrentState == GameState.Attract)
                {
                    yield return null;
                }

                if (GameServices.State == null || GameServices.State.CurrentState != GameState.Attract)
                {
                    break;
                }

                GameServices.Audio?.StopMusic(0.35f);

                float silentUntil = Time.unscaledTime + Mathf.Max(0, operations.attractMusicSilentSeconds);
                while (Time.unscaledTime < silentUntil &&
                       GameServices.State != null &&
                       GameServices.State.CurrentState == GameState.Attract)
                {
                    yield return null;
                }
            }

            attractCycleRoutine = null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            injectedSceneHandle = -1;
            currentCreditsValue = null;
            pendingTicketsValue = null;
            lifetimeCreditsValue = null;
            lifetimeTicketsValue = null;
            operatorStatusText = null;
            operatorMenu = null;

            if (string.Equals(scene.name, GameBootstrap.OperatorSceneName, StringComparison.Ordinal))
            {
                StartCoroutine(InjectOperatorControlsWhenReady(scene));
            }
        }

        private IEnumerator InjectOperatorControlsWhenReady(Scene scene)
        {
            float timeout = 4f;
            while (timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                operatorMenu = FindFirstObjectByType<OperatorMenuManager>(FindObjectsInactive.Include);
                RectTransform content = FindSettingsContent();
                if (operatorMenu != null && content != null && content.childCount > 0)
                {
                    InjectOperatorControls(scene, content);
                    yield break;
                }
                yield return null;
            }

            Debug.LogWarning("Balloon Rush v1.9.4 could not locate the Operator settings content for cabinet maintenance controls.");
        }

        private void InjectOperatorControls(Scene scene, RectTransform content)
        {
            if (injectedSceneHandle == scene.handle)
            {
                return;
            }

            if (FindChildByName(content, "V194 - CABINET OPERATIONS") != null)
            {
                injectedSceneHandle = scene.handle;
                return;
            }

            operatorStatusText = GetPrivateField<TMP_Text>(operatorMenu, "statusText");

            int insertIndex = Mathf.Min(1, content.childCount);
            CreateHeader(content, "V194 - CABINET OPERATIONS", "CABINET / ATTRACT OPERATIONS", insertIndex++);
            CreateToggleRow(
                content,
                "V194 - Attract music cycle enabled",
                "Attract music cycle enabled",
                () => operations.attractMusicCycleEnabled,
                value =>
                {
                    operations.attractMusicCycleEnabled = value;
                    SaveOperationsSettings();
                    RestartAttractCycle();
                    SetStatus(value ? "Attract music cycling enabled." : "Attract music will play continuously while in Attract.", Color.cyan);
                },
                insertIndex++);

            CreateIntegerFieldRow(
                content,
                "V194 - Attract music play duration seconds",
                "Attract music PLAY duration (seconds)",
                () => operations.attractMusicPlaySeconds,
                value =>
                {
                    operations.attractMusicPlaySeconds = Mathf.Clamp(value, 3, 180);
                    SaveOperationsSettings();
                    RestartAttractCycle();
                },
                insertIndex++);

            CreateIntegerFieldRow(
                content,
                "V194 - Attract music silent duration seconds",
                "Attract music SILENT duration (seconds)",
                () => operations.attractMusicSilentSeconds,
                value =>
                {
                    operations.attractMusicSilentSeconds = Mathf.Clamp(value, 0, 900);
                    SaveOperationsSettings();
                    RestartAttractCycle();
                },
                insertIndex++);

            CreateInfoRow(
                content,
                "V194 - Audio note",
                "MUSIC ROUTING",
                "Attract and Gameplay use different music cues. MASTER / MUSIC / SFX volume controls remain in AUDIO AND ACCESSIBILITY below.",
                insertIndex++);

            CreateHeader(content, "V194 - MACHINE COUNTERS", "MACHINE COUNTERS / MAINTENANCE", insertIndex++);
            currentCreditsValue = CreateCounterActionRow(
                content,
                "V194 - Current credits",
                "CURRENT CREDITS",
                "CLEAR CREDITS",
                ClearCurrentCredits,
                insertIndex++);

            pendingTicketsValue = CreateCounterActionRow(
                content,
                "V194 - Pending tickets",
                "PENDING TICKETS",
                "CLEAR PENDING",
                ClearPendingTickets,
                insertIndex++);

            lifetimeCreditsValue = CreateCounterActionRow(
                content,
                "V194 - Lifetime credits",
                "LIFETIME CREDITS",
                "CLEAR LIFETIME CREDITS",
                ClearLifetimeCredits,
                insertIndex++);

            lifetimeTicketsValue = CreateCounterActionRow(
                content,
                "V194 - Lifetime tickets",
                "LIFETIME TICKETS",
                "CLEAR LIFETIME TICKETS",
                ClearLifetimeTickets,
                insertIndex++);

            injectedSceneHandle = scene.handle;
            RefreshCounterValues();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        }

        private void ClearCurrentCredits()
        {
            int credits = GameServices.Credits != null ? GameServices.Credits.Credits : 0;
            if (credits <= 0)
            {
                SetStatus("Current credits are already 0.", Color.white);
                return;
            }

            if (!Confirm(ref clearCreditsArmedUntil, $"Press CLEAR CREDITS again within 4 seconds to remove {credits} unplayed credit(s)."))
            {
                return;
            }

            GameServices.Credits?.ClearCredits();
            SetStatus("Current/unplayed credits cleared. Lifetime credit/revenue audit counters were not changed.", Color.yellow);
            RefreshCounterValues();
        }

        private void ClearPendingTickets()
        {
            if (GameServices.Tickets == null || GameServices.Tickets.TicketsRemaining <= 0)
            {
                SetStatus("There are no pending tickets to clear.", Color.white);
                return;
            }

            int before = GameServices.Tickets.TicketsRemaining;
            if (!Confirm(ref clearPendingTicketsArmedUntil, $"Press CLEAR PENDING again within 4 seconds to cancel {before} queued/unsent ticket(s)."))
            {
                return;
            }

            GameServices.Tickets.CancelDispensing();
            int after = GameServices.Tickets.TicketsRemaining;
            if (after > 0 || GameServices.Tickets.IsDispensing)
            {
                SetStatus("Ticket command may already have been sent. It was NOT cancelled; review PAID:n / payout status to avoid a double-pay.", new Color(1f, 0.35f, 0.20f));
            }
            else
            {
                SetStatus("Queued/unsent ticket work cleared. Lifetime ticket counters were not changed.", Color.yellow);
            }
            RefreshCounterValues();
        }

        private void ClearLifetimeCredits()
        {
            MachineStatistics stats = GetStatistics();
            if (stats == null || stats.totalCredits <= 0)
            {
                SetStatus("Lifetime credits are already 0.", Color.white);
                return;
            }

            if (!Confirm(ref clearLifetimeCreditsArmedUntil, $"Press CLEAR LIFETIME CREDITS again within 4 seconds to reset the {stats.totalCredits:N0} credit counter."))
            {
                return;
            }

            // Deliberately keep card swipes, coin pulses, and revenue as audit history.
            stats.totalCredits = 0;
            GameServices.Save?.Save();
            RefreshOperatorStatistics();
            RefreshCounterValues();
            SetStatus("Lifetime credit-unit counter reset. Swipe/coin/revenue audit history was preserved.", Color.yellow);
        }

        private void ClearLifetimeTickets()
        {
            MachineStatistics stats = GetStatistics();
            if (stats == null || (stats.totalTicketsAwarded <= 0 && stats.totalTicketsPaid <= 0))
            {
                SetStatus("Lifetime ticket counters are already 0.", Color.white);
                return;
            }

            if (!Confirm(ref clearLifetimeTicketsArmedUntil,
                    $"Press CLEAR LIFETIME TICKETS again within 4 seconds to reset AWARDED {stats.totalTicketsAwarded:N0} / PAID {stats.totalTicketsPaid:N0}."))
            {
                return;
            }

            stats.totalTicketsAwarded = 0;
            stats.totalTicketsPaid = 0;
            GameServices.Save?.Save();
            RefreshOperatorStatistics();
            RefreshCounterValues();
            SetStatus("Lifetime ticket awarded/paid counters reset. Payout failure/mismatch history was preserved.", Color.yellow);
        }

        private bool Confirm(ref float armedUntil, string message)
        {
            if (Time.unscaledTime > armedUntil)
            {
                armedUntil = Time.unscaledTime + 4f;
                SetStatus(message, new Color(1f, 0.42f, 0.20f));
                return false;
            }

            armedUntil = 0f;
            return true;
        }

        private void RefreshCounterValues()
        {
            if (currentCreditsValue != null)
            {
                int credits = GameServices.Credits != null ? GameServices.Credits.Credits : 0;
                currentCreditsValue.text = credits.ToString("N0");
            }

            if (pendingTicketsValue != null)
            {
                int tickets = GameServices.Tickets != null ? GameServices.Tickets.TicketsRemaining : 0;
                pendingTicketsValue.text = tickets.ToString("N0");
            }

            MachineStatistics stats = GetStatistics();
            if (stats != null)
            {
                if (lifetimeCreditsValue != null)
                {
                    lifetimeCreditsValue.text = stats.totalCredits.ToString("N0");
                }

                if (lifetimeTicketsValue != null)
                {
                    lifetimeTicketsValue.text = $"AWARDED {stats.totalTicketsAwarded:N0}   /   PAID {stats.totalTicketsPaid:N0}";
                }
            }
        }

        private static MachineStatistics GetStatistics()
        {
            return GameServices.Save != null && GameServices.Save.Data != null
                ? GameServices.Save.Data.statistics
                : null;
        }

        private void RefreshOperatorStatistics()
        {
            if (operatorMenu == null)
            {
                return;
            }

            MethodInfo method = typeof(OperatorMenuManager).GetMethod(
                "RefreshStatistics",
                BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(operatorMenu, null);
        }

        private void SetStatus(string message, Color color)
        {
            if (operatorStatusText == null && operatorMenu != null)
            {
                operatorStatusText = GetPrivateField<TMP_Text>(operatorMenu, "statusText");
            }

            if (operatorStatusText != null)
            {
                operatorStatusText.text = message;
                operatorStatusText.color = color;
            }

            Debug.Log("Balloon Rush Operator: " + message);
        }

        private static T GetPrivateField<T>(object instance, string fieldName) where T : class
        {
            if (instance == null)
            {
                return null;
            }

            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null ? field.GetValue(instance) as T : null;
        }

        private RectTransform FindSettingsContent()
        {
            ScrollRect[] scrolls = FindObjectsByType<ScrollRect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < scrolls.Length; i++)
            {
                ScrollRect scroll = scrolls[i];
                if (scroll == null || scroll.content == null || !scroll.gameObject.scene.IsValid())
                {
                    continue;
                }

                string key = (scroll.name + " " + (scroll.transform.parent != null ? scroll.transform.parent.name : string.Empty)).ToLowerInvariant();
                if (key.Contains("setting") || scroll.content.name == "Content")
                {
                    return scroll.content;
                }
            }
            return null;
        }

        private static Transform FindChildByName(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != null && string.Equals(child.name, name, StringComparison.Ordinal))
                {
                    return child;
                }
            }
            return null;
        }

        private static void CreateHeader(RectTransform content, string objectName, string label, int siblingIndex)
        {
            GameObject row = CreateBaseRow(content, objectName, 58f, new Color(0.08f, 0.35f, 0.62f, 0.82f), siblingIndex);
            TMP_Text text = CreateText(row.transform, label, 29f, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 14f, 14f, 3f, 3f);
        }

        private static void CreateInfoRow(RectTransform content, string objectName, string label, string value, int siblingIndex)
        {
            GameObject row = CreateBaseRow(content, objectName, 88f, new Color(0.03f, 0.12f, 0.25f, 0.96f), siblingIndex);
            TMP_Text heading = CreateText(row.transform, label, 20f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            heading.color = new Color(0.25f, 0.9f, 1f);
            RectTransform h = heading.rectTransform;
            h.anchorMin = new Vector2(0f, 0f);
            h.anchorMax = new Vector2(0.24f, 1f);
            h.offsetMin = new Vector2(18f, 5f);
            h.offsetMax = new Vector2(-6f, -5f);

            TMP_Text body = CreateText(row.transform, value, 18f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            body.enableAutoSizing = true;
            body.fontSizeMin = 13f;
            body.fontSizeMax = 18f;
            body.textWrappingMode = TextWrappingModes.Normal;
            RectTransform b = body.rectTransform;
            b.anchorMin = new Vector2(0.24f, 0f);
            b.anchorMax = Vector2.one;
            b.offsetMin = new Vector2(8f, 5f);
            b.offsetMax = new Vector2(-18f, -5f);
        }

        private void CreateToggleRow(
            RectTransform content,
            string objectName,
            string label,
            Func<bool> getter,
            Action<bool> setter,
            int siblingIndex)
        {
            GameObject row = CreateBaseRow(content, objectName, 64f, new Color(0.03f, 0.06f, 0.15f, 0.90f), siblingIndex);
            HorizontalLayoutGroup layout = AddHorizontalLayout(row, 16, 18, 8, 8, 12f);

            TMP_Text labelText = CreateText(row.transform, label, 21f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 16f;
            labelText.fontSizeMax = 21f;
            LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            labelLayout.minWidth = 420f;

            GameObject toggleObject = new GameObject("Toggle", typeof(RectTransform), typeof(Image));
            toggleObject.transform.SetParent(row.transform, false);
            Image background = toggleObject.GetComponent<Image>();
            background.color = new Color(0.05f, 0.12f, 0.26f, 1f);
            Toggle toggle = toggleObject.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            LayoutElement toggleLayout = toggleObject.AddComponent<LayoutElement>();
            toggleLayout.preferredWidth = 92f;
            toggleLayout.preferredHeight = 44f;

            GameObject checkObject = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkObject.transform.SetParent(toggleObject.transform, false);
            RectTransform checkRect = (RectTransform)checkObject.transform;
            Stretch(checkRect, 9f, 9f, 8f, 8f);
            Image check = checkObject.GetComponent<Image>();
            check.color = new Color(0.15f, 1f, 0.45f, 1f);
            toggle.graphic = check;
            toggle.SetIsOnWithoutNotify(getter());
            toggle.onValueChanged.AddListener(value => setter(value));
        }

        private void CreateIntegerFieldRow(
            RectTransform content,
            string objectName,
            string label,
            Func<int> getter,
            Action<int> setter,
            int siblingIndex)
        {
            GameObject row = CreateBaseRow(content, objectName, 64f, new Color(0.03f, 0.06f, 0.15f, 0.90f), siblingIndex);
            AddHorizontalLayout(row, 16, 18, 8, 8, 12f);

            TMP_Text labelText = CreateText(row.transform, label, 21f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 16f;
            labelText.fontSizeMax = 21f;
            LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            labelLayout.minWidth = 420f;

            TMP_InputField input = CreateIntegerInput(row.transform);
            LayoutElement inputLayout = input.gameObject.AddComponent<LayoutElement>();
            inputLayout.preferredWidth = 220f;
            inputLayout.minWidth = 190f;
            input.SetTextWithoutNotify(getter().ToString());
            input.onEndEdit.AddListener(value =>
            {
                if (int.TryParse(value, out int parsed))
                {
                    setter(parsed);
                }
                input.SetTextWithoutNotify(getter().ToString());
            });
        }

        private TMP_Text CreateCounterActionRow(
            RectTransform content,
            string objectName,
            string label,
            string actionLabel,
            Action action,
            int siblingIndex)
        {
            GameObject row = CreateBaseRow(content, objectName, 72f, new Color(0.025f, 0.075f, 0.17f, 0.96f), siblingIndex);
            AddHorizontalLayout(row, 16, 16, 7, 7, 12f);

            TMP_Text labelText = CreateText(row.transform, label, 20f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            labelText.color = new Color(0.65f, 0.92f, 1f);
            LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 250f;
            labelLayout.minWidth = 220f;

            TMP_Text valueText = CreateText(row.transform, "0", 23f, FontStyles.Bold, TextAlignmentOptions.Center);
            valueText.color = Color.white;
            valueText.enableAutoSizing = true;
            valueText.fontSizeMin = 14f;
            valueText.fontSizeMax = 23f;
            LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.flexibleWidth = 1f;
            valueLayout.minWidth = 250f;

            Button button = CreateActionButton(row.transform, actionLabel);
            LayoutElement buttonLayout = button.gameObject.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = actionLabel.Length > 16 ? 275f : 220f;
            buttonLayout.minWidth = 200f;
            buttonLayout.preferredHeight = 48f;
            button.onClick.AddListener(() => action());

            return valueText;
        }

        private static GameObject CreateBaseRow(RectTransform content, string name, float height, Color color, int siblingIndex)
        {
            GameObject row = new GameObject(name, typeof(RectTransform), typeof(Image));
            row.transform.SetParent(content, false);
            row.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, content.childCount - 1));
            Image image = row.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            LayoutElement element = row.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0f;

            Outline outline = row.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.58f, 0.90f, 0.24f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = false;
            return row;
        }

        private static HorizontalLayoutGroup AddHorizontalLayout(GameObject row, int left, int right, int top, int bottom, float spacing)
        {
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(left, right, top, bottom);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        private static Button CreateActionButton(Transform parent, string label)
        {
            GameObject buttonObject = new GameObject(label + " Button", typeof(RectTransform), typeof(Image));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.05f, 0.58f, 0.82f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.85f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.70f, 0.90f, 1f, 1f);
            colors.selectedColor = new Color(0.90f, 1f, 1f, 1f);
            button.colors = colors;

            TMP_Text text = CreateText(buttonObject.transform, label, 17f, FontStyles.Bold, TextAlignmentOptions.Center);
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = 17f;
            text.raycastTarget = false;
            Stretch(text.rectTransform, 7f, 7f, 4f, 4f);
            return button;
        }

        private static TMP_InputField CreateIntegerInput(Transform parent)
        {
            GameObject inputObject = new GameObject("Input", typeof(RectTransform), typeof(Image));
            inputObject.transform.SetParent(parent, false);
            Image background = inputObject.GetComponent<Image>();
            background.color = new Color(0.05f, 0.12f, 0.26f, 1f);

            TMP_InputField input = inputObject.AddComponent<TMP_InputField>();
            input.contentType = TMP_InputField.ContentType.IntegerNumber;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.pointSize = 25f;
            input.caretColor = Color.white;
            input.selectionColor = new Color(0.15f, 0.65f, 1f, 0.55f);

            GameObject areaObject = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            areaObject.transform.SetParent(inputObject.transform, false);
            RectTransform area = (RectTransform)areaObject.transform;
            Stretch(area, 10f, 10f, 4f, 4f);

            TextMeshProUGUI text = (TextMeshProUGUI)CreateText(areaObject.transform, string.Empty, 25f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
            TextMeshProUGUI placeholder = (TextMeshProUGUI)CreateText(areaObject.transform, "value", 25f, FontStyles.Italic, TextAlignmentOptions.MidlineLeft);
            Stretch(placeholder.rectTransform, 0f, 0f, 0f, 0f);
            placeholder.color = new Color(1f, 1f, 1f, 0.25f);

            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        private static TMP_Text CreateText(Transform parent, string value, float fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private void LoadOperationsSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    operations = JsonUtility.FromJson<CabinetOperationsSettings>(File.ReadAllText(SettingsPath));
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Balloon Rush could not load cabinet operations settings: " + exception.Message);
            }

            if (operations == null)
            {
                operations = new CabinetOperationsSettings();
            }
            operations.Validate();
            SaveOperationsSettings();
        }

        private void SaveOperationsSettings()
        {
            operations.Validate();
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(SettingsPath, JsonUtility.ToJson(operations, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Balloon Rush could not save cabinet operations settings: " + exception.Message);
            }
        }

#if !UNITY_EDITOR
        private void EnforceBorderlessCabinetDisplay()
        {
            int width = GameServices.Config != null ? Mathf.Max(480, GameServices.Config.targetWidth) : 1080;
            int height = GameServices.Config != null ? Mathf.Max(800, GameServices.Config.targetHeight) : 1920;

            Application.runInBackground = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;

            bool wrongMode = Screen.fullScreenMode != FullScreenMode.FullScreenWindow || !Screen.fullScreen;
            bool wrongSize = Screen.width != width || Screen.height != height;
            if (wrongMode || wrongSize)
            {
                Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
            }
        }
#endif
    }
}
