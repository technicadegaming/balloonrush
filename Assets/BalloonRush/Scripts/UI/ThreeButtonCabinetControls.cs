using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using BalloonRush.Core;
using BalloonRush.Input;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// Cabinet flow/controller for the actual WOWCade hardware.
    ///
    /// LEFT  = LeftArrow / JoystickButton1
    /// POP   = UpArrow / JoystickButton2
    /// RIGHT = RightArrow / JoystickButton7
    /// MENU  = M / JoystickButton4 keyed switch (handled as OperatorPressed)
    ///
    /// Attract: a paid credit automatically starts one game after a short delay.
    /// POP can still start immediately when a credit is available.
    /// Operator Menu: LEFT/RIGHT move focus, POP select/edit/confirm.
    /// </summary>
    [DefaultExecutionOrder(800)]
    public sealed class ThreeButtonCabinetControls : MonoBehaviour
    {
        public const string Version = "1.9.2";

        // Mirrors the useful behavior from the older WOWCade UIFlowManager:
        // a new external credit starts promptly; a credit already waiting when
        // Attract is entered gets a slightly longer on-screen grace period.
        private const float NewCreditAutoStartDelay = 1.0f;
        private const float ExistingCreditAutoStartDelay = 3.0f;

        private ArcadeInputManager subscribedInput;
        private CreditManager subscribedCredits;
        private Coroutine autoStartRoutine;
        private bool wasInAttract;
        private bool autoStartPending;
        private float nextPromptRefresh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRuntimeObject()
        {
            ThreeButtonCabinetControls existing = FindFirstObjectByType<ThreeButtonCabinetControls>(FindObjectsInactive.Include);
            if (existing != null)
            {
                return;
            }

            GameObject host = new GameObject("Balloon Rush Three Button Cabinet Controls");
            host.AddComponent<ThreeButtonCabinetControls>();
            DontDestroyOnLoad(host);
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            CancelAutoStart();
            Unsubscribe();
        }

        private void Update()
        {
            EnsureSubscriptions();

            GameState state = GameServices.State != null ? GameServices.State.CurrentState : GameState.Boot;
            bool inAttract = state == GameState.Attract;

            if (inAttract && !wasInAttract)
            {
                HandleEnteredAttract();
            }
            else if (!inAttract && wasInAttract)
            {
                CancelAutoStart();
            }
            wasInAttract = inAttract;

            if (inAttract && Time.unscaledTime >= nextPromptRefresh)
            {
                nextPromptRefresh = Time.unscaledTime + 0.25f;
                RefreshAttractStartPrompt();
            }

            if (state == GameState.OperatorMenu || SceneManager.GetActiveScene().name == GameBootstrap.OperatorSceneName)
            {
                EnsureOperatorNavigator();
            }
        }

        private void EnsureSubscriptions()
        {
            ArcadeInputManager currentInput = GameServices.Input;
            CreditManager currentCredits = GameServices.Credits;

            if (currentInput != subscribedInput)
            {
                if (subscribedInput != null)
                {
                    subscribedInput.PopPressed -= HandlePop;
                    subscribedInput.OperatorPressed -= HandleOperatorPressed;
                }

                subscribedInput = currentInput;
                if (subscribedInput != null)
                {
                    subscribedInput.PopPressed += HandlePop;
                    subscribedInput.OperatorPressed += HandleOperatorPressed;
                }
            }

            if (currentCredits != subscribedCredits)
            {
                if (subscribedCredits != null)
                {
                    subscribedCredits.CreditsChanged -= HandleCreditsChanged;
                }

                subscribedCredits = currentCredits;
                if (subscribedCredits != null)
                {
                    subscribedCredits.CreditsChanged += HandleCreditsChanged;
                }
            }
        }

        private void Unsubscribe()
        {
            if (subscribedInput != null)
            {
                subscribedInput.PopPressed -= HandlePop;
                subscribedInput.OperatorPressed -= HandleOperatorPressed;
                subscribedInput = null;
            }

            if (subscribedCredits != null)
            {
                subscribedCredits.CreditsChanged -= HandleCreditsChanged;
                subscribedCredits = null;
            }
        }

        private void HandleEnteredAttract()
        {
            if (GameServices.Credits != null && GameServices.Credits.Credits > 0)
            {
                ScheduleAutoStart(ExistingCreditAutoStartDelay, false);
            }
        }

        private void HandleCreditsChanged(int credits)
        {
            if (credits <= 0)
            {
                CancelAutoStart();
                return;
            }

            if (GameServices.State != null && GameServices.State.CurrentState == GameState.Attract)
            {
                // A newly accepted card/coin credit should start promptly. If an
                // older 3-second pending start exists, replace it with the 1-second path.
                ScheduleAutoStart(NewCreditAutoStartDelay, true);
            }
        }

        private void HandlePop()
        {
            if (GameServices.State == null || GameServices.State.CurrentState != GameState.Attract)
            {
                return;
            }

            // A player can press the center POP button to skip the short auto-start
            // delay. This keeps development/free-play operation convenient too.
            CancelAutoStart();
            StartOneCreditedGame();
        }

        private void HandleOperatorPressed()
        {
            // The real keyed switch is M / JoystickButton4.
            // Always cancel an armed paid-game autostart before entering service.
            CancelAutoStart();

            // OperatorMenuManager normally handles OperatorPressed and returns to
            // Attract immediately. Add a one-frame safety fallback so the physical
            // key switch is guaranteed to EXIT Operator Mode even if a diagnostics
            // overlay or future menu revision temporarily owns focus.
            //
            // Waiting one frame avoids issuing a duplicate scene load when the
            // existing OperatorMenuManager already handled the same event.
            bool inOperator =
                (GameServices.State != null &&
                 GameServices.State.CurrentState == GameState.OperatorMenu) ||
                SceneManager.GetActiveScene().name == GameBootstrap.OperatorSceneName;

            if (inOperator)
            {
                StartCoroutine(EnsureOperatorExitNextFrame());
            }
        }

        private IEnumerator EnsureOperatorExitNextFrame()
        {
            yield return null;

            bool stillInOperator =
                (GameServices.State != null &&
                 GameServices.State.CurrentState == GameState.OperatorMenu) ||
                SceneManager.GetActiveScene().name == GameBootstrap.OperatorSceneName;

            if (stillInOperator)
            {
                GameServices.Bootstrap?.GoToAttractMode();
            }
        }

        private void ScheduleAutoStart(float delay, bool replaceExisting)
        {
            if (GameServices.State == null || GameServices.State.CurrentState != GameState.Attract)
            {
                return;
            }

            if (GameServices.Credits == null || !GameServices.Credits.CanStartGame())
            {
                return;
            }

            if (autoStartRoutine != null)
            {
                if (!replaceExisting)
                {
                    return;
                }

                StopCoroutine(autoStartRoutine);
                autoStartRoutine = null;
            }

            autoStartPending = true;
            RefreshAttractStartPrompt();
            autoStartRoutine = StartCoroutine(AutoStartAfterDelay(Mathf.Max(0.1f, delay)));
        }

        private IEnumerator AutoStartAfterDelay(float delay)
        {
            float deadline = Time.unscaledTime + delay;
            while (Time.unscaledTime < deadline)
            {
                if (GameServices.State == null || GameServices.State.CurrentState != GameState.Attract)
                {
                    autoStartRoutine = null;
                    autoStartPending = false;
                    yield break;
                }
                yield return null;
            }

            autoStartRoutine = null;
            autoStartPending = false;
            StartOneCreditedGame();
        }

        private void CancelAutoStart()
        {
            if (autoStartRoutine != null)
            {
                StopCoroutine(autoStartRoutine);
                autoStartRoutine = null;
            }
            autoStartPending = false;
        }

        private static void StartOneCreditedGame()
        {
            if (GameServices.State == null || GameServices.State.CurrentState != GameState.Attract)
            {
                return;
            }

            // Use the existing AttractModeManager route first. It already owns the
            // no-credit message, sound, exactly-one-credit consumption, and scene load.
            AttractModeManager attract = FindFirstObjectByType<AttractModeManager>(FindObjectsInactive.Include);
            if (attract != null)
            {
                attract.SendMessage("HandleStart", SendMessageOptions.DontRequireReceiver);
                return;
            }

            // Defensive fallback if the Attract scene is rebuilt later.
            if (GameServices.Credits != null && GameServices.Credits.TryConsumePlay())
            {
                GameServices.Bootstrap?.GoToMainGame();
            }
        }

        private void RefreshAttractStartPrompt()
        {
            int credits = GameServices.Credits != null ? GameServices.Credits.Credits : 0;
            bool freePlay = GameServices.Settings != null &&
                            GameServices.Settings.Current != null &&
                            GameServices.Settings.Current.freePlay;
            bool ready = freePlay || (GameServices.Credits != null && GameServices.Credits.CanStartGame());

            TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || !text.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(text.text))
                {
                    continue;
                }

                string value = text.text;
                bool isStartPrompt =
                    value.IndexOf("ENTER OR P TO START", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    value.IndexOf("PRESS POP TO START", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    value.IndexOf("SWIPE CARD", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    value.IndexOf("CREDIT ACCEPTED", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!isStartPrompt)
                {
                    continue;
                }

                if (autoStartPending && ready)
                {
                    text.text = credits > 1
                        ? $"{credits} CREDITS - STARTING NEXT GAME..."
                        : "CREDIT ACCEPTED - STARTING...";
                    text.color = new Color(0.32f, 1f, 0.52f, 1f);
                }
                else if (ready)
                {
                    text.text = "PRESS POP TO START";
                    text.color = new Color(0.35f, 1f, 0.55f, 1f);
                }
            }
        }

        private static void EnsureOperatorNavigator()
        {
            if (FindFirstObjectByType<ThreeButtonOperatorNavigator>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Canvas best = null;
            int bestOrder = int.MinValue;
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || !canvas.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (canvas.sortingOrder >= bestOrder)
                {
                    best = canvas;
                    bestOrder = canvas.sortingOrder;
                }
            }

            if (best != null)
            {
                best.gameObject.AddComponent<ThreeButtonOperatorNavigator>();
            }
        }
    }

    /// <summary>
    /// Runtime navigation layer for the existing Operator Menu and v1.9 diagnostics.
    /// It does not replace the existing UI; it makes its Selectables cabinet-friendly.
    /// </summary>
    public sealed class ThreeButtonOperatorNavigator : MonoBehaviour
    {
        private readonly List<Selectable> items = new List<Selectable>();

        private ArcadeInputManager input;
        private Selectable current;
        private TMP_InputField editingField;
        private Outline focusOutline;
        private TMP_Text guideText;
        private GameObject guideRoot;
        private int index;
        private bool subscribed;
        private bool editMode;
        private float nextRefresh;
        private bool lastDiagnosticsOpen;

        private IEnumerator Start()
        {
            yield return null;
            yield return null;

            BuildGuide();
            Subscribe();
            RefreshItems(true);
        }

        private void Update()
        {
            if (!subscribed && GameServices.Input != null)
            {
                Subscribe();
            }

            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + 0.35f;
                bool diagnosticsOpen = IsDiagnosticsPanelOpen();
                if (diagnosticsOpen != lastDiagnosticsOpen || current == null || !current.gameObject.activeInHierarchy)
                {
                    RefreshItems(true);
                }
                else
                {
                    RefreshItems(false);
                }
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
            RemoveFocusOutline();
        }

        private void Subscribe()
        {
            input = GameServices.Input;
            if (input == null || subscribed)
            {
                return;
            }

            input.LeftPressed += HandleLeft;
            input.RightPressed += HandleRight;
            input.PopPressed += HandlePop;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || input == null)
            {
                return;
            }

            input.LeftPressed -= HandleLeft;
            input.RightPressed -= HandleRight;
            input.PopPressed -= HandlePop;
            subscribed = false;
            input = null;
        }

        private void HandleLeft()
        {
            if (editMode)
            {
                AdjustEditingField(-1);
                return;
            }

            MoveSelection(-1);
        }

        private void HandleRight()
        {
            if (editMode)
            {
                AdjustEditingField(1);
                return;
            }

            MoveSelection(1);
        }

        private void HandlePop()
        {
            if (editMode)
            {
                CommitEdit();
                return;
            }

            ActivateCurrent();
        }

        private void MoveSelection(int direction)
        {
            RefreshItems(false);
            if (items.Count == 0)
            {
                return;
            }

            index = Mathf.Clamp(index, 0, items.Count - 1);
            index = (index + direction + items.Count) % items.Count;
            SetCurrent(items[index]);
        }

        private void ActivateCurrent()
        {
            if (current == null || !current.IsInteractable())
            {
                return;
            }

            if (current is Button button)
            {
                button.onClick.Invoke();
                StartCoroutine(RefreshAfterAction());
                return;
            }

            if (current is Toggle toggle)
            {
                toggle.isOn = !toggle.isOn;
                UpdateGuide();
                return;
            }

            if (current is TMP_InputField inputField)
            {
                BeginEdit(inputField);
            }
        }

        private IEnumerator RefreshAfterAction()
        {
            yield return null;
            RefreshItems(true);
        }

        private void BeginEdit(TMP_InputField field)
        {
            editingField = field;
            editMode = true;
            ApplyFocusOutline(new Color(1f, 0.76f, 0.10f, 1f));
            UpdateGuide();
        }

        private void CommitEdit()
        {
            if (editingField != null)
            {
                editingField.onEndEdit.Invoke(editingField.text);
                editingField.DeactivateInputField();
            }

            editingField = null;
            editMode = false;
            ApplyFocusOutline(new Color(0.10f, 0.92f, 1f, 1f));
            UpdateGuide();
        }

        private void AdjustEditingField(int direction)
        {
            if (editingField == null)
            {
                editMode = false;
                return;
            }

            string label = GetFriendlyName(editingField);
            string lower = label.ToLowerInvariant();

            if (lower.Contains("serial port"))
            {
                int port = ParseComPort(editingField.text);
                port = Mathf.Clamp(port + direction, 1, 64);
                editingField.SetTextWithoutNotify("COM" + port.ToString(CultureInfo.InvariantCulture));
                UpdateGuide();
                return;
            }

            if (lower.Contains("baud rate"))
            {
                int[] rates = { 9600, 19200, 38400, 57600, 115200, 230400, 460800 };
                int currentRate = 115200;
                int.TryParse(editingField.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentRate);
                int closest = 0;
                int closestDelta = int.MaxValue;
                for (int i = 0; i < rates.Length; i++)
                {
                    int delta = Mathf.Abs(rates[i] - currentRate);
                    if (delta < closestDelta)
                    {
                        closest = i;
                        closestDelta = delta;
                    }
                }
                closest = Mathf.Clamp(closest + direction, 0, rates.Length - 1);
                editingField.SetTextWithoutNotify(rates[closest].ToString(CultureInfo.InvariantCulture));
                UpdateGuide();
                return;
            }

            if (editingField.contentType == TMP_InputField.ContentType.IntegerNumber)
            {
                int value = 0;
                int.TryParse(editingField.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
                int step = GetIntegerStep(lower);
                value += direction * step;
                value = ClampInteger(label, value);
                editingField.SetTextWithoutNotify(value.ToString(CultureInfo.InvariantCulture));
                UpdateGuide();
                return;
            }

            float number = 0f;
            if (float.TryParse(editingField.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                number = parsed;
            }

            float floatStep = GetFloatStep(lower, number);
            number += direction * floatStep;
            number = ClampFloat(label, number);
            editingField.SetTextWithoutNotify(FormatFloat(number, floatStep));
            UpdateGuide();
        }

        private static int GetIntegerStep(string lowerLabel)
        {
            if (lowerLabel.Contains("price per play")) return 25;
            if (lowerLabel.Contains("debounce")) return 25;
            if (lowerLabel.Contains("jackpot tickets") ||
                lowerLabel.Contains("maximum tickets") ||
                lowerLabel.Contains("regular ticket cap")) return 5;
            if (lowerLabel.Contains("reward") || lowerLabel.Contains("ticket penalty")) return 5;
            return 1;
        }

        private static float GetFloatStep(string lowerLabel, float value)
        {
            if (lowerLabel.Contains("volume")) return 0.05f;
            if (lowerLabel.Contains("chance")) return 0.01f;
            if (lowerLabel.Contains("weight")) return 0.01f;
            if (lowerLabel.Contains("multiplier")) return 0.05f;
            if (lowerLabel.Contains("timing window") || lowerLabel.Contains("perfect timing") ||
                lowerLabel.Contains("great timing") || lowerLabel.Contains("good timing")) return 0.01f;
            if (lowerLabel.Contains("prize cost per ticket")) return 0.05f;
            if (lowerLabel.Contains("target prize-cost")) return 1f;
            if (lowerLabel.Contains("spawn interval")) return 0.05f;
            if (lowerLabel.Contains("speed")) return 0.1f;
            if (lowerLabel.Contains("duration") || lowerLabel.Contains("timeout") || lowerLabel.Contains("wait for")) return 0.5f;
            return Mathf.Abs(value) < 2f ? 0.05f : 0.1f;
        }

        private static int ClampInteger(string label, int value)
        {
            string lower = label.ToLowerInvariant();
            if (lower.Contains("jackpot tickets")) return Mathf.Clamp(value, 1, 500);
            if (lower.Contains("maximum tickets")) return Mathf.Clamp(value, 1, 1000);
            if (lower.Contains("game duration")) return Mathf.Clamp(value, 20, 120);
            if (lower.Contains("price per play")) return Mathf.Clamp(value, 0, 1000);
            if (lower.Contains("debounce")) return Mathf.Clamp(value, 0, 5000);
            return Mathf.Max(0, value);
        }

        private static float ClampFloat(string label, float value)
        {
            string lower = label.ToLowerInvariant();
            if (lower.Contains("volume")) return Mathf.Clamp01(value);
            if (lower.Contains("mystery-to-golden chance")) return Mathf.Clamp(value, 0f, 0.25f);
            if (lower.Contains("chance") || lower.Contains("weight")) return Mathf.Clamp(value, 0f, 1f);
            if (lower.Contains("target prize-cost")) return Mathf.Clamp(value, 0f, 100f);
            return Mathf.Max(0f, value);
        }

        private static string FormatFloat(float value, float step)
        {
            if (step < 0.02f) return value.ToString("0.00", CultureInfo.InvariantCulture);
            if (step < 0.1f) return value.ToString("0.00", CultureInfo.InvariantCulture);
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static int ParseComPort(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                string upper = value.Trim().ToUpperInvariant();
                if (upper.StartsWith("COM", StringComparison.Ordinal) &&
                    int.TryParse(upper.Substring(3), NumberStyles.Integer, CultureInfo.InvariantCulture, out int port))
                {
                    return Mathf.Clamp(port, 1, 64);
                }
            }
            return 8;
        }

        private void RefreshItems(bool forceSelection)
        {
            bool diagnosticsOpen = IsDiagnosticsPanelOpen();
            lastDiagnosticsOpen = diagnosticsOpen;

            Selectable previous = current;
            Selectable[] all = FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            items.Clear();

            Transform diagnosticsPanel = FindDiagnosticsPanel();
            for (int i = 0; i < all.Length; i++)
            {
                Selectable candidate = all[i];
                if (candidate == null || !candidate.gameObject.activeInHierarchy || !candidate.IsInteractable())
                {
                    continue;
                }

                if (!(candidate is Button) && !(candidate is Toggle) && !(candidate is TMP_InputField))
                {
                    continue;
                }

                if (guideRoot != null && candidate.transform.IsChildOf(guideRoot.transform))
                {
                    continue;
                }

                if (diagnosticsOpen && diagnosticsPanel != null && !candidate.transform.IsChildOf(diagnosticsPanel))
                {
                    continue;
                }

                // The v1.9 diagnostics panel already provides live input indicators,
                // so the older mouse-oriented input-test mode is unnecessary and can
                // conflict with three-button navigation.
                if (GetFriendlyName(candidate).IndexOf("TEST INPUT", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                items.Add(candidate);
            }

            items.Sort(CompareSelectables);

            if (items.Count == 0)
            {
                current = null;
                index = 0;
                UpdateGuide();
                return;
            }

            int previousIndex = previous != null ? items.IndexOf(previous) : -1;
            if (!forceSelection && previousIndex >= 0)
            {
                index = previousIndex;
                current = previous;
                return;
            }

            index = previousIndex >= 0 ? previousIndex : Mathf.Clamp(index, 0, items.Count - 1);
            SetCurrent(items[index]);
        }

        private static int CompareSelectables(Selectable a, Selectable b)
        {
            Vector3 pa = GetWorldCenter(a.transform as RectTransform);
            Vector3 pb = GetWorldCenter(b.transform as RectTransform);

            // Visual order: top-to-bottom, then left-to-right.
            float dy = pb.y - pa.y;
            if (Mathf.Abs(dy) > 18f)
            {
                return dy > 0f ? 1 : -1;
            }

            float dx = pa.x - pb.x;
            if (Mathf.Abs(dx) > 1f)
            {
                return dx < 0f ? -1 : 1;
            }

            return string.CompareOrdinal(GetHierarchyKey(a.transform), GetHierarchyKey(b.transform));
        }

        private void SetCurrent(Selectable selectable)
        {
            if (selectable == null)
            {
                return;
            }

            current = selectable;
            index = Mathf.Max(0, items.IndexOf(selectable));
            editMode = false;
            editingField = null;

            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            }

            ApplyFocusOutline(new Color(0.10f, 0.92f, 1f, 1f));
            EnsureVisible(selectable.transform as RectTransform);
            UpdateGuide();
        }

        private void ApplyFocusOutline(Color color)
        {
            RemoveFocusOutline();
            if (current == null || current.targetGraphic == null)
            {
                return;
            }

            focusOutline = current.targetGraphic.gameObject.AddComponent<Outline>();
            focusOutline.effectColor = color;
            focusOutline.effectDistance = new Vector2(4f, -4f);
            focusOutline.useGraphicAlpha = false;
        }

        private void RemoveFocusOutline()
        {
            if (focusOutline != null)
            {
                Destroy(focusOutline);
                focusOutline = null;
            }
        }

        private static void EnsureVisible(RectTransform target)
        {
            if (target == null)
            {
                return;
            }

            ScrollRect scroll = target.GetComponentInParent<ScrollRect>();
            if (scroll == null || scroll.content == null)
            {
                return;
            }

            RectTransform viewport = scroll.viewport != null ? scroll.viewport : scroll.transform as RectTransform;
            if (viewport == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, target);
            Rect view = viewport.rect;
            const float margin = 18f;
            float delta = 0f;

            if (bounds.min.y < view.yMin + margin)
            {
                delta = (view.yMin + margin) - bounds.min.y;
            }
            else if (bounds.max.y > view.yMax - margin)
            {
                delta = (view.yMax - margin) - bounds.max.y;
            }

            if (Mathf.Abs(delta) > 0.1f)
            {
                Vector2 position = scroll.content.anchoredPosition;
                position.y += delta;
                scroll.content.anchoredPosition = position;
                Canvas.ForceUpdateCanvases();
            }
        }

        private void BuildGuide()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }
            if (canvas == null)
            {
                return;
            }

            guideRoot = new GameObject("Three Button Cabinet Help", typeof(RectTransform), typeof(Image));
            guideRoot.transform.SetParent(canvas.transform, false);
            guideRoot.transform.SetAsLastSibling();
            RectTransform rect = (RectTransform)guideRoot.transform;
            rect.anchorMin = new Vector2(0.03f, 0.006f);
            rect.anchorMax = new Vector2(0.97f, 0.052f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = guideRoot.GetComponent<Image>();
            image.color = new Color(0.01f, 0.045f, 0.10f, 0.95f);
            image.raycastTarget = false;
            Outline outline = guideRoot.AddComponent<Outline>();
            outline.effectColor = new Color(0.05f, 0.85f, 1f, 0.95f);
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = false;

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(guideRoot.transform, false);
            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 2f);
            textRect.offsetMax = new Vector2(-14f, -2f);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 19f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 11f;
            text.fontSizeMax = 19f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            guideText = text;
            UpdateGuide();
        }

        private void UpdateGuide()
        {
            if (guideText == null)
            {
                return;
            }

            if (current == null)
            {
                guideText.text = "LEFT / RIGHT: MOVE     POP: SELECT     KEY SWITCH: EXIT";
                return;
            }

            string name = GetFriendlyName(current);
            if (editMode && editingField != null)
            {
                guideText.text = $"EDITING: {name} = {editingField.text}     LEFT/RIGHT: ADJUST     POP: CONFIRM     KEY SWITCH: EXIT";
                guideText.color = new Color(1f, 0.84f, 0.20f, 1f);
            }
            else
            {
                guideText.text = $"SELECTED: {name}     LEFT/RIGHT: MOVE     POP: SELECT / EDIT     KEY SWITCH: EXIT";
                guideText.color = new Color(0.70f, 0.96f, 1f, 1f);
            }
        }

        private static bool IsDiagnosticsPanelOpen()
        {
            Transform panel = FindDiagnosticsPanel();
            return panel != null && panel.gameObject.activeInHierarchy;
        }

        private static Transform FindDiagnosticsPanel()
        {
            GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null && string.Equals(objects[i].name, "Diagnostics Panel", StringComparison.Ordinal))
                {
                    return objects[i].transform;
                }
            }
            return null;
        }

        private static string GetFriendlyName(Selectable selectable)
        {
            if (selectable == null)
            {
                return "CONTROL";
            }

            if (selectable is TMP_InputField || selectable is Toggle)
            {
                Transform parent = selectable.transform.parent;
                if (parent != null && !IsGenericName(parent.name))
                {
                    return parent.name;
                }
            }

            TMP_Text[] texts = selectable.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && !string.IsNullOrWhiteSpace(texts[i].text))
                {
                    return texts[i].text.Replace("\n", " ").Trim();
                }
            }

            if (!IsGenericName(selectable.name))
            {
                return selectable.name;
            }

            return selectable.transform.parent != null ? selectable.transform.parent.name : "CONTROL";
        }

        private static bool IsGenericName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            string lower = value.Trim().ToLowerInvariant();
            return lower == "input" || lower == "button" || lower == "toggle" || lower == "selectable";
        }

        private static Vector3 GetWorldCenter(RectTransform rect)
        {
            if (rect == null)
            {
                return Vector3.zero;
            }

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f;
        }

        private static string GetHierarchyKey(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            Stack<int> indices = new Stack<int>();
            Transform cursor = transform;
            while (cursor != null)
            {
                indices.Push(cursor.GetSiblingIndex());
                cursor = cursor.parent;
            }

            return string.Join(".", indices);
        }
    }
}
