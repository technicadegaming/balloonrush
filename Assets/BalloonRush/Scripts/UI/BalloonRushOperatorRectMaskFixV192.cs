using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// Balloon Rush v1.9.2 operator menu viewport fix.
    ///
    /// The generated Operator scroll viewport used a rounded Image + Mask.
    /// Unity therefore clipped child rows to the rounded alpha shape, which
    /// visibly chopped the beginning/end of setting names near the top and
    /// bottom of the viewport.
    ///
    /// v1.9.2 replaces that alpha Mask with RectMask2D and normalizes every
    /// generated row to the full viewport width.
    ///
    /// It also removes remaining public-facing M / Operator instructions.
    /// </summary>
    [DefaultExecutionOrder(1200)]
    public sealed class BalloonRushOperatorRectMaskFixV192 : MonoBehaviour
    {
        private string sceneName;
        private float nextSweep;
        private bool operatorFixed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookSceneLoad()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
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
            string active = SceneManager.GetActiveScene().name;

            if (!string.Equals(active, "AttractMode", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(active, "MainGame", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(active, "Results", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(active, "OperatorMenu", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Canvas[] canvases = FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            if (canvases == null || canvases.Length == 0)
                return;

            Canvas target = canvases[0];

            string wanted =
                string.Equals(active, "OperatorMenu", StringComparison.OrdinalIgnoreCase)
                    ? "Operator"
                    : string.Equals(active, "MainGame", StringComparison.OrdinalIgnoreCase)
                        ? "Gameplay"
                        : string.Equals(active, "Results", StringComparison.OrdinalIgnoreCase)
                            ? "Results"
                            : "Attract";

            foreach (Canvas canvas in canvases)
            {
                if (canvas != null &&
                    canvas.name.IndexOf(
                        wanted,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    target = canvas;
                    break;
                }
            }

            if (target != null &&
                target.GetComponent<BalloonRushOperatorRectMaskFixV192>() == null)
            {
                target.gameObject.AddComponent<
                    BalloonRushOperatorRectMaskFixV192>();
            }
        }

        private IEnumerator Start()
        {
            sceneName = SceneManager.GetActiveScene().name;

            float timeout = 4f;

            while (timeout > 0f)
            {
                RemoveOperatorInstructions();

                if (string.Equals(
                        sceneName,
                        "OperatorMenu",
                        StringComparison.OrdinalIgnoreCase))
                {
                    FixOperatorViewportAndRows();
                }

                if (!string.Equals(
                        sceneName,
                        "OperatorMenu",
                        StringComparison.OrdinalIgnoreCase) ||
                    operatorFixed)
                {
                    break;
                }

                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void Update()
        {
            if (Time.unscaledTime < nextSweep)
                return;

            nextSweep = Time.unscaledTime + 0.75f;

            RemoveOperatorInstructions();

            if (string.Equals(
                    sceneName,
                    "OperatorMenu",
                    StringComparison.OrdinalIgnoreCase) &&
                !operatorFixed)
            {
                FixOperatorViewportAndRows();
            }
        }

        private void RemoveOperatorInstructions()
        {
            Transform service =
                FindDeepChild(
                    transform.root,
                    "BRUI_ServiceHint");

            if (service != null)
                service.gameObject.SetActive(false);

            TMP_Text[] texts = FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (TMP_Text text in texts)
            {
                if (text == null ||
                    string.IsNullOrWhiteSpace(text.text))
                {
                    continue;
                }

                string upper =
                    text.text.ToUpperInvariant();

                // Gameplay debug/service panel.
                if (upper.Contains("M OPENS OPERATOR SETTINGS"))
                {
                    text.text =
                        "ESC CLOSES PANEL";
                    continue;
                }

                // Old gameplay control footer.
                if (upper.Contains("M = OPERATOR") &&
                    (upper.Contains("SERVICE") ||
                     upper.Contains("DEBUG")))
                {
                    text.gameObject.SetActive(false);
                    continue;
                }

                // Results.
                if (upper.Contains("C = CREDIT") &&
                    upper.Contains("M = OPERATOR MENU"))
                {
                    text.text = "C = CREDIT";
                    continue;
                }

                // Attract.
                if (upper.Contains("LEFT/RIGHT SELECT") &&
                    upper.Contains("M OPERATOR"))
                {
                    text.text =
                        "LEFT/RIGHT SELECT   UP/SPACE POPS";
                    continue;
                }

                // Operator screen itself.
                if (upper.Contains("M OR ESC = RETURN TO ATTRACT"))
                {
                    text.text =
                        text.text.Replace(
                            "M OR ESC = RETURN TO ATTRACT",
                            "ESC = RETURN TO ATTRACT");
                    continue;
                }
            }
        }

        private void FixOperatorViewportAndRows()
        {
            Transform settingsPanel =
                FindDeepChild(
                    transform.root,
                    "Settings Panel");

            if (settingsPanel == null)
                return;

            Transform viewportTransform =
                FindDeepChild(
                    settingsPanel,
                    "Viewport");

            RectTransform viewport =
                viewportTransform as RectTransform;

            if (viewport == null)
                return;

            // THIS is the key v1.9.2 fix:
            // rounded alpha Mask clips rows/text at the curved corners.
            Mask oldMask =
                viewport.GetComponent<Mask>();

            if (oldMask != null)
                oldMask.enabled = false;

            RectMask2D rectMask =
                viewport.GetComponent<RectMask2D>();

            if (rectMask == null)
                rectMask =
                    viewport.gameObject.AddComponent<RectMask2D>();

            rectMask.padding =
                new Vector4(
                    0f,
                    0f,
                    0f,
                    0f);

            // Keep the visual background but make sure it isn't used as an
            // alpha mask anymore.
            Image viewportImage =
                viewport.GetComponent<Image>();

            if (viewportImage != null)
            {
                viewportImage.raycastTarget = false;
            }

            // Maximize usable rectangular width.
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin =
                new Vector2(8f, 8f);
            viewport.offsetMax =
                new Vector2(-18f, -8f);

            Transform contentTransform =
                FindDeepChild(
                    viewport,
                    "Content");

            RectTransform content =
                contentTransform as RectTransform;

            if (content == null)
                return;

            content.anchorMin =
                new Vector2(0f, 1f);

            content.anchorMax =
                new Vector2(1f, 1f);

            content.pivot =
                new Vector2(0.5f, 1f);

            content.offsetMin =
                new Vector2(0f, content.offsetMin.y);

            content.offsetMax =
                new Vector2(0f, content.offsetMax.y);

            content.localScale = Vector3.one;
            content.localRotation = Quaternion.identity;

            VerticalLayoutGroup vertical =
                content.GetComponent<VerticalLayoutGroup>();

            if (vertical != null)
            {
                vertical.padding =
                    new RectOffset(
                        6,
                        6,
                        6,
                        6);

                vertical.spacing = 6f;
                vertical.childControlWidth = true;
                vertical.childForceExpandWidth = true;
                vertical.childControlHeight = true;
                vertical.childForceExpandHeight = false;
                vertical.childAlignment =
                    TextAnchor.UpperCenter;
            }

            int normalizedRows = 0;

            for (int i = 0;
                 i < content.childCount;
                 i++)
            {
                Transform row =
                    content.GetChild(i);

                if (row == null)
                    continue;

                RectTransform rowRect =
                    row as RectTransform;

                if (rowRect == null)
                    continue;

                // Prevent old patches/layout calculations from leaving any row
                // with a horizontal position/scale that differs from the rest.
                rowRect.anchorMin =
                    new Vector2(0f, rowRect.anchorMin.y);

                rowRect.anchorMax =
                    new Vector2(1f, rowRect.anchorMax.y);

                rowRect.offsetMin =
                    new Vector2(0f, rowRect.offsetMin.y);

                rowRect.offsetMax =
                    new Vector2(0f, rowRect.offsetMax.y);

                rowRect.localScale = Vector3.one;
                rowRect.localRotation = Quaternion.identity;
                rowRect.anchoredPosition =
                    new Vector2(
                        0f,
                        rowRect.anchoredPosition.y);

                string rowName =
                    row.name ?? string.Empty;

                if (rowName.StartsWith(
                        "Info - KEYBOARD",
                        StringComparison.OrdinalIgnoreCase))
                {
                    row.gameObject.SetActive(false);

                    LayoutElement infoLayout =
                        row.GetComponent<LayoutElement>();

                    if (infoLayout != null)
                    {
                        infoLayout.ignoreLayout = true;
                        infoLayout.minHeight = 0f;
                        infoLayout.preferredHeight = 0f;
                    }

                    continue;
                }

                if (rowName.StartsWith(
                        "Header -",
                        StringComparison.OrdinalIgnoreCase))
                {
                    TMP_Text header =
                        GetDirectChildText(row);

                    if (header != null)
                    {
                        header.enableAutoSizing = true;
                        header.fontSizeMin = 15f;
                        header.fontSizeMax = 28f;
                        header.textWrappingMode =
                            TextWrappingModes.NoWrap;
                        header.overflowMode =
                            TextOverflowModes.Overflow;

                        RectTransform headerRect =
                            header.rectTransform;

                        headerRect.anchorMin =
                            new Vector2(0.02f, 0.02f);

                        headerRect.anchorMax =
                            new Vector2(0.98f, 0.98f);

                        headerRect.offsetMin = Vector2.zero;
                        headerRect.offsetMax = Vector2.zero;
                    }

                    normalizedRows++;
                    continue;
                }

                TMP_Text label =
                    GetDirectChildText(row);

                TMP_InputField input =
                    GetDirectChildInput(row);

                Toggle toggle =
                    GetDirectChildToggle(row);

                if (label != null &&
                    (input != null || toggle != null))
                {
                    LayoutElement rowLayout =
                        row.GetComponent<LayoutElement>();

                    if (rowLayout == null)
                    {
                        rowLayout =
                            row.gameObject.AddComponent<LayoutElement>();
                    }

                    rowLayout.minHeight = 112f;
                    rowLayout.preferredHeight = 112f;

                    HorizontalLayoutGroup horizontal =
                        row.GetComponent<HorizontalLayoutGroup>();

                    if (horizontal != null)
                        horizontal.enabled = false;

                    // The label owns effectively the complete row width.
                    RectTransform labelRect =
                        label.rectTransform;

                    labelRect.anchorMin =
                        new Vector2(0.02f, 0.44f);

                    labelRect.anchorMax =
                        new Vector2(0.98f, 0.98f);

                    labelRect.offsetMin =
                        new Vector2(8f, 0f);

                    labelRect.offsetMax =
                        new Vector2(-8f, 0f);

                    labelRect.localScale = Vector3.one;

                    label.enableAutoSizing = true;
                    label.fontSizeMin = 14f;
                    label.fontSizeMax = 23f;
                    label.textWrappingMode =
                        TextWrappingModes.Normal;
                    label.overflowMode =
                        TextOverflowModes.Overflow;
                    label.alignment =
                        TextAlignmentOptions.MidlineLeft;
                    label.lineSpacing = -3f;

                    LayoutElement labelLayout =
                        label.GetComponent<LayoutElement>();

                    if (labelLayout != null)
                        labelLayout.ignoreLayout = true;

                    if (input != null)
                    {
                        RectTransform inputRect =
                            input.transform as RectTransform;

                        inputRect.anchorMin =
                            new Vector2(0.52f, 0.06f);

                        inputRect.anchorMax =
                            new Vector2(0.97f, 0.39f);

                        inputRect.offsetMin = Vector2.zero;
                        inputRect.offsetMax = Vector2.zero;
                        inputRect.localScale = Vector3.one;

                        LayoutElement inputLayout =
                            input.GetComponent<LayoutElement>();

                        if (inputLayout != null)
                            inputLayout.ignoreLayout = true;
                    }

                    if (toggle != null)
                    {
                        RectTransform toggleRect =
                            toggle.transform as RectTransform;

                        toggleRect.anchorMin =
                            new Vector2(0.77f, 0.04f);

                        toggleRect.anchorMax =
                            new Vector2(0.97f, 0.40f);

                        toggleRect.offsetMin = Vector2.zero;
                        toggleRect.offsetMax = Vector2.zero;
                        toggleRect.localScale = Vector3.one;

                        LayoutElement toggleLayout =
                            toggle.GetComponent<LayoutElement>();

                        if (toggleLayout != null)
                            toggleLayout.ignoreLayout = true;
                    }
                }

                normalizedRows++;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            Canvas.ForceUpdateCanvases();

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            operatorFixed =
                normalizedRows > 10;

            if (operatorFixed)
            {
                Debug.Log(
                    "Balloon Rush v1.9.2: Operator scroll viewport changed " +
                    "from rounded alpha Mask to RectMask2D; " +
                    normalizedRows +
                    " rows normalized to full width.");
            }
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
                        .GetComponent<TMP_Text>();

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
                        .GetComponent<TMP_InputField>();

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
                        .GetComponent<Toggle>();

                if (toggle != null)
                    return toggle;
            }

            return null;
        }

        private static Transform FindDeepChild(
            Transform parent,
            string name)
        {
            if (parent == null)
                return null;

            Transform[] all =
                parent.GetComponentsInChildren<Transform>(true);

            foreach (Transform item in all)
            {
                if (item != null &&
                    string.Equals(
                        item.name,
                        name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }

            return null;
        }
    }
}
