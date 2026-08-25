using System;
using System.Collections;
using BalloonRush.Core;
using BalloonRush.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// Balloon Rush v1.8.6 feedback support, consolidated by v1.8.8.
    ///
    /// Keeps:
    /// - small passive/unattempted MISSED feedback
    /// - larger actual player MISS sizing
    /// - successful-hit release failsafe
    ///
    /// The older extra particle burst has intentionally been removed because
    /// BalloonRushHitPopV187 now owns the visible balloon-body pop effect.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class BalloonRushMissPopPolishV186 : MonoBehaviour
    {
        private static BalloonRushMissPopPolishV186 instance;

        private static readonly Color SoftMiss =
            new Color32(255, 92, 105, 220);

        private Canvas canvas;
        private TMP_FontAsset sharedFont;

        private readonly Coroutine[] passiveMissRoutines =
            new Coroutine[3];

        private readonly TMP_Text[] passiveMissLabels =
            new TMP_Text[3];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallInitialScene()
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
                        BalloonRushMissPopPolishV186>() == null)
                {
                    candidate.gameObject.AddComponent<
                        BalloonRushMissPopPolishV186>();
                }

                return;
            }
        }

        private void Awake()
        {
            instance = this;
            canvas = GetComponent<Canvas>();

            if (canvas == null)
            {
                enabled = false;
                return;
            }

            sharedFont = FindExistingFont();
            BuildPassiveMissLabels();
        }

        private void Start()
        {
            GameEvents.TimingJudged -= HandleTimingJudged;
            GameEvents.TimingJudged += HandleTimingJudged;

            GameEvents.BalloonPopped -= HandleBalloonPopped;
            GameEvents.BalloonPopped += HandleBalloonPopped;
        }

        private void OnDestroy()
        {
            GameEvents.TimingJudged -= HandleTimingJudged;
            GameEvents.BalloonPopped -= HandleBalloonPopped;

            if (instance == this)
                instance = null;
        }

        public static void NotifyPassiveMiss(int laneIndex)
        {
            if (instance != null)
                instance.ShowPassiveMiss(laneIndex);
        }

        private void BuildPassiveMissLabels()
        {
            float[] laneCenters =
            {
                0.285f,
                0.500f,
                0.715f
            };

            for (int i = 0; i < 3; i++)
            {
                GameObject go = new GameObject(
                    "BR186_PassiveMiss_" + (i + 1),
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));

                RectTransform rt =
                    go.GetComponent<RectTransform>();

                rt.SetParent(canvas.transform, false);

                float x = laneCenters[i];

                rt.anchorMin =
                    new Vector2(
                        x - 0.072f,
                        0.474f);

                rt.anchorMax =
                    new Vector2(
                        x + 0.072f,
                        0.511f);

                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                TextMeshProUGUI text =
                    go.GetComponent<TextMeshProUGUI>();

                text.text = "MISSED";
                text.alignment =
                    TextAlignmentOptions.Center;
                text.enableAutoSizing = true;
                text.fontSizeMin = 11f;
                text.fontSizeMax = 18f;
                text.fontStyle = FontStyles.Bold;
                text.textWrappingMode =
                    TextWrappingModes.NoWrap;

                text.color =
                    new Color(
                        SoftMiss.r,
                        SoftMiss.g,
                        SoftMiss.b,
                        0f);

                text.outlineColor =
                    new Color32(
                        0,
                        0,
                        0,
                        220);

                text.outlineWidth = 0.10f;
                text.raycastTarget = false;

                if (sharedFont != null)
                    text.font = sharedFont;

                passiveMissLabels[i] = text;
                go.SetActive(false);
            }
        }

        private void ShowPassiveMiss(int laneIndex)
        {
            int lane =
                Mathf.Clamp(
                    laneIndex,
                    0,
                    2);

            if (passiveMissRoutines[lane] != null)
            {
                StopCoroutine(
                    passiveMissRoutines[lane]);
            }

            passiveMissRoutines[lane] =
                StartCoroutine(
                    PassiveMissRoutine(lane));
        }

        private IEnumerator PassiveMissRoutine(int lane)
        {
            TMP_Text label =
                passiveMissLabels[lane];

            if (label == null)
                yield break;

            RectTransform rt =
                label.rectTransform;

            float startY = 0.474f;
            float endY = 0.497f;

            float minX = rt.anchorMin.x;
            float maxX = rt.anchorMax.x;

            rt.anchorMin =
                new Vector2(
                    minX,
                    startY);

            rt.anchorMax =
                new Vector2(
                    maxX,
                    startY + 0.037f);

            rt.localScale =
                Vector3.one * 0.88f;

            label.gameObject.SetActive(true);

            const float duration = 0.42f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        duration);

                float alpha;

                if (t < 0.16f)
                {
                    alpha = t / 0.16f;
                }
                else
                {
                    alpha =
                        1f -
                        Mathf.Clamp01(
                            (t - 0.44f) /
                            0.56f);
                }

                float y =
                    Mathf.Lerp(
                        startY,
                        endY,
                        t);

                rt.anchorMin =
                    new Vector2(
                        minX,
                        y);

                rt.anchorMax =
                    new Vector2(
                        maxX,
                        y + 0.037f);

                rt.localScale =
                    Vector3.one *
                    Mathf.Lerp(
                        0.88f,
                        0.98f,
                        Mathf.Min(
                            1f,
                            t * 4f));

                label.color =
                    new Color(
                        SoftMiss.r,
                        SoftMiss.g,
                        SoftMiss.b,
                        alpha * 0.68f);

                yield return null;
            }

            label.gameObject.SetActive(false);
            rt.localScale = Vector3.one;

            passiveMissRoutines[lane] = null;
        }

        private void HandleTimingJudged(
            TimingRating rating)
        {
            Transform ratingTransform =
                FindDeepChild(
                    canvas.transform,
                    "BRUI_Rating");

            TMP_Text ratingText =
                ratingTransform != null
                    ? ratingTransform.GetComponent<
                        TMP_Text>()
                    : null;

            if (ratingText == null)
                return;

            if (rating == TimingRating.Miss)
            {
                ratingText.fontSizeMin = 34f;
                ratingText.fontSizeMax = 58f;
                ratingText.characterSpacing = 1.4f;
            }
            else
            {
                ratingText.fontSizeMin = 22f;
                ratingText.fontSizeMax = 42f;
                ratingText.characterSpacing = 0f;
            }
        }

        private void HandleBalloonPopped(
            Balloon balloon,
            TimingRating rating)
        {
            if (balloon == null ||
                balloon.Definition == null ||
                balloon.Definition.IsDangerous)
            {
                return;
            }

            BalloonDefinition originalDefinition =
                balloon.Definition;

            Vector3 hitPosition =
                balloon.transform.position;

            // The normal Balloon.PopRoutine should release very quickly.
            // v1.8.7 owns the visible balloon-body burst. This coroutine is
            // only a safety net for a pooled balloon that somehow stays active.
            StartCoroutine(
                GuaranteeRelease(
                    balloon,
                    originalDefinition,
                    hitPosition));
        }

        private IEnumerator GuaranteeRelease(
            Balloon balloon,
            BalloonDefinition originalDefinition,
            Vector3 hitPosition)
        {
            yield return
                new WaitForSecondsRealtime(
                    0.28f);

            if (balloon == null)
                yield break;

            if (!balloon.IsActiveBalloon)
                yield break;

            if (balloon.Definition !=
                originalDefinition)
            {
                yield break;
            }

            if (Vector3.Distance(
                    balloon.transform.position,
                    hitPosition) > 1.5f)
            {
                yield break;
            }

            balloon.ReleaseImmediately();
        }

        private TMP_FontAsset FindExistingFont()
        {
            TMP_Text[] texts =
                canvas.GetComponentsInChildren<
                    TMP_Text>(true);

            foreach (TMP_Text text in texts)
            {
                if (text != null &&
                    text.font != null)
                {
                    return text.font;
                }
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
