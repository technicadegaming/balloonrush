using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// Balloon Rush v1.9.1 operator-menu final readability fix.
    ///
    /// - Removes public-facing Operator Menu key instructions.
    /// - Converts dynamically generated Operator setting rows from a cramped
    ///   side-by-side layout into a stacked label/value layout.
    /// - Works on the already-generated OperatorMenu scene without rebuilding.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class BalloonRushOperatorMenuFinalFixV191 : MonoBehaviour
    {
        private string sceneName;
        private float nextSweep;
        private bool operatorRowsFixed;

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

            foreach (Canvas canvas in canvases)
            {
                if (canvas == null)
                    continue;

                if (string.Equals(active, "OperatorMenu", StringComparison.OrdinalIgnoreCase) &&
                    canvas.name.IndexOf("Operator", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    target = canvas;
                    break;
                }

                if (string.Equals(active, "MainGame", StringComparison.OrdinalIgnoreCase) &&
                    canvas.name.IndexOf("Gameplay", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    target = canvas;
                    break;
                }

                if (string.Equals(active, "Results", StringComparison.OrdinalIgnoreCase) &&
                    canvas.name.IndexOf("Results", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    target = canvas;
                    break;
                }

                if (string.Equals(active, "AttractMode", StringComparison.OrdinalIgnoreCase) &&
                    canvas.name.IndexOf("Attract", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    target = canvas;
                    break;
                }
            }

            if (target != null &&
                target.GetComponent<BalloonRushOperatorMenuFinalFixV191>() == null)
            {
                target.gameObject.AddComponent<BalloonRushOperatorMenuFinalFixV191>();
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
                    FixOperatorRows();
                }

                if (!string.Equals(
                        sceneName,
                        "OperatorMenu",
                        StringComparison.OrdinalIgnoreCase) ||
                    operatorRowsFixed)
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
                !operatorRowsFixed)
            {
                FixOperatorRows();
            }
        }

        private void RemoveOperatorInstructions()
        {
            // Unified gameplay HUD: remove the tiny service/operator line entirely.
            Transform serviceHint = FindDeepChild(
                transform.root,
                "BRUI_ServiceHint");

            if (serviceHint != null)
                serviceHint.gameObject.SetActive(false);

            TMP_Text[] texts = FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (TMP_Text text in texts)
            {
                if (text == null || string.IsNullOrWhiteSpace(text.text))
                    continue;

                string original = text.text;
                string upper = original.ToUpperInvariant();

                // Original Attract instruction.
                if (upper.Contains("LEFT/RIGHT SELECT") &&
                    upper.Contains("M OPERATOR"))
                {
                    text.text = "LEFT/RIGHT SELECT   UP/SPACE POPS";
                    continue;
                }

                // Results helper.
                if (upper.Contains("C = CREDIT") &&
                    upper.Contains("M = OPERATOR MENU"))
                {
                    text.text = "C = CREDIT";
                    continue;
                }

                // Old gameplay helper if a legacy visual is visible.
                if (upper.Contains("M = OPERATOR") &&
                    upper.Contains("SERVICE"))
                {
                    text.gameObject.SetActive(false);
                    continue;
                }

                // Operator screen itself: ESC is sufficient to leave.
                if (upper.Contains("M OR ESC = RETURN TO ATTRACT"))
                {
                    text.text =
                        original.Replace(
                            "M OR ESC = RETURN TO ATTRACT",
                            "ESC = RETURN TO ATTRACT");
                    continue;
                }

                // Other exact operator hints on player-facing screens.
                if (!string.Equals(
                        sceneName,
                        "OperatorMenu",
                        StringComparison.OrdinalIgnoreCase) &&
                    (upper.Contains("M = OPERATOR MENU") ||
                     upper.Contains("M OPERATOR")))
                {
                    text.text = RemoveOperatorPhrase(original);
                }
            }
        }

        private void FixOperatorRows()
        {
            Transform settingsPanel =
                FindDeepChild(
                    transform.root,
                    "Settings Panel");

            if (settingsPanel == null)
                return;

            Transform viewport =
                FindDeepChild(
                    settingsPanel,
                    "Viewport");

            Transform contentTransform =
                viewport != null
                    ? FindDeepChild(viewport, "Content")
                    : null;

            RectTransform content =
                contentTransform as RectTransform;

            if (content == null || content.childCount < 3)
                return;

            int fixedSettings = 0;

            for (int i = 0; i < content.childCount; i++)
            {
                Transform row = content.GetChild(i);
                if (row == null)
                    continue;

                string rowName = row.name ?? string.Empty;

                // Remove the keyboard/operator instruction block entirely.
                if (rowName.StartsWith(
                        "Info - KEYBOARD",
                        StringComparison.OrdinalIgnoreCase))
                {
                    row.gameObject.SetActive(false);

                    LayoutElement hiddenLayout =
                        row.GetComponent<LayoutElement>();

                    if (hiddenLayout != null)
                    {
                        hiddenLayout.ignoreLayout = true;
                        hiddenLayout.preferredHeight = 0f;
                        hiddenLayout.minHeight = 0f;
                    }

                    continue;
                }

                if (rowName.StartsWith(
                        "Header -",
                        StringComparison.OrdinalIgnoreCase))
                {
                    LayoutElement headerLayout =
                        row.GetComponent<LayoutElement>();

                    if (headerLayout != null)
                    {
                        headerLayout.preferredHeight = 58f;
                        headerLayout.minHeight = 58f;
                    }

                    TMP_Text headerText =
                        GetDirectChildText(row);

                    if (headerText != null)
                    {
                        headerText.enableAutoSizing = true;
                        headerText.fontSizeMin = 15f;
                        headerText.fontSizeMax = 28f;
                        headerText.textWrappingMode =
                            TextWrappingModes.NoWrap;
                        headerText.overflowMode =
                            TextOverflowModes.Overflow;
                    }

                    continue;
                }

                if (rowName.StartsWith(
                        "Info -",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                TMP_Text label =
                    GetDirectChildText(row);

                TMP_InputField input =
                    GetDirectChildInput(row);

                Toggle toggle =
                    GetDirectChildToggle(row);

                if (label == null ||
                    (input == null && toggle == null))
                {
                    continue;
                }

                HorizontalLayoutGroup horizontal =
                    row.GetComponent<HorizontalLayoutGroup>();

                if (horizontal != null)
                    horizontal.enabled = false;

                LayoutElement rowLayout =
                    row.GetComponent<LayoutElement>();

                if (rowLayout == null)
                    rowLayout = row.gameObject.AddComponent<LayoutElement>();

                // Enough height for even the longest setting names to wrap.
                rowLayout.preferredHeight = 108f;
                rowLayout.minHeight = 108f;

                RectTransform labelRect =
                    label.rectTransform;

                labelRect.anchorMin =
                    new Vector2(0.025f, 0.46f);

                labelRect.anchorMax =
                    new Vector2(0.975f, 0.965f);

                labelRect.offsetMin =
                    new Vector2(10f, 0f);

                labelRect.offsetMax =
                    new Vector2(-10f, 0f);

                label.enableAutoSizing = true;
                label.fontSizeMin = 14f;
                label.fontSizeMax = 23f;
                label.fontStyle |= FontStyles.Bold;
                label.alignment =
                    TextAlignmentOptions.MidlineLeft;
                label.textWrappingMode =
                    TextWrappingModes.Normal;
                label.overflowMode =
                    TextOverflowModes.Overflow;
                label.lineSpacing = -4f;

                LayoutElement labelLayout =
                    label.GetComponent<LayoutElement>();

                if (labelLayout != null)
                    labelLayout.ignoreLayout = true;

                if (input != null)
                {
                    RectTransform inputRect =
                        input.transform as RectTransform;

                    inputRect.anchorMin =
                        new Vector2(0.54f, 0.075f);

                    inputRect.anchorMax =
                        new Vector2(0.965f, 0.405f);

                    inputRect.offsetMin = Vector2.zero;
                    inputRect.offsetMax = Vector2.zero;

                    input.pointSize = 22f;

                    LayoutElement inputLayout =
                        input.GetComponent<LayoutElement>();

                    if (inputLayout != null)
                        inputLayout.ignoreLayout = true;

                    if (input.textComponent != null)
                    {
                        input.textComponent.enableAutoSizing = true;
                        input.textComponent.fontSizeMin = 14f;
                        input.textComponent.fontSizeMax = 22f;
                        input.textComponent.textWrappingMode =
                            TextWrappingModes.NoWrap;
                    }

                    TMP_Text placeholder =
                        input.placeholder as TMP_Text;

                    if (placeholder != null)
                    {
                        placeholder.enableAutoSizing = true;
                        placeholder.fontSizeMin = 14f;
                        placeholder.fontSizeMax = 22f;
                        placeholder.textWrappingMode =
                            TextWrappingModes.NoWrap;
                    }
                }

                if (toggle != null)
                {
                    RectTransform toggleRect =
                        toggle.transform as RectTransform;

                    toggleRect.anchorMin =
                        new Vector2(0.78f, 0.055f);

                    toggleRect.anchorMax =
                        new Vector2(0.965f, 0.415f);

                    toggleRect.offsetMin = Vector2.zero;
                    toggleRect.offsetMax = Vector2.zero;

                    LayoutElement toggleLayout =
                        toggle.GetComponent<LayoutElement>();

                    if (toggleLayout != null)
                        toggleLayout.ignoreLayout = true;
                }

                fixedSettings++;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            operatorRowsFixed = fixedSettings > 10;

            if (operatorRowsFixed)
            {
                Debug.Log(
                    "Balloon Rush v1.9.1: Operator rows converted to " +
                    "full-width stacked labels (" +
                    fixedSettings +
                    " settings).");
            }
        }

        private static string RemoveOperatorPhrase(string value)
        {
            string result = value;

            result = result.Replace(
                "     M = OPERATOR MENU",
                string.Empty);

            result = result.Replace(
                "M = OPERATOR MENU     ",
                string.Empty);

            result = result.Replace(
                "   M OPERATOR",
                string.Empty);

            result = result.Replace(
                "M OPERATOR   ",
                string.Empty);

            return result.Trim();
        }

        private static TMP_Text GetDirectChildText(
            Transform row)
        {
            for (int i = 0; i < row.childCount; i++)
            {
                TMP_Text text =
                    row.GetChild(i).GetComponent<TMP_Text>();

                if (text != null)
                    return text;
            }

            return null;
        }

        private static TMP_InputField GetDirectChildInput(
            Transform row)
        {
            for (int i = 0; i < row.childCount; i++)
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
            for (int i = 0; i < row.childCount; i++)
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
            string childName)
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
                        childName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }

            return null;
        }
    }
}
