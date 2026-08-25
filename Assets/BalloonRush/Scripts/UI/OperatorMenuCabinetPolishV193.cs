using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// Cabinet-focused cleanup for the existing Operator Menu.
    ///
    /// This does NOT replace OperatorMenuManager. It fixes the runtime layout,
    /// footer, diagnostics presentation, and cabinet-facing help text after the
    /// existing menu has finished building itself.
    /// </summary>
    [DefaultExecutionOrder(1500)]
    public sealed class OperatorMenuCabinetPolishV193 : MonoBehaviour
    {
        public const string Version = "1.9.3";

        private ScrollRect settingsScroll;
        private RectTransform settingsContent;
        private bool appliedInitial;
        private float nextMaintenance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!string.Equals(scene.name, BalloonRush.Core.GameBootstrap.OperatorSceneName, StringComparison.Ordinal))
            {
                return;
            }

            if (FindFirstObjectByType<OperatorMenuCabinetPolishV193>(FindObjectsInactive.Include) != null)
            {
                return;
            }

            GameObject host = new GameObject("Balloon Rush Operator Menu Polish v1.9.3");
            host.AddComponent<OperatorMenuCabinetPolishV193>();
        }

        private IEnumerator Start()
        {
            // OperatorMenuManager builds its rows during Start(), diagnostics builds
            // separately, and the three-button navigator waits a couple frames.
            // Give all three systems time to exist before correcting layout.
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();

            ApplyAll(true);

            yield return new WaitForSecondsRealtime(0.15f);
            ApplyAll(false);

            yield return new WaitForSecondsRealtime(0.35f);
            ApplyAll(false);

            appliedInitial = true;
        }

        private void Update()
        {
            if (!appliedInitial || Time.unscaledTime < nextMaintenance)
            {
                return;
            }

            nextMaintenance = Time.unscaledTime + 0.75f;

            // Reassert only the safe structural pieces. This catches menu rebuilds
            // or diagnostics being constructed slightly later on a slower cabinet.
            FindSettingsScroll();
            FixSettingsLayout(false);
            FixDiagnosticsPage(false);
        }

        private void ApplyAll(bool resetScrollToTop)
        {
            FindSettingsScroll();
            FixSettingsLayout(resetScrollToTop);
            FixFooter();
            FixCabinetHelpText();
            FixDiagnosticsPage(true);
        }

        private void FindSettingsScroll()
        {
            ScrollRect[] scrolls = FindObjectsByType<ScrollRect>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            settingsScroll = null;
            settingsContent = null;

            for (int i = 0; i < scrolls.Length; i++)
            {
                ScrollRect candidate = scrolls[i];
                if (candidate == null || candidate.content == null)
                {
                    continue;
                }

                string parentName = candidate.transform.parent != null
                    ? candidate.transform.parent.name
                    : string.Empty;

                bool likelySettings =
                    string.Equals(candidate.content.name, "Content", StringComparison.OrdinalIgnoreCase) &&
                    (candidate.name.IndexOf("setting", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     parentName.IndexOf("setting", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     candidate.transform.root.name.IndexOf("operator", StringComparison.OrdinalIgnoreCase) >= 0);

                if (!likelySettings)
                {
                    continue;
                }

                settingsScroll = candidate;
                settingsContent = candidate.content;
                return;
            }

            // Defensive fallback: Operator Menu currently has one ScrollRect.
            if (scrolls.Length == 1 && scrolls[0] != null)
            {
                settingsScroll = scrolls[0];
                settingsContent = scrolls[0].content;
            }
        }

        private void FixSettingsLayout(bool resetScrollToTop)
        {
            if (settingsScroll == null || settingsContent == null)
            {
                return;
            }

            // Content must be top-anchored and stretch horizontally.
            settingsContent.anchorMin = new Vector2(0f, 1f);
            settingsContent.anchorMax = new Vector2(1f, 1f);
            settingsContent.pivot = new Vector2(0.5f, 1f);
            settingsContent.anchoredPosition = new Vector2(0f, settingsContent.anchoredPosition.y);
            settingsContent.sizeDelta = new Vector2(0f, settingsContent.sizeDelta.y);

            VerticalLayoutGroup layout = settingsContent.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = settingsContent.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = new RectOffset(10, 10, 10, 12);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childScaleWidth = false;
            layout.childScaleHeight = false;

            ContentSizeFitter fitter = settingsContent.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = settingsContent.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            settingsScroll.horizontal = false;
            settingsScroll.vertical = true;
            settingsScroll.movementType = ScrollRect.MovementType.Clamped;
            settingsScroll.inertia = false;
            settingsScroll.scrollSensitivity = 70f;
            settingsScroll.content = settingsContent;

            RectTransform viewport = settingsScroll.viewport != null
                ? settingsScroll.viewport
                : settingsScroll.transform as RectTransform;

            if (viewport != null)
            {
                settingsScroll.viewport = viewport;

                if (viewport.GetComponent<RectMask2D>() == null &&
                    viewport.GetComponent<Mask>() == null)
                {
                    viewport.gameObject.AddComponent<RectMask2D>();
                }
            }

            // Normalize dynamically created rows so none collapse to a few pixels.
            for (int i = 0; i < settingsContent.childCount; i++)
            {
                RectTransform row = settingsContent.GetChild(i) as RectTransform;
                if (row == null)
                {
                    continue;
                }

                LayoutElement element = row.GetComponent<LayoutElement>();
                if (element == null)
                {
                    element = row.gameObject.AddComponent<LayoutElement>();
                }

                string rowName = row.name ?? string.Empty;
                bool header = rowName.StartsWith("Header - ", StringComparison.OrdinalIgnoreCase);
                bool info = rowName.StartsWith("Info - ", StringComparison.OrdinalIgnoreCase);

                float preferred = header ? 58f : (info ? 94f : 64f);
                if (element.preferredHeight <= 8f)
                {
                    element.preferredHeight = preferred;
                }
                element.minHeight = Mathf.Max(element.minHeight, preferred);
                element.flexibleHeight = 0f;

                // The old stacked shadows become a large black blob when layout is
                // delayed. Keep the outline subtle even during a rebuild frame.
                Outline[] outlines = row.GetComponents<Outline>();
                for (int o = 0; o < outlines.Length; o++)
                {
                    Outline outline = outlines[o];
                    if (outline == null)
                    {
                        continue;
                    }

                    Color c = outline.effectColor;
                    c.a = Mathf.Min(c.a, 0.32f);
                    outline.effectColor = c;
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(settingsContent);
            Canvas.ForceUpdateCanvases();

            if (resetScrollToTop)
            {
                settingsContent.anchoredPosition = Vector2.zero;
                settingsScroll.verticalNormalizedPosition = 1f;
            }
        }

        private void FixFooter()
        {
            Button save = FindButton("SAVE Button");
            Button defaults = FindButton("RESET DEFAULTS Button");
            Button testInputs = FindButton("TEST INPUTS Button");
            Button testTickets = FindButton("TEST TICKETS Button");
            Button resetStats = FindButton("RESET STATISTICS Button");
            Button back = FindButton("BACK Button");

            // Diagnostics now provides a better live input test. Hiding this avoids
            // a duplicate service mode and frees space for RESET STATS.
            if (testInputs != null)
            {
                testInputs.gameObject.SetActive(false);
            }

            Button[] visible = { save, defaults, testTickets, resetStats, back };
            float[] mins = { 0.01f, 0.205f, 0.400f, 0.595f, 0.790f };
            float[] maxs = { 0.190f, 0.385f, 0.580f, 0.775f, 0.990f };

            for (int i = 0; i < visible.Length; i++)
            {
                Button button = visible[i];
                if (button == null)
                {
                    continue;
                }

                RectTransform rect = button.transform as RectTransform;
                if (rect == null)
                {
                    continue;
                }

                rect.anchorMin = new Vector2(mins[i], 0.50f);
                rect.anchorMax = new Vector2(maxs[i], 0.95f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    if (button == resetStats)
                    {
                        label.text = "RESET\nSTATS";
                    }
                    else if (button == defaults)
                    {
                        label.text = "RESET\nDEFAULTS";
                    }
                    else if (button == testTickets)
                    {
                        label.text = "TEST\nTICKETS";
                    }

                    label.enableAutoSizing = true;
                    label.fontSizeMin = 11f;
                    label.fontSizeMax = 22f;
                }
            }
        }

        private void FixCabinetHelpText()
        {
            TMP_Text[] texts = FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (text == null || string.IsNullOrWhiteSpace(text.text))
                {
                    continue;
                }

                string value = text.text;

                if (value.IndexOf("M OR ESC", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    value.IndexOf("RETURN TO ATTRACT", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    text.text = "KEY SWITCH (M / JOY4) - RETURN TO ATTRACT     |     CHANGES APPLY AFTER SAVE";
                    text.enableAutoSizing = true;
                    text.fontSizeMin = 12f;
                }

                if (value.IndexOf("C CREDIT", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    value.IndexOf("UP/SPACE POP", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    text.text =
                        "CABINET: JOY1 LEFT  |  JOY2 POP/SELECT  |  JOY7 RIGHT  |  JOY4 KEY SWITCH\n" +
                        "KEYBOARD: LEFT/RIGHT  |  UP/SPACE POP  |  M OPERATOR  |  ESC BACK";
                    text.enableAutoSizing = true;
                    text.fontSizeMin = 12f;
                    text.fontSizeMax = 20f;
                }
            }
        }

        private void FixDiagnosticsPage(bool closeOnInitialSetup)
        {
            GameObject panel = FindObjectNamed("Diagnostics Panel");
            GameObject openButtonObject = FindObjectNamed("CABINET DIAGNOSTICS");

            if (panel == null)
            {
                return;
            }

            RectTransform panelRect = panel.transform as RectTransform;
            if (panelRect != null)
            {
                // Treat diagnostics as its own page, not a translucent floating card.
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
            }

            Image image = panel.GetComponent<Image>();
            if (image != null)
            {
                // Keep the panel's existing sprite. The cleanup only needs
                // an opaque background color; avoiding RuntimeSpriteLibrary
                // keeps this patch independent of optional visual helpers.
                image.type = Image.Type.Simple;
                image.color = new Color(0.005f, 0.025f, 0.065f, 1f);

                Outline outline = panel.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = panel.AddComponent<Outline>();
                }
                outline.effectColor = new Color(0.05f, 0.88f, 1f, 0.95f);
                outline.effectDistance = new Vector2(3f, -3f);
                outline.useGraphicAlpha = false;
            }

            if (closeOnInitialSetup && panel.activeSelf)
            {
                panel.SetActive(false);
                if (openButtonObject != null)
                {
                    openButtonObject.SetActive(true);
                }
            }
        }

        private static Button FindButton(string exactName)
        {
            GameObject obj = FindObjectNamed(exactName);
            return obj != null ? obj.GetComponent<Button>() : null;
        }

        private static GameObject FindObjectNamed(string exactName)
        {
            GameObject[] objects = FindObjectsByType<GameObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (obj != null && string.Equals(obj.name, exactName, StringComparison.Ordinal))
                {
                    return obj;
                }
            }

            return null;
        }
    }
}
