using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// Balloon Rush v1.8.8 final presentation polish.
    ///
    /// Presentation-only runtime safeguards:
    /// - makes Operator Menu rows readable at portrait resolution
    /// - simplifies the narrow Attract HOW TO PLAY rail
    /// - suppresses a known leftover diagnostic text if it exists in-game
    ///
    /// Does not alter payout, balance, input, ticket hardware, or game rules.
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class BalloonRushFinalPolishV188 : MonoBehaviour
    {
        private float nextDiagnosticSweep;
        private bool operatorLayoutApplied;
        private bool attractPolishApplied;

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
            string sceneName =
                SceneManager.GetActiveScene().name;

            if (!string.Equals(
                    sceneName,
                    "OperatorMenu",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    sceneName,
                    "AttractMode",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    sceneName,
                    "MainGame",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    sceneName,
                    "Results",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Canvas target = FindBestCanvas(sceneName);

            if (target != null &&
                target.GetComponent<
                    BalloonRushFinalPolishV188>() == null)
            {
                target.gameObject.AddComponent<
                    BalloonRushFinalPolishV188>();
            }
        }

        private static Canvas FindBestCanvas(
            string sceneName)
        {
            Canvas[] canvases =
                FindObjectsByType<Canvas>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (canvases == null ||
                canvases.Length == 0)
            {
                return null;
            }

            string preferred =
                string.Equals(
                    sceneName,
                    "OperatorMenu",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Operator"
                    : string.Equals(
                        sceneName,
                        "MainGame",
                        StringComparison.OrdinalIgnoreCase)
                        ? "Gameplay"
                        : string.Equals(
                            sceneName,
                            "Results",
                            StringComparison.OrdinalIgnoreCase)
                            ? "Results"
                            : "Attract";

            foreach (Canvas canvas in canvases)
            {
                if (canvas != null &&
                    canvas.name.IndexOf(
                        preferred,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return canvas;
                }
            }

            return canvases[0];
        }

        private IEnumerator Start()
        {
            // Operator rows are generated at runtime after services initialize.
            // Give the manager time to populate the scroll content.
            float elapsed = 0f;

            while (elapsed < 3f)
            {
                ApplyScenePolish();

                if (operatorLayoutApplied ||
                    attractPolishApplied)
                {
                    break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            ApplyScenePolish();
            HideKnownDiagnosticText();
        }

        private void Update()
        {
            if (Time.unscaledTime >=
                nextDiagnosticSweep)
            {
                nextDiagnosticSweep =
                    Time.unscaledTime + 1.0f;

                HideKnownDiagnosticText();

                if (!operatorLayoutApplied ||
                    !attractPolishApplied)
                {
                    ApplyScenePolish();
                }
            }
        }

        private void ApplyScenePolish()
        {
            string sceneName =
                SceneManager.GetActiveScene().name;

            if (string.Equals(
                    sceneName,
                    "OperatorMenu",
                    StringComparison.OrdinalIgnoreCase))
            {
                ApplyOperatorLayout();
                return;
            }

            if (string.Equals(
                    sceneName,
                    "AttractMode",
                    StringComparison.OrdinalIgnoreCase))
            {
                ApplyAttractTweaks();
            }
        }

        private void ApplyOperatorLayout()
        {
            Transform settingsPanel =
                FindDeepChild(
                    transform,
                    "Settings Panel");

            if (settingsPanel == null)
                return;

            RectTransform settingsRect =
                settingsPanel as RectTransform;

            if (settingsRect != null)
            {
                // A small amount of extra usable space without shrinking text.
                settingsRect.anchorMin =
                    new Vector2(
                        0.015f,
                        0.238f);

                settingsRect.anchorMax =
                    new Vector2(
                        0.985f,
                        0.838f);

                settingsRect.offsetMin =
                    Vector2.zero;

                settingsRect.offsetMax =
                    Vector2.zero;
            }

            Transform viewport =
                FindDeepChild(
                    settingsPanel,
                    "Viewport");

            if (viewport is RectTransform viewportRect)
            {
                viewportRect.anchorMin =
                    Vector2.zero;

                viewportRect.anchorMax =
                    Vector2.one;

                viewportRect.offsetMin =
                    new Vector2(
                        8f,
                        8f);

                viewportRect.offsetMax =
                    new Vector2(
                        -16f,
                        -8f);
            }

            Transform contentTransform =
                viewport != null
                    ? FindDeepChild(
                        viewport,
                        "Content")
                    : null;

            RectTransform content =
                contentTransform as RectTransform;

            if (content == null ||
                content.childCount == 0)
            {
                return;
            }

            int styledRows = 0;

            for (int i = 0;
                 i < content.childCount;
                 i++)
            {
                Transform row =
                    content.GetChild(i);

                if (row == null)
                    continue;

                string rowName =
                    row.name ?? string.Empty;

                LayoutElement rowLayout =
                    row.GetComponent<
                        LayoutElement>();

                bool isHeader =
                    rowName.StartsWith(
                        "Header -",
                        StringComparison.OrdinalIgnoreCase);

                bool isInfo =
                    rowName.StartsWith(
                        "Info -",
                        StringComparison.OrdinalIgnoreCase);

                if (isHeader)
                {
                    if (rowLayout != null)
                    {
                        rowLayout.preferredHeight = 52f;
                        rowLayout.minHeight = 52f;
                    }

                    TMP_Text header =
                        GetDirectChildText(row);

                    if (header != null)
                    {
                        header.enableAutoSizing = true;
                        header.fontSizeMin = 16f;
                        header.fontSizeMax = 26f;
                        header.textWrappingMode =
                            TextWrappingModes.NoWrap;
                    }

                    styledRows++;
                    continue;
                }

                if (isInfo)
                {
                    if (rowLayout != null)
                    {
                        rowLayout.preferredHeight = 92f;
                        rowLayout.minHeight = 92f;
                    }

                    TMP_Text[] infoTexts =
                        row.GetComponentsInChildren<
                            TMP_Text>(true);

                    foreach (TMP_Text text in infoTexts)
                    {
                        if (text == null)
                            continue;

                        text.enableAutoSizing = true;
                        text.fontSizeMin = 12f;
                        text.fontSizeMax = 19f;
                        text.textWrappingMode =
                            TextWrappingModes.Normal;
                        text.overflowMode =
                            TextOverflowModes.Overflow;
                    }

                    styledRows++;
                    continue;
                }

                if (rowLayout != null)
                {
                    // Two-line names now have enough vertical room.
                    rowLayout.preferredHeight = 76f;
                    rowLayout.minHeight = 76f;
                }

                HorizontalLayoutGroup horizontal =
                    row.GetComponent<
                        HorizontalLayoutGroup>();

                if (horizontal != null)
                {
                    horizontal.padding =
                        new RectOffset(
                            12,
                            12,
                            7,
                            7);

                    horizontal.spacing = 10f;
                    horizontal.childAlignment =
                        TextAnchor.MiddleCenter;
                    horizontal.childControlWidth = true;
                    horizontal.childControlHeight = true;

                    // Critical fix: do not force the value box to expand
                    // and steal space from the setting name.
                    horizontal.childForceExpandWidth = false;
                    horizontal.childForceExpandHeight = false;
                }

                TMP_Text label =
                    GetDirectChildText(row);

                if (label != null)
                {
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 12f;
                    label.fontSizeMax = 20f;
                    label.textWrappingMode =
                        TextWrappingModes.Normal;
                    label.overflowMode =
                        TextOverflowModes.Overflow;
                    label.alignment =
                        TextAlignmentOptions.MidlineLeft;

                    LayoutElement labelLayout =
                        label.GetComponent<
                            LayoutElement>();

                    if (labelLayout == null)
                    {
                        labelLayout =
                            label.gameObject.AddComponent<
                                LayoutElement>();
                    }

                    labelLayout.minWidth = 0f;
                    labelLayout.preferredWidth = 0f;
                    labelLayout.flexibleWidth = 1f;
                }

                TMP_InputField input =
                    GetDirectChildInput(row);

                if (input != null)
                {
                    LayoutElement inputLayout =
                        input.GetComponent<
                            LayoutElement>();

                    if (inputLayout == null)
                    {
                        inputLayout =
                            input.gameObject.AddComponent<
                                LayoutElement>();
                    }

                    inputLayout.minWidth = 145f;
                    inputLayout.preferredWidth = 170f;
                    inputLayout.flexibleWidth = 0f;
                    input.pointSize = 22f;

                    if (input.textComponent != null)
                    {
                        input.textComponent.fontSize = 22f;
                        input.textComponent.enableAutoSizing = true;
                        input.textComponent.fontSizeMin = 15f;
                        input.textComponent.fontSizeMax = 22f;
                        input.textComponent.textWrappingMode =
                            TextWrappingModes.NoWrap;
                    }
                }

                Toggle toggle =
                    GetDirectChildToggle(row);

                if (toggle != null)
                {
                    LayoutElement toggleLayout =
                        toggle.GetComponent<
                            LayoutElement>();

                    if (toggleLayout == null)
                    {
                        toggleLayout =
                            toggle.gameObject.AddComponent<
                                LayoutElement>();
                    }

                    toggleLayout.minWidth = 68f;
                    toggleLayout.preferredWidth = 72f;
                    toggleLayout.flexibleWidth = 0f;
                    toggleLayout.preferredHeight = 44f;
                }

                styledRows++;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(
                content);

            operatorLayoutApplied =
                styledRows > 0;
        }

        private void ApplyAttractTweaks()
        {
            Transform overlay =
                FindDeepChild(
                    transform,
                    "BR187_AttractOverlay");

            if (overlay == null)
                return;

            Transform howTitleTransform =
                FindDeepChild(
                    overlay,
                    "HowTitle");

            TMP_Text howTitle =
                howTitleTransform != null
                    ? howTitleTransform.GetComponent<
                        TMP_Text>()
                    : null;

            if (howTitle != null)
            {
                // Two lines instead of three gives this narrow rail
                // noticeably larger, easier-to-read type.
                howTitle.text =
                    "HOW TO\nPLAY";

                howTitle.enableAutoSizing = true;
                howTitle.fontSizeMin = 12f;
                howTitle.fontSizeMax = 19f;
                howTitle.textWrappingMode =
                    TextWrappingModes.NoWrap;

                RectTransform rt =
                    howTitle.rectTransform;

                rt.anchorMin =
                    new Vector2(
                        0.06f,
                        0.72f);

                rt.anchorMax =
                    new Vector2(
                        0.94f,
                        0.97f);

                rt.offsetMin =
                    Vector2.zero;

                rt.offsetMax =
                    Vector2.zero;
            }

            Transform stepsTransform =
                FindDeepChild(
                    overlay,
                    "Steps");

            TMP_Text steps =
                stepsTransform != null
                    ? stepsTransform.GetComponent<
                        TMP_Text>()
                    : null;

            if (steps != null)
            {
                // Same lesson, less tiny copy.
                steps.text =
                    "1\nSELECT\n\n2\nWAIT\n\n3\nPOP!";

                steps.enableAutoSizing = true;
                steps.fontSizeMin = 11f;
                steps.fontSizeMax = 16f;
                steps.textWrappingMode =
                    TextWrappingModes.NoWrap;

                RectTransform rt =
                    steps.rectTransform;

                rt.anchorMin =
                    new Vector2(
                        0.06f,
                        0.07f);

                rt.anchorMax =
                    new Vector2(
                        0.94f,
                        0.72f);

                rt.offsetMin =
                    Vector2.zero;

                rt.offsetMax =
                    Vector2.zero;
            }

            Transform priceTransform =
                FindDeepChild(
                    overlay,
                    "Price");

            TMP_Text price =
                priceTransform != null
                    ? priceTransform.GetComponent<
                        TMP_Text>()
                    : null;

            if (price != null)
            {
                price.fontSizeMin = 10f;
                price.fontSizeMax = 14f;
            }

            attractPolishApplied = true;
        }

        private void HideKnownDiagnosticText()
        {
            TMP_Text[] tmpTexts =
                FindObjectsByType<TMP_Text>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (TMP_Text text in tmpTexts)
            {
                if (text == null ||
                    string.IsNullOrWhiteSpace(
                        text.text))
                {
                    continue;
                }

                if (LooksLikeLeftoverDiagnostic(
                        text.text))
                {
                    text.gameObject.SetActive(false);
                }
            }

            Text[] legacyTexts =
                FindObjectsByType<Text>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (Text text in legacyTexts)
            {
                if (text == null ||
                    string.IsNullOrWhiteSpace(
                        text.text))
                {
                    continue;
                }

                if (LooksLikeLeftoverDiagnostic(
                        text.text))
                {
                    text.gameObject.SetActive(false);
                }
            }
        }

        private static bool LooksLikeLeftoverDiagnostic(
            string value)
        {
            string normalized =
                value.ToLowerInvariant();

            return
                normalized.Contains(
                    "jackpot and ui are connected") ||
                normalized.Contains(
                    "round, jackpot and ui") ||
                normalized.Contains(
                    "round jackpot and ui");
        }

        private static TMP_Text GetDirectChildText(
            Transform row)
        {
            for (int i = 0;
                 i < row.childCount;
                 i++)
            {
                TMP_Text text =
                    row.GetChild(i)
                        .GetComponent<
                            TMP_Text>();

                if (text != null)
                    return text;
            }

            return null;
        }

        private static TMP_InputField GetDirectChildInput(
            Transform row)
        {
            for (int i = 0;
                 i < row.childCount;
                 i++)
            {
                TMP_InputField input =
                    row.GetChild(i)
                        .GetComponent<
                            TMP_InputField>();

                if (input != null)
                    return input;
            }

            return null;
        }

        private static Toggle GetDirectChildToggle(
            Transform row)
        {
            for (int i = 0;
                 i < row.childCount;
                 i++)
            {
                Toggle toggle =
                    row.GetChild(i)
                        .GetComponent<
                            Toggle>();

                if (toggle != null)
                    return toggle;
            }

            return null;
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
