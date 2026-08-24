using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BalloonRush.UI
{
    /// <summary>
    /// v1.8.2 Results refinement. Presentation only.
    /// ResultsManager remains responsible for result values, payouts, credits,
    /// replay, countdown, and scene navigation.
    /// </summary>
    [DefaultExecutionOrder(62)]
    public sealed class BalloonRushResultsPresentationV182 : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform backRoot;
        private RectTransform frontRoot;
        private RectTransform ticketCard;
        private RectTransform statisticsCard;
        private RectTransform replayCard;
        private TMP_Text ticketResult;
        private TMP_Text replayPrompt;

        private Sprite panel;
        private Sprite goldPanel;
        private Sprite cyanPanel;
        private Sprite sparkSprite;

        private readonly Color cyan = new Color32(0, 226, 255, 255);
        private readonly Color magenta = new Color32(255, 35, 183, 255);
        private readonly Color gold = new Color32(255, 193, 28, 255);
        private readonly Color green = new Color32(52, 231, 93, 255);

        private IEnumerator Start()
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                yield break;

            BuildSprites();
            RemoveOldDecor();
            BuildBackgroundDecor();

            // ResultsManager populates values during Start.
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.08f);

            CacheCards();
            StyleCards();
            StyleTexts();
            BuildConfettiRoot();

            if (ticketResult != null)
                StartCoroutine(WatchTicketCountAndCelebrate());
            if (replayPrompt != null)
                StartCoroutine(PulseReplayPrompt());
        }

        private void BuildSprites()
        {
            panel = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(4, 20, 48, 247), cyan, 128, 36, 5, 30);
            goldPanel = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(74, 44, 0, 248), gold, 128, 42, 6, 34);
            cyanPanel = RoundedSpriteFactory.CreateRoundedPanelSprite(new Color32(0, 44, 72, 245), cyan, 128, 38, 5, 30);
            sparkSprite = RoundedSpriteFactory.CreateRoundedPanelSprite(Color.white, Color.white, 48, 20, 1, 14);
        }

        private void RemoveOldDecor()
        {
            DestroyCanvasChild("BRUI_ResultsDecor");
            DestroyCanvasChild("BRFX_ResultsBackV181");
            DestroyCanvasChild("BRFX_ResultsFrontV181");
            DestroyCanvasChild("BRFX_ResultsBackV182");
            DestroyCanvasChild("BRFX_ResultsFrontV182");
        }

        private void BuildBackgroundDecor()
        {
            GameObject back = new GameObject("BRFX_ResultsBackV182", typeof(RectTransform), typeof(CanvasGroup));
            backRoot = back.GetComponent<RectTransform>();
            backRoot.SetParent(canvas.transform, false);
            SetAnchors(backRoot, Vector2.zero, Vector2.one);
            backRoot.SetAsFirstSibling();

            CanvasGroup group = back.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            CreateSolid("BRFX_ResultLeftGlow", backRoot, new Vector2(0.003f, 0f), new Vector2(0.013f, 1f), magenta);
            CreateSolid("BRFX_ResultRightGlow", backRoot, new Vector2(0.987f, 0f), new Vector2(0.997f, 1f), cyan);

            for (int i = 0; i < 12; i++)
            {
                float x = 0.035f + i * 0.078f;
                RectTransform ray = CreatePanel(
                    "BRFX_ResultRay_" + i,
                    backRoot,
                    new Vector2(x, 0.18f),
                    new Vector2(Mathf.Min(0.995f, x + 0.008f), 0.91f),
                    panel,
                    i % 2 == 0
                        ? new Color(cyan.r, cyan.g, cyan.b, 0.026f)
                        : new Color(magenta.r, magenta.g, magenta.b, 0.026f));
                ray.localRotation = Quaternion.Euler(0f, 0f, (i - 5.5f) * 2.4f);
            }
        }

        private void CacheCards()
        {
            ticketCard = FindRectByName("Ticket Result");
            statisticsCard = FindRectByName("Statistics");
            replayCard = FindRectByName("Replay Prompt");

            if (ticketCard != null)
            {
                TMP_Text[] ticketTexts = ticketCard.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text t in ticketTexts)
                {
                    if (t == null)
                        continue;
                    if (ticketResult == null || t.fontSize > ticketResult.fontSize)
                        ticketResult = t;
                }
            }

            if (replayCard != null)
            {
                TMP_Text[] replayTexts = replayCard.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text t in replayTexts)
                {
                    if (t == null)
                        continue;
                    string value = (t.text ?? string.Empty).ToUpperInvariant();
                    if (value.Contains("ENTER OR P") || value.Contains("PLAY AGAIN"))
                    {
                        replayPrompt = t;
                        break;
                    }
                }
            }
        }

        private void StyleCards()
        {
            StyleCard(ticketCard, goldPanel, gold, 4f);
            StyleCard(statisticsCard, panel, cyan, 3f);
            StyleCard(replayCard, cyanPanel, cyan, 3f);

            // Important: no second halo/card is created behind Ticket Result.
            // One strong rounded ticket card reads cleaner than the v1.8.1 double-box.
        }

        private void StyleCard(RectTransform rt, Sprite sprite, Color outlineColor, float outlineDistance)
        {
            if (rt == null)
                return;

            Image image = rt.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }

            Outline outline = rt.GetComponent<Outline>();
            if (outline == null)
                outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.62f);
            outline.effectDistance = new Vector2(outlineDistance, -outlineDistance);
            outline.useGraphicAlpha = false;

        }

        private void StyleTexts()
        {
            TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text t in texts)
            {
                if (t == null || IsDecor(t.transform))
                    continue;

                string value = (t.text ?? string.Empty).ToUpperInvariant();
                t.textWrappingMode = TextWrappingModes.NoWrap;

                if (value.Contains("BALLOON RUSH RESULTS") || value.Contains("JACKPOT RESULTS"))
                {
                    SetTextSize(t, 48f, 32f);
                    t.fontStyle |= FontStyles.Bold;
                    t.outlineWidth = Mathf.Max(t.outlineWidth, 0.16f);
                }
                else if (t == ticketResult || (value.Contains("TICKETS") && !value.Contains("JACKPOT")))
                {
                    SetTextSize(t, 72f, 42f);
                    t.fontStyle |= FontStyles.Bold;
                    t.color = Color.white;
                    t.outlineWidth = Mathf.Max(t.outlineWidth, 0.18f);
                    t.outlineColor = new Color32(0, 0, 0, 235);
                }
                else if (value.Contains("FINAL SCORE"))
                {
                    SetTextSize(t, 28f, 19f);
                    t.fontStyle |= FontStyles.Bold;
                    t.color = Color.white;
                }
                else if (value.Contains("HIGHEST COMBO"))
                {
                    SetTextSize(t, 25f, 18f);
                    t.fontStyle |= FontStyles.Bold;
                    t.color = gold;
                }
                else if (value.Contains("PERFECT") && value.Contains("MISS"))
                {
                    SetTextSize(t, 19f, 14f);
                    t.color = new Color(0.82f, 0.94f, 1f);
                }
                else if (value.Contains("GOLDEN BALLOONS"))
                {
                    SetTextSize(t, 18f, 13f);
                    t.color = gold;
                }
                else if (value.Contains("ENTER OR P") || value.Contains("PLAY AGAIN"))
                {
                    SetTextSize(t, 23f, 16f);
                    t.fontStyle |= FontStyles.Bold;
                    t.color = cyan;
                    t.outlineWidth = Mathf.Max(t.outlineWidth, 0.10f);
                }
                else if (value.Contains("NEW HIGH") || value.Contains("NEW TICKET") || value.Contains("TOP SCORE") || value.Contains("POINTS FROM"))
                {
                    SetTextSize(t, 18f, 13f);
                    t.fontStyle |= FontStyles.Bold;
                    t.color = gold;
                }
            }
        }

        private static void SetTextSize(TMP_Text text, float max, float min)
        {
            text.enableAutoSizing = true;
            text.fontSize = max;
            text.fontSizeMax = max;
            text.fontSizeMin = min;
        }

        private void BuildConfettiRoot()
        {
            GameObject front = new GameObject("BRFX_ResultsFrontV182", typeof(RectTransform), typeof(CanvasGroup));
            frontRoot = front.GetComponent<RectTransform>();
            frontRoot.SetParent(canvas.transform, false);
            SetAnchors(frontRoot, Vector2.zero, Vector2.one);
            frontRoot.SetAsLastSibling();

            CanvasGroup group = front.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private IEnumerator WatchTicketCountAndCelebrate()
        {
            Vector3 baseScale = ticketResult.transform.localScale;
            string previous = ticketResult.text;
            float stable = 0f;
            float timeout = 5f;

            while (timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                string current = ticketResult.text;

                if (current != previous)
                {
                    previous = current;
                    stable = 0f;
                    ticketResult.transform.localScale = baseScale * 1.025f;
                }
                else
                {
                    stable += Time.unscaledDeltaTime;
                    ticketResult.transform.localScale = Vector3.Lerp(ticketResult.transform.localScale, baseScale, Time.unscaledDeltaTime * 14f);
                }

                if (stable >= 0.38f && current.ToUpperInvariant().Contains("TICKETS"))
                    break;

                yield return null;
            }

            ticketResult.transform.localScale = baseScale;
            yield return CelebrateFinalTicketValue(baseScale);
        }

        private IEnumerator CelebrateFinalTicketValue(Vector3 baseScale)
        {
            SpawnConfetti(24);

            float elapsed = 0f;
            const float duration = 0.82f;
            Color baseColor = ticketResult.color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float bounce = Mathf.Sin(t * Mathf.PI);
                ticketResult.transform.localScale = baseScale * (1f + bounce * 0.12f);
                ticketResult.color = Color.Lerp(gold, baseColor, t);
                yield return null;
            }

            ticketResult.transform.localScale = baseScale;
            ticketResult.color = baseColor;
        }

        private IEnumerator PulseReplayPrompt()
        {
            Vector3 baseScale = replayPrompt.transform.localScale;
            while (replayPrompt != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.0f);
                replayPrompt.transform.localScale = baseScale * Mathf.Lerp(1f, 1.035f, pulse);
                replayPrompt.color = Color.Lerp(cyan, Color.white, pulse * 0.42f);
                yield return null;
            }
        }

        private void SpawnConfetti(int count)
        {
            if (frontRoot == null)
                return;

            for (int i = 0; i < count; i++)
                StartCoroutine(ConfettiRoutine(i));
        }

        private IEnumerator ConfettiRoutine(int index)
        {
            GameObject go = new GameObject("BRFX_Confetti_" + index, typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(frontRoot, false);
            rt.anchorMin = new Vector2(Random.Range(0.23f, 0.77f), Random.Range(0.57f, 0.78f));
            rt.anchorMax = rt.anchorMin;
            rt.sizeDelta = new Vector2(Random.Range(6f, 13f), Random.Range(11f, 21f));

            Image img = go.GetComponent<Image>();
            img.sprite = sparkSprite;
            img.type = Image.Type.Sliced;
            Color[] colors = { cyan, magenta, gold, green, Color.white };
            Color color = colors[index % colors.Length];
            img.color = color;
            img.raycastTarget = false;

            Vector2 velocity = new Vector2(Random.Range(-145f, 145f), Random.Range(120f, 260f));
            float gravity = Random.Range(250f, 390f);
            float duration = Random.Range(0.82f, 1.30f);
            float elapsed = 0f;
            Vector2 position = Vector2.zero;

            while (elapsed < duration)
            {
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;
                velocity.y -= gravity * dt;
                position += velocity * dt;
                rt.anchoredPosition = position;
                rt.localRotation = Quaternion.Euler(0f, 0f, elapsed * (140f + index * 6f));
                float alpha = 1f - Mathf.Clamp01((elapsed - duration * 0.62f) / (duration * 0.38f));
                img.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            Destroy(go);
        }

        private RectTransform FindRectByName(string objectName)
        {
            RectTransform[] all = canvas.GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rt in all)
            {
                if (rt != null && rt.name == objectName)
                    return rt;
            }
            return null;
        }

        private bool IsDecor(Transform t)
        {
            return (backRoot != null && t.IsChildOf(backRoot)) || (frontRoot != null && t.IsChildOf(frontRoot));
        }

        private void DestroyCanvasChild(string objectName)
        {
            Transform t = canvas.transform.Find(objectName);
            if (t != null)
                Destroy(t.gameObject);
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

        private static void CreateSolid(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            SetAnchors(rt, min, max);
            Image img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private static void SetAnchors(RectTransform rt, Vector2 min, Vector2 max)
        {
            if (rt == null)
                return;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
