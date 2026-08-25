using System;
using System.Collections.Generic;
using BalloonRush.Core;
using BalloonRush.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// Balloon Rush v1.8.4 visual polish.
    /// Runtime auto-installs on the Gameplay Canvas.
    /// No prefab/scene wiring is required.
    /// </summary>
    [DefaultExecutionOrder(-55)]
    public sealed class BalloonRushCabinetEffectsV184 : MonoBehaviour
    {
        private readonly List<Image> leftBulbs = new List<Image>(20);
        private readonly List<Image> rightBulbs = new List<Image>(20);
        private readonly Image[] laneFrames = new Image[3];

        private Canvas canvas;
        private RectTransform root;
        private Sprite capsuleDark;
        private Sprite capsuleGlow;
        private Sprite capsulePanel;
        private float flashBoost;
        private float hueOffset;

        private static readonly Color Cyan = new Color32(0, 229, 255, 255);
        private static readonly Color Magenta = new Color32(255, 36, 185, 255);
        private static readonly Color Blue = new Color32(36, 130, 255, 255);
        private static readonly Color Gold = new Color32(255, 194, 28, 255);
        private static readonly Color Green = new Color32(35, 232, 105, 255);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Canvas c in canvases)
            {
                if (c == null)
                    continue;

                if (c.name.IndexOf("Gameplay", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (c.GetComponent<BalloonRushCabinetEffectsV184>() == null)
                    c.gameObject.AddComponent<BalloonRushCabinetEffectsV184>();

                return;
            }
        }

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                enabled = false;
                return;
            }

            capsuleDark = RoundedSpriteFactory.CreateRoundedPanelSprite(
                new Color32(4, 15, 40, 238),
                new Color32(0, 229, 255, 180),
                128, 56, 4, 48);

            capsuleGlow = RoundedSpriteFactory.CreateRoundedPanelSprite(
                Color.white,
                Color.white,
                128, 58, 1, 50);

            capsulePanel = RoundedSpriteFactory.CreateRoundedPanelSprite(
                new Color32(7, 20, 50, 246),
                new Color32(82, 210, 255, 190),
                128, 50, 4, 44);

            BuildFxLayer();
            RestyleExistingHud();
        }

        private void OnEnable()
        {
            GameEvents.BalloonPopped += HandleBalloonPopped;
            GameEvents.TimingJudged += HandleTimingJudged;
            GameEvents.GoldenRoundStarted += HandleGoldenRoundStarted;
            GameEvents.JackpotWon += HandleJackpot;
        }

        private void OnDisable()
        {
            GameEvents.BalloonPopped -= HandleBalloonPopped;
            GameEvents.TimingJudged -= HandleTimingJudged;
            GameEvents.GoldenRoundStarted -= HandleGoldenRoundStarted;
            GameEvents.JackpotWon -= HandleJackpot;
        }

        private void Update()
        {
            AnimateSideLights();
            AnimateLaneFrames();
            flashBoost = Mathf.MoveTowards(flashBoost, 0f, Time.unscaledDeltaTime * 2.7f);
        }

        private void BuildFxLayer()
        {
            Transform existing = canvas.transform.Find("BR184_CabinetFX");
            if (existing != null)
                Destroy(existing.gameObject);

            GameObject go = new GameObject(
                "BR184_CabinetFX",
                typeof(RectTransform),
                typeof(CanvasGroup));

            root = go.GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            Stretch(root);
            root.SetAsLastSibling();

            CanvasGroup group = go.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            BuildSideTower(true);
            BuildSideTower(false);
            BuildLaneFrames();

            CreateCapsule(
                "BR184_TopSweep",
                root,
                new Vector2(0.145f, 0.812f),
                new Vector2(0.855f, 0.833f),
                new Color(Cyan.r, Cyan.g, Cyan.b, 0.18f),
                capsuleGlow);

            CreateCapsule(
                "BR184_BottomSweep",
                root,
                new Vector2(0.145f, 0.190f),
                new Vector2(0.855f, 0.212f),
                new Color(Magenta.r, Magenta.g, Magenta.b, 0.16f),
                capsuleGlow);
        }

        private void RestyleExistingHud()
        {
            // The prior rebuild already creates these objects, but the corner radius
            // was too subtle. Replace the visible large panels with much rounder sprites.
            string[] panelNames =
            {
                "BRUI_Header",
                "BRUI_TicketsCard",
                "BRUI_TitleCard",
                "BRUI_JackpotCard",
                "BRUI_TimerCard",
                "BRUI_ComboRail",
                "BRUI_PayoutRail",
                "BRUI_ControlDeck",
                "BRUI_Control_LEFT",
                "BRUI_Control_POP",
                "BRUI_Control_RIGHT",
                "BRUI_ControlFace_LEFT",
                "BRUI_ControlFace_POP",
                "BRUI_ControlFace_RIGHT",
                "BRUI_HitZone"
            };

            foreach (string panelName in panelNames)
            {
                Transform t = FindDeepChild(canvas.transform, panelName);
                if (t == null)
                    continue;

                Image image = t.GetComponent<Image>();
                if (image == null)
                    continue;

                image.sprite = capsulePanel;
                image.type = Image.Type.Sliced;
            }

            // Lane tabs should look like pills, not short rectangles.
            for (int i = 1; i <= 3; i++)
            {
                Transform t = FindDeepChild(canvas.transform, "BRUI_LaneTab_" + i);
                if (t == null) continue;

                Image image = t.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = capsulePanel;
                    image.type = Image.Type.Sliced;
                }
            }
        }

        private void BuildSideTower(bool left)
        {
            float x0 = left ? 0.006f : 0.952f;
            float x1 = left ? 0.050f : 0.994f;
            Color baseColor = left ? Magenta : Cyan;

            RectTransform tower = CreateCapsule(
                left ? "BR184_LeftTower" : "BR184_RightTower",
                root,
                new Vector2(x0, 0.205f),
                new Vector2(x1, 0.825f),
                new Color(baseColor.r, baseColor.g, baseColor.b, 0.22f),
                capsuleDark);

            const int count = 17;
            for (int i = 0; i < count; i++)
            {
                float y0 = 0.018f + i * 0.0565f;
                float y1 = Mathf.Min(0.982f, y0 + 0.038f);

                RectTransform bulb = CreateCapsule(
                    (left ? "BR184_L_" : "BR184_R_") + i,
                    tower,
                    new Vector2(0.17f, y0),
                    new Vector2(0.83f, y1),
                    Color.white,
                    capsuleGlow);

                Image image = bulb.GetComponent<Image>();
                image.color = new Color(
                    baseColor.r,
                    baseColor.g,
                    baseColor.b,
                    0.15f);

                if (left)
                    leftBulbs.Add(image);
                else
                    rightBulbs.Add(image);
            }
        }

        private void BuildLaneFrames()
        {
            float[] mins = { 0.176f, 0.392f, 0.608f };
            float[] maxs = { 0.387f, 0.603f, 0.824f };
            Color[] colors = { Blue, Gold, Green };

            for (int i = 0; i < 3; i++)
            {
                RectTransform frame = CreateCapsule(
                    "BR184_LaneFrame_" + (i + 1),
                    root,
                    new Vector2(mins[i], 0.222f),
                    new Vector2(maxs[i], 0.783f),
                    new Color(colors[i].r, colors[i].g, colors[i].b, 0.10f),
                    capsuleDark);

                laneFrames[i] = frame.GetComponent<Image>();

                RectTransform inner = CreateCapsule(
                    "BR184_LaneInner_" + (i + 1),
                    frame,
                    new Vector2(0.045f, 0.012f),
                    new Vector2(0.955f, 0.988f),
                    new Color(0.005f, 0.015f, 0.055f, 0.72f),
                    capsuleGlow);

                inner.SetAsFirstSibling();
            }
        }

        private void AnimateSideLights()
        {
            float t = Time.unscaledTime;
            AnimateBulbs(leftBulbs, t * 5.7f, Magenta, Cyan, false);
            AnimateBulbs(rightBulbs, t * 5.7f + 3.2f, Cyan, Gold, true);

            hueOffset = Mathf.Repeat(
                hueOffset + Time.unscaledDeltaTime * 0.012f,
                1f);
        }

        private void AnimateBulbs(
            List<Image> bulbs,
            float phase,
            Color a,
            Color b,
            bool reverse)
        {
            int count = bulbs.Count;

            for (int i = 0; i < count; i++)
            {
                int waveIndex = reverse ? count - 1 - i : i;

                float wave =
                    0.5f +
                    0.5f * Mathf.Sin(phase - waveIndex * 0.74f);

                wave = Mathf.Pow(wave, 4.6f);

                float hue = Mathf.Repeat(
                    waveIndex / (float)Mathf.Max(1, count - 1) +
                    hueOffset +
                    phase * 0.010f,
                    1f);

                Color rainbow =
                    Color.HSVToRGB(hue, 0.82f, 1f);

                Color baseMixed =
                    Color.Lerp(a, b, 0.45f);

                Color mixed =
                    Color.Lerp(baseMixed, rainbow, 0.42f);

                float alpha = Mathf.Clamp01(
                    0.09f +
                    wave * 0.84f +
                    flashBoost * 0.42f);

                Image image = bulbs[i];
                image.color =
                    new Color(mixed.r, mixed.g, mixed.b, alpha);

                image.transform.localScale =
                    Vector3.one *
                    (0.84f +
                     wave * 0.30f +
                     flashBoost * 0.05f);
            }
        }

        private void AnimateLaneFrames()
        {
            LaneManager lanes =
                FindFirstObjectByType<LaneManager>(
                    FindObjectsInactive.Include);

            int selected =
                lanes != null
                    ? Mathf.Clamp(lanes.SelectedLane, 0, 2)
                    : 1;

            Color[] colors = { Blue, Gold, Green };

            for (int i = 0; i < laneFrames.Length; i++)
            {
                Image image = laneFrames[i];
                if (image == null)
                    continue;

                bool active = i == selected;

                float pulse = active
                    ? 0.5f +
                      0.5f * Mathf.Sin(Time.unscaledTime * 7.8f)
                    : 0f;

                float alpha =
                    active
                        ? 0.25f + pulse * 0.26f
                        : 0.075f;

                alpha +=
                    flashBoost *
                    (active ? 0.18f : 0.045f);

                Color c = colors[i];

                image.color =
                    new Color(
                        c.r,
                        c.g,
                        c.b,
                        Mathf.Clamp01(alpha));

                image.transform.localScale =
                    Vector3.one *
                    (active
                        ? 1.006f + pulse * 0.012f
                        : 1f);
            }
        }

        private void HandleBalloonPopped(
            Balloon balloon,
            TimingRating rating)
        {
            flashBoost = Mathf.Max(
                flashBoost,
                rating == TimingRating.Perfect
                    ? 1f
                    : 0.52f);
        }

        private void HandleTimingJudged(TimingRating rating)
        {
            if (rating == TimingRating.Miss)
                flashBoost = Mathf.Max(flashBoost, 0.34f);
        }

        private void HandleGoldenRoundStarted()
        {
            flashBoost = 1.35f;
        }

        private void HandleJackpot(int tickets)
        {
            flashBoost = 2f;
        }

        private RectTransform CreateCapsule(
            string objectName,
            Transform parent,
            Vector2 min,
            Vector2 max,
            Color color,
            Sprite sprite)
        {
            GameObject go = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image));

            RectTransform rt =
                go.GetComponent<RectTransform>();

            rt.SetParent(parent, false);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image image =
                go.GetComponent<Image>();

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;

            return rt;
        }

        private static Transform FindDeepChild(
            Transform parent,
            string childName)
        {
            if (parent == null)
                return null;

            Transform[] all =
                parent.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in all)
            {
                if (t != null &&
                    string.Equals(
                        t.name,
                        childName,
                        StringComparison.OrdinalIgnoreCase))
                    return t;
            }

            return null;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
