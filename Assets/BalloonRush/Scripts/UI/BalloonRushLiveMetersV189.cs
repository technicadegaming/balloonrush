using System;
using System.Collections;
using System.Collections.Generic;
using BalloonRush.Core;
using BalloonRush.Gameplay;
using BalloonRush.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// Balloon Rush v1.8.9 live meter pass.
    ///
    /// COMBO:
    /// - converts the almost-invisible 0..30 fill into progress toward the
    ///   NEXT combo milestone (5 / 10 / 15 / 20 / 30)
    /// - adds a separate chain-time bar using ComboManager timeout data
    /// - clearly says what the next milestone is and how many hits remain
    ///
    /// PAYOUT:
    /// - turns the previously static 500/250/100/50/25/10/5/1 ladder into
    ///   live ticket progress
    /// - reached tiers light up
    /// - the next tier pulses and says NEXT
    /// - a vertical progress bar moves through the displayed payout tiers
    ///
    /// Presentation only. No score, ticket, payout, combo, timing or hardware
    /// rules are modified.
    /// </summary>
    [DefaultExecutionOrder(760)]
    public sealed class BalloonRushLiveMetersV189 : MonoBehaviour
    {
        private static readonly int[] ComboMilestones =
        {
            5, 10, 15, 20, 30
        };

        // Ascending version of the visible payout ladder.
        private static readonly int[] PayoutThresholds =
        {
            0, 1, 5, 10, 25, 50, 100, 250, 500
        };

        private static readonly Color Cyan =
            new Color32(0, 229, 255, 255);

        private static readonly Color Blue =
            new Color32(40, 132, 255, 255);

        private static readonly Color Green =
            new Color32(45, 224, 91, 255);

        private static readonly Color Gold =
            new Color32(255, 193, 28, 255);

        private static readonly Color Orange =
            new Color32(255, 123, 27, 255);

        private static readonly Color Purple =
            new Color32(165, 76, 255, 255);

        private Canvas canvas;
        private ComboManager comboManager;
        private ScoreManager scoreManager;

        private Image comboFill;
        private TMP_Text comboSubtext;
        private TMP_Text comboValue;

        private RectTransform comboTimeoutTrack;
        private Image comboTimeoutFill;

        private RectTransform payoutRail;
        private Image payoutProgressFill;
        private TMP_Text payoutTitle;

        private readonly Dictionary<int, Image> payoutTiles =
            new Dictionary<int, Image>();

        private readonly Dictionary<int, TMP_Text> payoutLabels =
            new Dictionary<int, TMP_Text>();

        private readonly Dictionary<int, Color> payoutBaseColors =
            new Dictionary<int, Color>();

        private Sprite smallRoundedSprite;

        private int currentCombo;
        private int currentTickets;
        private float targetComboProgress;
        private float shownComboProgress;
        private float targetPayoutProgress;
        private float shownPayoutProgress;
        private float comboPulse;
        private float payoutPulse;
        private bool built;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookSceneLoad()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallCurrentScene()
        {
            TryInstall();
        }

        private static void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            TryInstall();
        }

        private static void TryInstall()
        {
            if (!string.Equals(
                    SceneManager.GetActiveScene().name,
                    "MainGame",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Canvas[] canvases =
                FindObjectsByType<Canvas>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (Canvas candidate in canvases)
            {
                if (candidate == null)
                    continue;

                if (candidate.name.IndexOf(
                        "Gameplay",
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (candidate.GetComponent<
                        BalloonRushLiveMetersV189>() == null)
                {
                    candidate.gameObject.AddComponent<
                        BalloonRushLiveMetersV189>();
                }

                return;
            }
        }

        private void Awake()
        {
            canvas = GetComponent<Canvas>();

            smallRoundedSprite =
                RoundedSpriteFactory.CreateRoundedPanelSprite(
                    Color.white,
                    Color.white,
                    96,
                    32,
                    2,
                    28);
        }

        private IEnumerator Start()
        {
            // BalloonRushMainGameVisualRebuild builds its runtime HUD during
            // Awake. Wait briefly in case script ordering/import timing causes
            // the named objects to appear a frame later.
            float timeout = 2.5f;

            while (!TryBuild() &&
                   timeout > 0f)
            {
                timeout -=
                    Time.unscaledDeltaTime;

                yield return null;
            }

            comboManager =
                FindFirstObjectByType<ComboManager>(
                    FindObjectsInactive.Include);

            scoreManager =
                FindFirstObjectByType<ScoreManager>(
                    FindObjectsInactive.Include);

            GameEvents.ComboChanged -= HandleComboChanged;
            GameEvents.ComboChanged += HandleComboChanged;

            GameEvents.TicketsChanged -= HandleTicketsChanged;
            GameEvents.TicketsChanged += HandleTicketsChanged;

            HandleComboChanged(
                comboManager != null
                    ? comboManager.CurrentCombo
                    : 0);

            HandleTicketsChanged(
                scoreManager != null
                    ? scoreManager.Tickets
                    : 0);
        }

        private void OnDestroy()
        {
            GameEvents.ComboChanged -= HandleComboChanged;
            GameEvents.TicketsChanged -= HandleTicketsChanged;
        }

        private void Update()
        {
            if (!built)
            {
                TryBuild();
                return;
            }

            shownComboProgress =
                Mathf.MoveTowards(
                    shownComboProgress,
                    targetComboProgress,
                    Time.unscaledDeltaTime * 2.8f);

            shownPayoutProgress =
                Mathf.MoveTowards(
                    shownPayoutProgress,
                    targetPayoutProgress,
                    Time.unscaledDeltaTime * 1.9f);

            comboPulse =
                Mathf.MoveTowards(
                    comboPulse,
                    0f,
                    Time.unscaledDeltaTime * 3.7f);

            payoutPulse =
                Mathf.MoveTowards(
                    payoutPulse,
                    0f,
                    Time.unscaledDeltaTime * 3.0f);

            UpdateComboAnimation();
            UpdatePayoutAnimation();
        }

        private bool TryBuild()
        {
            if (built)
                return true;

            if (canvas == null)
                return false;

            Transform comboFillTransform =
                FindDeepChild(
                    canvas.transform,
                    "BRUI_ComboFill");

            Transform comboSubtextTransform =
                FindDeepChild(
                    canvas.transform,
                    "BRUI_ComboSubtext");

            Transform comboValueTransform =
                FindDeepChild(
                    canvas.transform,
                    "BRUI_ComboValue");

            Transform comboTrackTransform =
                FindDeepChild(
                    canvas.transform,
                    "BRUI_ComboTrack");

            Transform payoutRailTransform =
                FindDeepChild(
                    canvas.transform,
                    "BRUI_PayoutRail");

            Transform payoutTitleTransform =
                FindDeepChild(
                    canvas.transform,
                    "BRUI_PayoutTitle");

            if (comboFillTransform == null ||
                comboTrackTransform == null ||
                payoutRailTransform == null)
            {
                return false;
            }

            comboFill =
                comboFillTransform.GetComponent<Image>();

            comboSubtext =
                comboSubtextTransform != null
                    ? comboSubtextTransform.GetComponent<TMP_Text>()
                    : null;

            comboValue =
                comboValueTransform != null
                    ? comboValueTransform.GetComponent<TMP_Text>()
                    : null;

            payoutRail =
                payoutRailTransform as RectTransform;

            payoutTitle =
                payoutTitleTransform != null
                    ? payoutTitleTransform.GetComponent<TMP_Text>()
                    : null;

            if (comboFill == null ||
                payoutRail == null)
            {
                return false;
            }

            PrepareComboMeter(
                comboTrackTransform as RectTransform);

            PreparePayoutMeter();

            built = true;
            return true;
        }

        private void PrepareComboMeter(
            RectTransform comboTrack)
        {
            if (comboTrack == null ||
                comboFill == null)
            {
                return;
            }

            // Make milestone progress much wider and more visible.
            RectTransform fillRect =
                comboFill.rectTransform;

            fillRect.anchorMin =
                new Vector2(
                    0.13f,
                    0.03f);

            fillRect.anchorMax =
                new Vector2(
                    0.70f,
                    0.03f);

            fillRect.pivot =
                new Vector2(
                    0.5f,
                    0f);

            fillRect.offsetMin =
                Vector2.zero;

            fillRect.offsetMax =
                Vector2.zero;

            comboFill.type =
                Image.Type.Sliced;

            Transform old =
                comboTrack.Find(
                    "BR189_ComboTimeoutTrack");

            if (old != null)
            {
                Destroy(old.gameObject);
            }

            GameObject trackObject =
                new GameObject(
                    "BR189_ComboTimeoutTrack",
                    typeof(RectTransform),
                    typeof(Image));

            comboTimeoutTrack =
                trackObject.GetComponent<RectTransform>();

            comboTimeoutTrack.SetParent(
                comboTrack,
                false);

            comboTimeoutTrack.anchorMin =
                new Vector2(
                    0.77f,
                    0.03f);

            comboTimeoutTrack.anchorMax =
                new Vector2(
                    0.91f,
                    0.97f);

            comboTimeoutTrack.offsetMin =
                Vector2.zero;

            comboTimeoutTrack.offsetMax =
                Vector2.zero;

            Image timeoutBackground =
                trackObject.GetComponent<Image>();

            timeoutBackground.sprite =
                smallRoundedSprite;

            timeoutBackground.type =
                Image.Type.Sliced;

            timeoutBackground.color =
                new Color(
                    0.05f,
                    0.13f,
                    0.25f,
                    0.90f);

            timeoutBackground.raycastTarget =
                false;

            GameObject fillObject =
                new GameObject(
                    "BR189_ComboTimeoutFill",
                    typeof(RectTransform),
                    typeof(Image));

            RectTransform rt =
                fillObject.GetComponent<RectTransform>();

            rt.SetParent(
                comboTimeoutTrack,
                false);

            rt.anchorMin =
                new Vector2(
                    0.18f,
                    0.03f);

            rt.anchorMax =
                new Vector2(
                    0.82f,
                    0.03f);

            rt.pivot =
                new Vector2(
                    0.5f,
                    0f);

            rt.offsetMin =
                Vector2.zero;

            rt.offsetMax =
                Vector2.zero;

            comboTimeoutFill =
                fillObject.GetComponent<Image>();

            comboTimeoutFill.sprite =
                smallRoundedSprite;

            comboTimeoutFill.type =
                Image.Type.Sliced;

            comboTimeoutFill.color =
                Cyan;

            comboTimeoutFill.raycastTarget =
                false;
        }

        private void PreparePayoutMeter()
        {
            Transform oldTrack =
                payoutRail.Find(
                    "BR189_PayoutProgressTrack");

            if (oldTrack != null)
            {
                Destroy(oldTrack.gameObject);
            }

            GameObject trackObject =
                new GameObject(
                    "BR189_PayoutProgressTrack",
                    typeof(RectTransform),
                    typeof(Image));

            RectTransform track =
                trackObject.GetComponent<RectTransform>();

            track.SetParent(
                payoutRail,
                false);

            track.anchorMin =
                new Vector2(
                    0.025f,
                    0.055f);

            track.anchorMax =
                new Vector2(
                    0.105f,
                    0.895f);

            track.offsetMin =
                Vector2.zero;

            track.offsetMax =
                Vector2.zero;

            Image background =
                trackObject.GetComponent<Image>();

            background.sprite =
                smallRoundedSprite;

            background.type =
                Image.Type.Sliced;

            background.color =
                new Color(
                    0.04f,
                    0.11f,
                    0.24f,
                    0.92f);

            background.raycastTarget =
                false;

            track.SetAsFirstSibling();

            GameObject fillObject =
                new GameObject(
                    "BR189_PayoutProgressFill",
                    typeof(RectTransform),
                    typeof(Image));

            RectTransform fill =
                fillObject.GetComponent<RectTransform>();

            fill.SetParent(
                track,
                false);

            fill.anchorMin =
                new Vector2(
                    0.16f,
                    0.02f);

            fill.anchorMax =
                new Vector2(
                    0.84f,
                    0.02f);

            fill.pivot =
                new Vector2(
                    0.5f,
                    0f);

            fill.offsetMin =
                Vector2.zero;

            fill.offsetMax =
                Vector2.zero;

            payoutProgressFill =
                fillObject.GetComponent<Image>();

            payoutProgressFill.sprite =
                smallRoundedSprite;

            payoutProgressFill.type =
                Image.Type.Sliced;

            payoutProgressFill.color =
                Green;

            payoutProgressFill.raycastTarget =
                false;

            int[] values =
            {
                500, 250, 100, 50,
                25, 10, 5, 1
            };

            foreach (int value in values)
            {
                Transform tileTransform =
                    FindDeepChild(
                        payoutRail,
                        "BRUI_Payout_" + value);

                Transform labelTransform =
                    FindDeepChild(
                        payoutRail,
                        "BRUI_PayoutLabel_" + value);

                Image tile =
                    tileTransform != null
                        ? tileTransform.GetComponent<Image>()
                        : null;

                TMP_Text label =
                    labelTransform != null
                        ? labelTransform.GetComponent<TMP_Text>()
                        : null;

                if (tile != null)
                {
                    payoutTiles[value] = tile;
                    payoutBaseColors[value] =
                        tile.color;
                }

                if (label != null)
                {
                    payoutLabels[value] = label;
                    label.enableAutoSizing = true;
                }
            }

            if (payoutTitle != null)
            {
                payoutTitle.enableAutoSizing = true;
                payoutTitle.fontSizeMin = 9f;
                payoutTitle.fontSizeMax = 17f;
                payoutTitle.textWrappingMode =
                    TextWrappingModes.NoWrap;

                RectTransform rt =
                    payoutTitle.rectTransform;

                rt.anchorMin =
                    new Vector2(
                        0.04f,
                        0.900f);

                rt.anchorMax =
                    new Vector2(
                        0.96f,
                        0.993f);

                rt.offsetMin =
                    Vector2.zero;

                rt.offsetMax =
                    Vector2.zero;
            }
        }

        private void HandleComboChanged(int combo)
        {
            currentCombo =
                Mathf.Max(
                    0,
                    combo);

            targetComboProgress =
                GetComboMilestoneProgress(
                    currentCombo);

            comboPulse = 1f;

            RefreshComboCopy();
        }

        private void HandleTicketsChanged(int tickets)
        {
            currentTickets =
                Mathf.Max(
                    0,
                    tickets);

            targetPayoutProgress =
                GetPayoutProgress(
                    currentTickets);

            payoutPulse = 1f;

            RefreshPayoutCopy();
        }

        private void RefreshComboCopy()
        {
            if (comboValue != null)
            {
                comboValue.text =
                    "COMBO\nx" +
                    currentCombo;
            }

            if (comboSubtext == null)
                return;

            if (currentCombo <= 0)
            {
                comboSubtext.text =
                    "BUILD YOUR\nCOMBO!";

                comboSubtext.color =
                    Gold;

                return;
            }

            if (IsComboMilestone(
                    currentCombo))
            {
                comboSubtext.text =
                    "MILESTONE!\nx" +
                    currentCombo;

                comboSubtext.color =
                    currentCombo >= 20
                        ? Gold
                        : Orange;

                return;
            }

            int next =
                GetNextComboMilestone(
                    currentCombo);

            if (next <= currentCombo)
            {
                comboSubtext.text =
                    "MAX COMBO\nBONUS!";

                comboSubtext.color =
                    Gold;

                return;
            }

            comboSubtext.text =
                "NEXT x" +
                next +
                "\n" +
                (next - currentCombo) +
                " TO GO";

            comboSubtext.color =
                currentCombo >= 10
                    ? Orange
                    : Gold;
        }

        private void RefreshPayoutCopy()
        {
            if (payoutTitle != null)
            {
                payoutTitle.text =
                    "PAYOUT\n" +
                    currentTickets +
                    " TIX";
            }

            int nextValue =
                GetNextPayoutThreshold(
                    currentTickets);

            foreach (KeyValuePair<int, TMP_Text> pair
                     in payoutLabels)
            {
                int value = pair.Key;
                TMP_Text label = pair.Value;

                if (label == null)
                    continue;

                if (value == nextValue)
                {
                    label.text =
                        value +
                        "\nNEXT";

                    label.fontSizeMin = 7f;
                    label.fontSizeMax = 13f;
                }
                else
                {
                    label.text =
                        value.ToString();

                    label.fontSizeMin =
                        value == 500
                            ? 11f
                            : 9f;

                    label.fontSizeMax =
                        value == 500
                            ? 22f
                            : 17f;
                }
            }
        }

        private void UpdateComboAnimation()
        {
            if (comboFill != null)
            {
                RectTransform rt =
                    comboFill.rectTransform;

                float top =
                    Mathf.Lerp(
                        0.03f,
                        0.97f,
                        shownComboProgress);

                rt.anchorMax =
                    new Vector2(
                        rt.anchorMax.x,
                        top);

                float timeout =
                    comboManager != null
                        ? comboManager
                            .NormalizedTimeoutRemaining
                        : 0f;

                Color baseColor =
                    Color.Lerp(
                        Blue,
                        Gold,
                        Mathf.Clamp01(
                            currentCombo / 20f));

                float dim =
                    currentCombo > 0
                        ? Mathf.Lerp(
                            0.52f,
                            1f,
                            timeout)
                        : 0.45f;

                comboFill.color =
                    new Color(
                        baseColor.r,
                        baseColor.g,
                        baseColor.b,
                        dim);

                float pulse =
                    1f +
                    comboPulse * 0.055f;

                comboFill.transform.localScale =
                    new Vector3(
                        pulse,
                        1f,
                        1f);
            }

            if (comboTimeoutFill != null)
            {
                float timeout =
                    comboManager != null
                        ? comboManager
                            .NormalizedTimeoutRemaining
                        : 0f;

                RectTransform rt =
                    comboTimeoutFill.rectTransform;

                rt.anchorMax =
                    new Vector2(
                        rt.anchorMax.x,
                        0.03f +
                        0.94f *
                        Mathf.Clamp01(
                            timeout));

                comboTimeoutFill.color =
                    Color.Lerp(
                        Orange,
                        Cyan,
                        timeout);

                comboTimeoutFill.gameObject.SetActive(
                    currentCombo > 0);

                comboTimeoutTrack.gameObject.SetActive(
                    currentCombo > 0);
            }

            // Main visual rebuild also writes combo copy on ComboChanged.
            // Refreshing here guarantees this more informative wording wins.
            if (currentCombo > 0 &&
                comboSubtext != null)
            {
                RefreshComboCopy();
            }
        }

        private void UpdatePayoutAnimation()
        {
            if (payoutProgressFill != null)
            {
                RectTransform rt =
                    payoutProgressFill.rectTransform;

                rt.anchorMax =
                    new Vector2(
                        rt.anchorMax.x,
                        0.02f +
                        0.96f *
                        shownPayoutProgress);

                payoutProgressFill.color =
                    Color.Lerp(
                        Green,
                        Gold,
                        shownPayoutProgress);
            }

            int nextValue =
                GetNextPayoutThreshold(
                    currentTickets);

            float pulse =
                0.5f +
                0.5f *
                Mathf.Sin(
                    Time.unscaledTime *
                    7.0f);

            foreach (KeyValuePair<int, Image> pair
                     in payoutTiles)
            {
                int value = pair.Key;
                Image tile = pair.Value;

                if (tile == null)
                    continue;

                Color baseColor =
                    payoutBaseColors.ContainsKey(value)
                        ? payoutBaseColors[value]
                        : Color.white;

                bool reached =
                    currentTickets >= value;

                bool next =
                    value == nextValue;

                if (reached)
                {
                    tile.color =
                        Color.Lerp(
                            baseColor,
                            Green,
                            value == 500
                                ? 0.22f
                                : 0.34f);

                    tile.color =
                        new Color(
                            tile.color.r,
                            tile.color.g,
                            tile.color.b,
                            1f);

                    tile.transform.localScale =
                        Vector3.one *
                        (1.015f +
                         payoutPulse * 0.025f);
                }
                else if (next)
                {
                    tile.color =
                        Color.Lerp(
                            baseColor,
                            Gold,
                            0.42f +
                            pulse * 0.22f);

                    tile.color =
                        new Color(
                            tile.color.r,
                            tile.color.g,
                            tile.color.b,
                            1f);

                    tile.transform.localScale =
                        Vector3.one *
                        (1.025f +
                         pulse * 0.055f);
                }
                else
                {
                    tile.color =
                        new Color(
                            baseColor.r,
                            baseColor.g,
                            baseColor.b,
                            0.68f);

                    tile.transform.localScale =
                        Vector3.one;
                }

                if (payoutLabels.TryGetValue(
                        value,
                        out TMP_Text label) &&
                    label != null)
                {
                    label.color =
                        reached
                            ? Color.white
                            : next
                                ? Gold
                                : new Color(
                                    0.78f,
                                    0.88f,
                                    1f,
                                    0.78f);
                }
            }
        }

        private static float GetComboMilestoneProgress(
            int combo)
        {
            if (combo <= 0)
                return 0f;

            int previous = 0;

            foreach (int milestone
                     in ComboMilestones)
            {
                if (combo <= milestone)
                {
                    return Mathf.Clamp01(
                        (combo - previous) /
                        (float)(milestone - previous));
                }

                previous = milestone;
            }

            return 1f;
        }

        private static int GetNextComboMilestone(
            int combo)
        {
            foreach (int milestone
                     in ComboMilestones)
            {
                if (milestone > combo)
                    return milestone;
            }

            return ComboMilestones[
                ComboMilestones.Length - 1];
        }

        private static bool IsComboMilestone(
            int combo)
        {
            foreach (int milestone
                     in ComboMilestones)
            {
                if (combo == milestone)
                    return true;
            }

            return false;
        }

        private static float GetPayoutProgress(
            int tickets)
        {
            int safeTickets =
                Mathf.Max(
                    0,
                    tickets);

            int segmentCount =
                PayoutThresholds.Length - 1;

            if (safeTickets >=
                PayoutThresholds[
                    PayoutThresholds.Length - 1])
            {
                return 1f;
            }

            for (int i = 1;
                 i < PayoutThresholds.Length;
                 i++)
            {
                int high =
                    PayoutThresholds[i];

                if (safeTickets > high)
                    continue;

                int low =
                    PayoutThresholds[i - 1];

                float local =
                    high > low
                        ? Mathf.InverseLerp(
                            low,
                            high,
                            safeTickets)
                        : 1f;

                return Mathf.Clamp01(
                    ((i - 1) + local) /
                    segmentCount);
            }

            return 1f;
        }

        private static int GetNextPayoutThreshold(
            int tickets)
        {
            foreach (int value
                     in PayoutThresholds)
            {
                if (value > tickets)
                    return value;
            }

            return -1;
        }

        private static Transform FindDeepChild(
            Transform parent,
            string childName)
        {
            if (parent == null)
                return null;

            Transform[] all =
                parent.GetComponentsInChildren<
                    Transform>(true);

            foreach (Transform t in all)
            {
                if (t != null &&
                    string.Equals(
                        t.name,
                        childName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return t;
                }
            }

            return null;
        }
    }
}
