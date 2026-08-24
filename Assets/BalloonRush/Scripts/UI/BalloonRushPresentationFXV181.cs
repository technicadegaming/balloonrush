using System.Collections;
using BalloonRush.Core;
using BalloonRush.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// v1.8.1 presentation-effects companion for the unified MainGame HUD.
    /// This component does NOT change scoring, tickets, credits, input, spawning,
    /// round duration, jackpot, serial I/O, or any gameplay state.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public sealed class BalloonRushPresentationFXV181 : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform unifiedRoot;
        private RectTransform effectsRoot;
        private RectTransform hitZone;
        private TMP_Text hitLeft;
        private TMP_Text hitRight;
        private Image hitImage;
        private RectTransform comboRail;
        private RectTransform ratingPlate;
        private Image ratingPlateImage;
        private TMP_Text ratingText;
        private RectTransform comboBurst;
        private TMP_Text comboBurstText;
        private TMP_Text comboBurstSubtext;
        private Image comboBurstImage;

        private Sprite cyanPlate;
        private Sprite greenPlate;
        private Sprite goldPlate;
        private Sprite redPlate;
        private Sprite purplePlate;
        private Sprite sparkSprite;

        private Color cyan = new Color32(0, 226, 255, 255);
        private Color green = new Color32(62, 235, 102, 255);
        private Color gold = new Color32(255, 196, 30, 255);
        private Color red = new Color32(255, 68, 78, 255);
        private Color purple = new Color32(178, 82, 255, 255);
        private Color orange = new Color32(255, 132, 27, 255);

        private Coroutine impactRoutine;
        private Coroutine comboRoutine;
        private int lastMilestone;
        private float chevronPhase;

        private void Start()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                enabled = false;
                return;
            }

            BuildSprites();
            StartCoroutine(InitializeWhenUnifiedHudExists());
        }

        private IEnumerator InitializeWhenUnifiedHudExists()
        {
            float timeout = 3f;
            while (timeout > 0f)
            {
                Transform root = canvas.transform.Find("BalloonRushUnifiedHUD");
                if (root != null)
                {
                    unifiedRoot = root as RectTransform;
                    break;
                }

                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (unifiedRoot == null)
            {
                Debug.LogWarning("Balloon Rush v1.8.1 FX: unified HUD was not found. Effects companion disabled.");
                enabled = false;
                yield break;
            }

            CacheTargets();
            BuildEffectsRoot();
            UpgradeRatingPlate();
            BuildComboBurst();
            BindEvents();
        }

        private void OnDestroy()
        {
            UnbindEvents();
        }

        private void Update()
        {
            AnimateHitZoneChevrons();
        }

        private void BuildSprites()
        {
            cyanPlate = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(0, 62, 94, 245), cyan, 128, 36, 5, 30);
            greenPlate = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(8, 82, 50, 245), green, 128, 36, 5, 30);
            goldPlate = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(92, 57, 0, 248), gold, 128, 36, 5, 30);
            redPlate = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(88, 12, 25, 248), red, 128, 36, 5, 30);
            purplePlate = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(40, 16, 74, 248), purple, 128, 36, 5, 30);
            sparkSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(Color.white, Color.white, 48, 20, 1, 14);
        }

        private void CacheTargets()
        {
            hitZone = FindRect("BRUI_HitZone");
            hitLeft = FindText("BRUI_HitLeft");
            hitRight = FindText("BRUI_HitRight");
            hitImage = hitZone != null ? hitZone.GetComponent<Image>() : null;
            comboRail = FindRect("BRUI_ComboRail");
            ratingPlate = FindRect("BRUI_RatingPlate");
            ratingText = FindText("BRUI_Rating");
        }

        private void BuildEffectsRoot()
        {
            Transform old = canvas.transform.Find("BRFX_V181");
            if (old != null)
                Destroy(old.gameObject);

            GameObject go = new GameObject("BRFX_V181", typeof(RectTransform), typeof(CanvasGroup));
            effectsRoot = go.GetComponent<RectTransform>();
            effectsRoot.SetParent(canvas.transform, false);
            SetAnchors(effectsRoot, Vector2.zero, Vector2.one);
            effectsRoot.SetAsLastSibling();

            CanvasGroup group = go.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void UpgradeRatingPlate()
        {
            if (ratingPlate == null)
                return;

            ratingPlateImage = ratingPlate.GetComponent<Image>();
            if (ratingPlateImage == null)
                ratingPlateImage = ratingPlate.gameObject.AddComponent<Image>();

            ratingPlateImage.sprite = purplePlate;
            ratingPlateImage.type = Image.Type.Sliced;
            ratingPlateImage.color = new Color(1f, 1f, 1f, 0f);
            ratingPlateImage.raycastTarget = false;

            if (ratingText != null)
            {
                ratingText.outlineWidth = Mathf.Max(ratingText.outlineWidth, 0.18f);
                ratingText.outlineColor = new Color32(0, 0, 0, 235);
            }
        }

        private void BuildComboBurst()
        {
            comboBurst = CreatePanel(
                "BRFX_ComboBurst",
                effectsRoot,
                new Vector2(0.20f, 0.405f),
                new Vector2(0.80f, 0.525f),
                purplePlate,
                new Color(1f, 1f, 1f, 0f));

            comboBurstImage = comboBurst.GetComponent<Image>();

            comboBurstText = CreateText(
                "BRFX_ComboBurstText",
                comboBurst,
                string.Empty,
                55f,
                TextAlignmentOptions.Center,
                Color.white);
            SetAnchors(comboBurstText.rectTransform, new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.94f));
            comboBurstText.fontStyle |= FontStyles.Bold;
            comboBurstText.outlineWidth = 0.16f;
            comboBurstText.outlineColor = new Color32(0, 0, 0, 235);

            comboBurstSubtext = CreateText(
                "BRFX_ComboBurstSubtext",
                comboBurst,
                string.Empty,
                18f,
                TextAlignmentOptions.Center,
                gold);
            SetAnchors(comboBurstSubtext.rectTransform, new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.30f));
            comboBurstSubtext.fontStyle |= FontStyles.Bold;

            comboBurst.gameObject.SetActive(false);
        }

        private void BindEvents()
        {
            GameEvents.TimingJudged += HandleTimingJudged;
            GameEvents.ComboChanged += HandleComboChanged;
            GameEvents.JackpotWon += HandleJackpotWon;
            GameEvents.GoldenRoundStarted += HandleGoldenRoundStarted;
        }

        private void UnbindEvents()
        {
            GameEvents.TimingJudged -= HandleTimingJudged;
            GameEvents.ComboChanged -= HandleComboChanged;
            GameEvents.JackpotWon -= HandleJackpotWon;
            GameEvents.GoldenRoundStarted -= HandleGoldenRoundStarted;
        }

        private void AnimateHitZoneChevrons()
        {
            if (hitZone == null || hitLeft == null || hitRight == null)
                return;

            chevronPhase += Time.unscaledDeltaTime * 4.8f;
            float wave = 0.5f + 0.5f * Mathf.Sin(chevronPhase * Mathf.PI * 2f);
            float shift = Mathf.Lerp(0f, 13f, wave);

            RectTransform leftRt = hitLeft.rectTransform;
            RectTransform rightRt = hitRight.rectTransform;
            leftRt.anchoredPosition = new Vector2(shift, 0f);
            rightRt.anchoredPosition = new Vector2(-shift, 0f);

            Color glow = Color.Lerp(new Color(cyan.r, cyan.g, cyan.b, 0.50f), Color.white, wave);
            hitLeft.color = glow;
            hitRight.color = glow;
        }

        private void HandleTimingJudged(TimingRating rating)
        {
            Sprite sprite;
            Color color;
            float strength;

            switch (rating)
            {
                case TimingRating.Perfect:
                    sprite = goldPlate;
                    color = gold;
                    strength = 1.0f;
                    break;
                case TimingRating.Great:
                    sprite = greenPlate;
                    color = green;
                    strength = 0.82f;
                    break;
                case TimingRating.Good:
                    sprite = cyanPlate;
                    color = cyan;
                    strength = 0.62f;
                    break;
                default:
                    sprite = redPlate;
                    color = red;
                    strength = 0.46f;
                    break;
            }

            if (impactRoutine != null)
                StopCoroutine(impactRoutine);
            impactRoutine = StartCoroutine(TimingImpactRoutine(sprite, color, strength));
        }

        private IEnumerator TimingImpactRoutine(Sprite sprite, Color color, float strength)
        {
            if (ratingPlateImage != null)
            {
                ratingPlateImage.sprite = sprite;
                ratingPlateImage.color = new Color(1f, 1f, 1f, 0.94f);
            }

            if (hitImage != null)
                hitImage.color = color;

            SpawnBurst(color, Mathf.RoundToInt(Mathf.Lerp(5f, 13f, strength)), 0.52f);

            Vector3 hitBase = hitZone != null ? hitZone.localScale : Vector3.one;
            Vector3 plateBase = ratingPlate != null ? ratingPlate.localScale : Vector3.one;
            float elapsed = 0f;
            const float duration = 0.42f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pop = Mathf.Sin(t * Mathf.PI);

                if (hitZone != null)
                    hitZone.localScale = hitBase * (1f + pop * 0.055f * strength);
                if (ratingPlate != null)
                    ratingPlate.localScale = plateBase * (1f + pop * 0.11f * strength);
                if (ratingPlateImage != null)
                {
                    Color c = ratingPlateImage.color;
                    c.a = Mathf.Lerp(0.94f, 0f, Mathf.Clamp01((t - 0.58f) / 0.42f));
                    ratingPlateImage.color = c;
                }
                yield return null;
            }

            if (hitZone != null)
                hitZone.localScale = hitBase;
            if (ratingPlate != null)
                ratingPlate.localScale = plateBase;
            if (ratingPlateImage != null)
                ratingPlateImage.color = new Color(1f, 1f, 1f, 0f);

            impactRoutine = null;
        }

        private void HandleComboChanged(int combo)
        {
            int milestone = GetMilestone(combo);
            if (milestone <= 0 || milestone == lastMilestone)
                return;

            lastMilestone = milestone;
            if (comboRoutine != null)
                StopCoroutine(comboRoutine);
            comboRoutine = StartCoroutine(ComboMilestoneRoutine(milestone));
        }

        private static int GetMilestone(int combo)
        {
            if (combo >= 30) return 30;
            if (combo >= 20) return 20;
            if (combo >= 15) return 15;
            if (combo >= 10) return 10;
            if (combo >= 5) return 5;
            return 0;
        }

        private IEnumerator ComboMilestoneRoutine(int combo)
        {
            if (comboBurst == null)
                yield break;

            Color accent = combo >= 20 ? gold : combo >= 10 ? orange : purple;
            string sub = combo >= 30 ? "MEGA STREAK!" : combo >= 20 ? "AMAZING!" : combo >= 15 ? "KEEP IT GOING!" : combo >= 10 ? "ON FIRE!" : "NICE STREAK!";

            comboBurst.gameObject.SetActive(true);
            comboBurstText.text = "COMBO x" + combo;
            comboBurstText.color = Color.white;
            comboBurstSubtext.text = sub;
            comboBurstSubtext.color = accent;
            comboBurstImage.sprite = combo >= 20 ? goldPlate : purplePlate;
            comboBurstImage.color = Color.white;

            SpawnBurst(accent, combo >= 20 ? 18 : 10, 0.70f);

            Vector3 baseScale = Vector3.one;
            float elapsed = 0f;
            const float duration = 0.95f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float entry = Mathf.Clamp01(t / 0.20f);
                float exit = t > 0.68f ? 1f - Mathf.Clamp01((t - 0.68f) / 0.32f) : 1f;
                float bounce = 1f + Mathf.Sin(entry * Mathf.PI) * 0.12f;
                comboBurst.localScale = baseScale * bounce;
                comboBurstImage.color = new Color(1f, 1f, 1f, exit);
                comboBurstText.alpha = exit;
                comboBurstSubtext.alpha = exit;

                if (comboRail != null)
                    comboRail.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.025f);

                yield return null;
            }

            comboBurst.localScale = Vector3.one;
            comboBurstText.alpha = 1f;
            comboBurstSubtext.alpha = 1f;
            comboBurst.gameObject.SetActive(false);
            if (comboRail != null)
                comboRail.localScale = Vector3.one;

            comboRoutine = null;
        }

        private void HandleJackpotWon(int tickets)
        {
            SpawnBurst(gold, 28, 1.0f);
        }

        private void HandleGoldenRoundStarted()
        {
            SpawnBurst(gold, 18, 0.8f);
        }

        private void SpawnBurst(Color color, int count, float radius)
        {
            if (effectsRoot == null)
                return;

            Vector2 center = new Vector2(0.50f, 0.50f);
            for (int i = 0; i < count; i++)
            {
                float angle = (360f / Mathf.Max(1, count)) * i + Random.Range(-9f, 9f);
                float r = Random.Range(radius * 0.35f, radius);
                Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                StartCoroutine(SparkRoutine(center, direction, r, color, i));
            }
        }

        private IEnumerator SparkRoutine(Vector2 center, Vector2 direction, float distance, Color color, int index)
        {
            GameObject go = new GameObject("BRFX_Spark_" + index, typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(effectsRoot, false);
            rt.anchorMin = center;
            rt.anchorMax = center;
            rt.sizeDelta = new Vector2(Random.Range(8f, 16f), Random.Range(8f, 18f));

            Image img = go.GetComponent<Image>();
            img.sprite = sparkSprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;

            Vector2 start = Vector2.zero;
            Vector2 end = direction * distance * 220f;
            float duration = Random.Range(0.32f, 0.58f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);
                rt.anchoredPosition = Vector2.Lerp(start, end, eased);
                rt.localRotation = Quaternion.Euler(0f, 0f, t * 180f + index * 11f);
                img.color = new Color(color.r, color.g, color.b, 1f - t);
                yield return null;
            }

            Destroy(go);
        }

        private RectTransform FindRect(string name)
        {
            Transform t = unifiedRoot != null ? unifiedRoot.Find(name) : null;
            if (t == null && unifiedRoot != null)
            {
                RectTransform[] all = unifiedRoot.GetComponentsInChildren<RectTransform>(true);
                foreach (RectTransform rt in all)
                {
                    if (rt != null && rt.name == name)
                        return rt;
                }
            }
            return t as RectTransform;
        }

        private TMP_Text FindText(string name)
        {
            RectTransform rt = FindRect(name);
            return rt != null ? rt.GetComponent<TMP_Text>() : null;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 min, Vector2 max, Sprite sprite, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            SetAnchors(rt, min, max);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return rt;
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            SetAnchors(rt, Vector2.zero, Vector2.one);

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = Mathf.Max(9f, fontSize * 0.58f);
            tmp.fontSizeMax = fontSize;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
