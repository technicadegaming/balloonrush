using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BalloonRush.Effects
{
    public sealed class FloatingTextPool : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textPrefab;
        [SerializeField, Min(4)] private int prewarmCount = 24;

        private readonly Queue<TextMeshPro> available = new Queue<TextMeshPro>();
        private readonly HashSet<TextMeshPro> active = new HashSet<TextMeshPro>();
        private bool initialized;

        public int AvailableCount => available.Count;
        public int ActiveCount => active.Count;

        public void Configure(TextMeshPro prefab, int count)
        {
            textPrefab = prefab;
            prewarmCount = Mathf.Max(4, count);
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            if (textPrefab == null)
            {
                textPrefab = CreateRuntimeTemplate();
            }

            for (int i = 0; i < prewarmCount; i++)
            {
                TextMeshPro item = Instantiate(textPrefab, transform);
                item.name = $"FloatingText_{i:00}";
                item.gameObject.SetActive(false);
                available.Enqueue(item);
            }

            if (textPrefab.transform.parent == transform)
            {
                textPrefab.gameObject.SetActive(false);
            }
            initialized = true;
        }

        public void Show(string text, Vector3 worldPosition, Color color, float duration = 0.75f, float riseDistance = 1.0f)
        {
            if (!initialized)
            {
                Initialize();
            }

            if (available.Count == 0)
            {
                return;
            }

            TextMeshPro item = available.Dequeue();
            active.Add(item);
            item.text = text;
            item.color = color;
            item.alpha = 1f;
            item.transform.position = worldPosition;
            item.transform.localScale = Vector3.one;
            item.gameObject.SetActive(true);
            StartCoroutine(Animate(item, duration, riseDistance));
        }

        public void Clear()
        {
            StopAllCoroutines();
            foreach (TextMeshPro item in active)
            {
                item.gameObject.SetActive(false);
                available.Enqueue(item);
            }
            active.Clear();
        }

        private IEnumerator Animate(TextMeshPro item, float duration, float riseDistance)
        {
            Vector3 start = item.transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                item.transform.position = start + Vector3.up * (riseDistance * t);
                item.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.2f, Mathf.Sin(t * Mathf.PI));
                item.alpha = 1f - t;
                yield return null;
            }

            active.Remove(item);
            item.gameObject.SetActive(false);
            available.Enqueue(item);
        }

        private TextMeshPro CreateRuntimeTemplate()
        {
            GameObject templateObject = new GameObject("RuntimeFloatingTextTemplate");
            templateObject.transform.SetParent(transform, false);
            TextMeshPro text = templateObject.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 4.5f;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = 2.5f;
            text.fontSizeMax = 5f;
            text.rectTransform.sizeDelta = new Vector2(3f, 1.2f);
            text.sortingOrder = 50;
            templateObject.SetActive(false);
            return text;
        }
    }
}
