using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BalloonRush.Gameplay
{
    public sealed class BalloonPool : MonoBehaviour
    {
        [SerializeField] private Balloon balloonPrefab;
        [SerializeField, Min(8)] private int prewarmCount = 48;

        private readonly Queue<Balloon> available = new Queue<Balloon>();
        private readonly List<Balloon> all = new List<Balloon>();
        private bool initialized;
        private bool warnedExhausted;

        public int AvailableCount => available.Count;
        public int TotalCount => all.Count;

        public void Configure(Balloon prefab, int count)
        {
            balloonPrefab = prefab;
            prewarmCount = Mathf.Max(8, count);
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (balloonPrefab == null)
            {
                balloonPrefab = CreateRuntimeTemplate();
            }

            for (int i = 0; i < prewarmCount; i++)
            {
                Balloon balloon = Instantiate(balloonPrefab, transform);
                balloon.name = $"Balloon_{i:00}";
                balloon.gameObject.SetActive(false);
                all.Add(balloon);
                available.Enqueue(balloon);
            }

            if (balloonPrefab.transform.parent == transform && balloonPrefab.name == "RuntimeBalloonTemplate")
            {
                balloonPrefab.gameObject.SetActive(false);
            }

            initialized = true;
        }

        public Balloon Acquire()
        {
            if (!initialized)
            {
                Initialize();
            }

            if (available.Count == 0)
            {
                if (!warnedExhausted)
                {
                    warnedExhausted = true;
                    Debug.LogWarning("Balloon pool exhausted. Spawn skipped instead of allocating during gameplay.");
                }
                return null;
            }

            return available.Dequeue();
        }

        public void Release(Balloon balloon)
        {
            if (balloon == null || available.Contains(balloon))
            {
                return;
            }

            balloon.transform.SetParent(transform, false);
            balloon.gameObject.SetActive(false);
            available.Enqueue(balloon);
        }

        private Balloon CreateRuntimeTemplate()
        {
            GameObject root = new GameObject("RuntimeBalloonTemplate");
            root.transform.SetParent(transform, false);
            root.SetActive(false);

            Balloon balloon = root.AddComponent<Balloon>();
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            GameObject glowObject = new GameObject("Glow");
            glowObject.transform.SetParent(visual.transform, false);
            SpriteRenderer glow = glowObject.AddComponent<SpriteRenderer>();
            glow.sprite = RuntimeSpriteLibrary.RadialGlowSprite;
            glow.sortingOrder = 4;
            glowObject.transform.localScale = Vector3.one * 2.1f;

            GameObject bodyObject = new GameObject("Body");
            bodyObject.transform.SetParent(visual.transform, false);
            SpriteRenderer body = bodyObject.AddComponent<SpriteRenderer>();
            body.sprite = RuntimeSpriteLibrary.BalloonSprite;
            body.sortingOrder = 5;
            bodyObject.transform.localScale = new Vector3(1.45f, 1.45f, 1f);

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(visual.transform, false);
            TextMeshPro icon = iconObject.AddComponent<TextMeshPro>();
            icon.alignment = TextAlignmentOptions.Center;
            icon.fontSize = 5.5f;
            icon.fontStyle = FontStyles.Bold;
            icon.sortingOrder = 6;
            icon.rectTransform.sizeDelta = new Vector2(2f, 1f);
            iconObject.transform.localPosition = new Vector3(0f, 0.12f, -0.1f);

            GameObject stringObject = new GameObject("String");
            stringObject.transform.SetParent(visual.transform, false);
            LineRenderer stringRenderer = stringObject.AddComponent<LineRenderer>();
            stringRenderer.useWorldSpace = false;
            stringRenderer.widthMultiplier = 0.025f;
            stringRenderer.material = new Material(Shader.Find("Sprites/Default"));
            stringRenderer.sortingOrder = 3;

            balloon.ConfigureVisuals(visual.transform, body, glow, icon, stringRenderer);
            return balloon;
        }
    }
}
