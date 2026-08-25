using System;
using System.Collections;
using BalloonRush.Core;
using BalloonRush.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// Balloon Rush v1.9.0 cabinet-final presentation pass.
    ///
    /// Final goals:
    /// - untouched reward balloons no longer break combo by default
    /// - hit-zone copy gets out of the balloon's way
    /// - rating feedback is shorter/less obstructive
    /// - live Combo/Payout labels are easier to read
    /// - background scanners/rays are quieter than gameplay objects
    /// - Results prioritizes the useful numbers
    ///
    /// No ticket math, score math, timing windows, hardware protocol,
    /// balloon speed, spawn weights, or payout limits are changed here.
    /// </summary>
    [DefaultExecutionOrder(900)]
    public sealed class BalloonRushCabinetFinalV190 : MonoBehaviour
    {
        private const string ComboMigrationKey =
            "BalloonRush.v1.9.0.PassedBalloonComboMigration";

        private string sceneName;
        private int ratingGeneration;
        private float nextPolishAttempt;
        private bool mainPolished;
        private bool resultsPolished;
        private bool settingsMigrationChecked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
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
            string active =
                SceneManager.GetActiveScene().name;

            if (!string.Equals(
                    active,
                    "AttractMode",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    active,
                    "MainGame",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(
                    active,
                    "Results",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Canvas[] canvases =
                FindObjectsByType<Canvas>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            if (canvases == null ||
                canvases.Length == 0)
            {
                return;
            }

            Canvas target = null;
            string preferred =
                string.Equals(
                    active,
                    "MainGame",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Gameplay"
                    : string.Equals(
                        active,
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
                    target = canvas;
                    break;
                }
            }

            if (target == null)
                target = canvases[0];

            if (target != null &&
                target.GetComponent<
                    BalloonRushCabinetFinalV190>() == null)
            {
                target.gameObject.AddComponent<
                    BalloonRushCabinetFinalV190>();
            }
        }

        private IEnumerator Start()
        {
            sceneName =
                SceneManager.GetActiveScene().name;

            GameEvents.TimingJudged -= HandleTimingJudged;
            GameEvents.TimingJudged += HandleTimingJudged;

            float timeout = 3f;

            while (timeout > 0f)
            {
                TryApplyPolish();
                TryMigrateComboDefault();

                bool presentationDone =
                    !string.Equals(
                        sceneName,
                        "MainGame",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(
                        sceneName,
                        "Results",
                        StringComparison.OrdinalIgnoreCase);

                if (string.Equals(
                        sceneName,
                        "MainGame",
                        StringComparison.OrdinalIgnoreCase))
                {
                    presentationDone = mainPolished;
                }
                else if (string.Equals(
                             sceneName,
                             "Results",
                             StringComparison.OrdinalIgnoreCase))
                {
                    presentationDone = resultsPolished;
                }

                if (presentationDone &&
                    settingsMigrationChecked)
                {
                    break;
                }

                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void OnDestroy()
        {
            GameEvents.TimingJudged -= HandleTimingJudged;
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextPolishAttempt)
            {
                nextPolishAttempt =
                    Time.unscaledTime + 0.75f;

                TryApplyPolish();
                TryMigrateComboDefault();
            }
        }

        private void LateUpdate()
        {
            if (!string.Equals(
                    sceneName,
                    "MainGame",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // v1.8.5 animates the lane scanner every Update.
            // Tone it down after that animation has run so balloons stay
            // visually dominant.
            for (int i = 1; i <= 3; i++)
            {
                Transform scanner =
                    FindDeepChild(
                        transform.root,
                        "BR185_LaneScanner_" + i);

                Image image =
                    scanner != null
                        ? scanner.GetComponent<Image>()
                        : null;

                if (image == null)
                    continue;

                Color c = image.color;
                c.a *= 0.52f;
                image.color = c;
            }
        }

        private void TryMigrateComboDefault()
        {
            if (settingsMigrationChecked)
                return;

            if (PlayerPrefs.GetInt(
                    ComboMigrationKey,
                    0) == 1)
            {
                settingsMigrationChecked = true;
                return;
            }

            if (!GameServices.IsReady ||
                GameServices.Settings == null ||
                GameServices.Settings.Current == null)
            {
                return;
            }

            OperatorSettings editable =
                GameServices.Settings.CreateEditableCopy();

            if (editable != null)
            {
                // v1.9.0 cabinet default:
                // deliberately ignoring a reward balloon is not a failed
                // POP attempt. Timeout, actual miss and bomb behavior remain.
                editable.passedBalloonBreaksCombo = false;
                GameServices.Settings.Apply(editable);
            }

            PlayerPrefs.SetInt(
                ComboMigrationKey,
                1);

            PlayerPrefs.Save();

            settingsMigrationChecked = true;

            Debug.Log(
                "Balloon Rush v1.9.0: Passed reward balloons break combo " +
                "default migrated to OFF. The Operator Menu can still turn it ON.");
        }

        private void TryApplyPolish()
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                sceneName =
                    SceneManager.GetActiveScene().name;
            }

            if (string.Equals(
                    sceneName,
                    "MainGame",
                    StringComparison.OrdinalIgnoreCase))
            {
                ApplyMainGamePolish();
            }
            else if (string.Equals(
                         sceneName,
                         "Results",
                         StringComparison.OrdinalIgnoreCase))
            {
                ApplyResultsPolish();
            }
        }

        private void ApplyMainGamePolish()
        {
            Transform hitZone =
                FindDeepChild(
                    transform.root,
                    "BRUI_HitZone");

            Transform hitLabel =
                FindDeepChild(
                    transform.root,
                    "BRUI_HitLabel");

            Transform hitLeft =
                FindDeepChild(
                    transform.root,
                    "BRUI_HitLeft");

            Transform hitRight =
                FindDeepChild(
                    transform.root,
                    "BRUI_HitRight");

            Transform ratingPlate =
                FindDeepChild(
                    transform.root,
                    "BRUI_RatingPlate");

            Transform rating =
                FindDeepChild(
                    transform.root,
                    "BRUI_Rating");

            Transform comboSub =
                FindDeepChild(
                    transform.root,
                    "BRUI_ComboSubtext");

            Transform comboValue =
                FindDeepChild(
                    transform.root,
                    "BRUI_ComboValue");

            Transform payoutTitle =
                FindDeepChild(
                    transform.root,
                    "BRUI_PayoutTitle");

            if (hitZone == null ||
                comboSub == null ||
                payoutTitle == null)
            {
                return;
            }

            Image hitImage =
                hitZone.GetComponent<Image>();

            if (hitImage != null)
            {
                Color c = hitImage.color;
                c.a = Mathf.Min(c.a, 0.78f);
                hitImage.color = c;
            }

            StyleHitZoneText(
                hitLabel,
                new Vector2(0.22f, 0.66f),
                new Vector2(0.78f, 0.99f),
                16f,
                21f);

            StyleHitZoneText(
                hitLeft,
                new Vector2(0.03f, 0.66f),
                new Vector2(0.23f, 0.99f),
                14f,
                20f);

            StyleHitZoneText(
                hitRight,
                new Vector2(0.77f, 0.66f),
                new Vector2(0.97f, 0.99f),
                14f,
                20f);

            RectTransform ratingPlateRect =
                ratingPlate as RectTransform;

            if (ratingPlateRect != null)
            {
                ratingPlateRect.anchorMin =
                    new Vector2(
                        0.285f,
                        0.408f);

                ratingPlateRect.anchorMax =
                    new Vector2(
                        0.715f,
                        0.454f);

                ratingPlateRect.offsetMin =
                    Vector2.zero;

                ratingPlateRect.offsetMax =
                    Vector2.zero;
            }

            TMP_Text ratingText =
                rating != null
                    ? rating.GetComponent<TMP_Text>()
                    : null;

            if (ratingText != null)
            {
                ratingText.enableAutoSizing = true;
                ratingText.fontSizeMin = 24f;
                ratingText.fontSizeMax = 48f;
                ratingText.textWrappingMode =
                    TextWrappingModes.NoWrap;
            }

            TMP_Text comboSubText =
                comboSub.GetComponent<TMP_Text>();

            if (comboSubText != null)
            {
                comboSubText.enableAutoSizing = true;
                comboSubText.fontSizeMin = 13f;
                comboSubText.fontSizeMax = 18f;
                comboSubText.textWrappingMode =
                    TextWrappingModes.Normal;

                RectTransform rt =
                    comboSubText.rectTransform;

                rt.anchorMin =
                    new Vector2(
                        0.035f,
                        0.018f);

                rt.anchorMax =
                    new Vector2(
                        0.965f,
                        0.185f);

                rt.offsetMin =
                    Vector2.zero;

                rt.offsetMax =
                    Vector2.zero;
            }

            TMP_Text comboValueText =
                comboValue != null
                    ? comboValue.GetComponent<TMP_Text>()
                    : null;

            if (comboValueText != null)
            {
                comboValueText.enableAutoSizing = true;
                comboValueText.fontSizeMin = 18f;
                comboValueText.fontSizeMax = 29f;
            }

            TMP_Text payoutTitleText =
                payoutTitle.GetComponent<TMP_Text>();

            if (payoutTitleText != null)
            {
                payoutTitleText.enableAutoSizing = true;
                payoutTitleText.fontSizeMin = 10f;
                payoutTitleText.fontSizeMax = 18f;
                payoutTitleText.lineSpacing = -8f;
                payoutTitleText.textWrappingMode =
                    TextWrappingModes.NoWrap;

                RectTransform rt =
                    payoutTitleText.rectTransform;

                rt.anchorMin =
                    new Vector2(
                        0.03f,
                        0.892f);

                rt.anchorMax =
                    new Vector2(
                        0.97f,
                        0.997f);

                rt.offsetMin =
                    Vector2.zero;

                rt.offsetMax =
                    Vector2.zero;
            }

            // Quieter static background rays.
            for (int i = 0; i < 12; i++)
            {
                Transform ray =
                    FindDeepChild(
                        transform.root,
                        "BRUI_BackRay_" + i);

                Image image =
                    ray != null
                        ? ray.GetComponent<Image>()
                        : null;

                if (image == null)
                    continue;

                Color c = image.color;
                c.a = Mathf.Min(c.a, 0.020f);
                image.color = c;
            }

            mainPolished = true;
        }

        private static void StyleHitZoneText(
            Transform target,
            Vector2 anchorMin,
            Vector2 anchorMax,
            float minSize,
            float maxSize)
        {
            TMP_Text text =
                target != null
                    ? target.GetComponent<TMP_Text>()
                    : null;

            if (text == null)
                return;

            text.enableAutoSizing = true;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
            text.textWrappingMode =
                TextWrappingModes.NoWrap;

            RectTransform rt =
                text.rectTransform;

            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void HandleTimingJudged(
            Gameplay.TimingRating rating)
        {
            ratingGeneration++;
            int generation = ratingGeneration;

            // Actual player feedback remains readable, but gets out of the
            // playfield faster than the older 0.48-0.65 second passes.
            StartCoroutine(
                HideRatingSoon(
                    generation));
        }

        private IEnumerator HideRatingSoon(
            int generation)
        {
            yield return
                new WaitForSecondsRealtime(
                    0.38f);

            if (generation != ratingGeneration)
                yield break;

            Transform rating =
                FindDeepChild(
                    transform.root,
                    "BRUI_Rating");

            if (rating != null)
                rating.gameObject.SetActive(false);
        }

        private void ApplyResultsPolish()
        {
            TMP_Text[] texts =
                FindObjectsByType<TMP_Text>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            bool foundUsefulResults = false;

            foreach (TMP_Text text in texts)
            {
                if (text == null ||
                    string.IsNullOrWhiteSpace(text.text))
                {
                    continue;
                }

                string value =
                    text.text.ToUpperInvariant();

                if (value.Contains(
                        "FINAL SCORE"))
                {
                    StyleResultsText(
                        text,
                        21f,
                        34f,
                        true);

                    foundUsefulResults = true;
                }
                else if (value.Contains(
                             "HIGHEST COMBO"))
                {
                    StyleResultsText(
                        text,
                        21f,
                        33f,
                        true);

                    foundUsefulResults = true;
                }
                else if (value.Contains("PERFECT") &&
                         value.Contains("GREAT") &&
                         value.Contains("GOOD"))
                {
                    StyleResultsText(
                        text,
                        16f,
                        27f,
                        true);

                    text.characterSpacing = 0.35f;
                    foundUsefulResults = true;
                }
                else if (value.Contains(
                             "GOLDEN BALLOONS"))
                {
                    StyleResultsText(
                        text,
                        17f,
                        27f,
                        true);

                    foundUsefulResults = true;
                }
            }

            if (foundUsefulResults)
                resultsPolished = true;
        }

        private static void StyleResultsText(
            TMP_Text text,
            float min,
            float max,
            bool bold)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
            text.textWrappingMode =
                TextWrappingModes.NoWrap;

            if (bold)
                text.fontStyle |= FontStyles.Bold;
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
