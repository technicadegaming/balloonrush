using System;
using System.Collections;
using BalloonRush.Core;
using BalloonRush.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalloonRush.UI
{
    /// <summary>
    /// v1.8.7 hit-pop readability pass.
    ///
    /// The gameplay balloon still performs its normal pooled pop/release.
    /// This creates a short-lived visual "ghost" of the actual balloon body so
    /// the player clearly sees the balloon itself burst rather than simply vanish.
    /// </summary>
    [DefaultExecutionOrder(300)]
    public sealed class BalloonRushHitPopV187 : MonoBehaviour
    {
        private Sprite shardSprite;

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
            if (!string.Equals(
                    SceneManager.GetActiveScene().name,
                    "MainGame",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Canvas[] canvases =
                FindObjectsByType<Canvas>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (Canvas canvas in canvases)
            {
                if (canvas == null)
                    continue;

                if (canvas.name.IndexOf(
                        "Gameplay",
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (canvas.GetComponent<BalloonRushHitPopV187>() == null)
                {
                    canvas.gameObject.AddComponent<
                        BalloonRushHitPopV187>();
                }

                return;
            }
        }

        private void Awake()
        {
            shardSprite =
                CreateSoftCircleSprite();
        }

        private void Start()
        {
            GameEvents.BalloonPopped -= HandleBalloonPopped;
            GameEvents.BalloonPopped += HandleBalloonPopped;
        }

        private void OnDestroy()
        {
            GameEvents.BalloonPopped -= HandleBalloonPopped;
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

            SpriteRenderer body =
                FindBodyRenderer(balloon);

            if (body == null ||
                body.sprite == null)
            {
                return;
            }

            SpawnBalloonPop(
                body,
                rating);
        }

        private void SpawnBalloonPop(
            SpriteRenderer source,
            TimingRating rating)
        {
            GameObject root =
                new GameObject(
                    "BR187_BalloonPop");

            root.transform.position =
                source.transform.position;

            GameObject ghostObject =
                new GameObject(
                    "BalloonGhost",
                    typeof(SpriteRenderer));

            ghostObject.transform.SetParent(
                root.transform,
                false);

            SpriteRenderer ghost =
                ghostObject.GetComponent<SpriteRenderer>();

            CopyRenderer(source, ghost);

            ghost.sortingOrder =
                source.sortingOrder + 30;

            Vector3 originalScale =
                source.transform.lossyScale;

            ghostObject.transform.localScale =
                originalScale;

            GameObject flashObject =
                new GameObject(
                    "WhiteFlash",
                    typeof(SpriteRenderer));

            flashObject.transform.SetParent(
                root.transform,
                false);

            SpriteRenderer flash =
                flashObject.GetComponent<SpriteRenderer>();

            CopyRenderer(source, flash);

            flash.sortingOrder =
                source.sortingOrder + 31;

            flash.color =
                new Color(
                    1f,
                    1f,
                    1f,
                    0.78f);

            flashObject.transform.localScale =
                originalScale * 1.03f;

            int shardCount =
                rating == TimingRating.Perfect
                    ? 14
                    : rating == TimingRating.Great
                        ? 11
                        : 9;

            SpriteRenderer[] shards =
                new SpriteRenderer[shardCount];

            Vector2[] directions =
                new Vector2[shardCount];

            float[] speeds =
                new float[shardCount];

            Color baseColor =
                source.color;

            for (int i = 0; i < shardCount; i++)
            {
                float angle =
                    Mathf.PI * 2f *
                    (i / (float)shardCount) +
                    (i % 2) * 0.14f;

                directions[i] =
                    new Vector2(
                        Mathf.Cos(angle),
                        Mathf.Sin(angle));

                speeds[i] =
                    1.0f +
                    (i % 4) * 0.20f;

                GameObject shardObject =
                    new GameObject(
                        "Shard_" + i,
                        typeof(SpriteRenderer));

                shardObject.transform.SetParent(
                    root.transform,
                    false);

                SpriteRenderer shard =
                    shardObject.GetComponent<SpriteRenderer>();

                shard.sprite =
                    shardSprite;

                shard.sortingLayerID =
                    source.sortingLayerID;

                shard.sortingOrder =
                    source.sortingOrder + 29;

                shard.color =
                    i % 3 == 0
                        ? Color.white
                        : baseColor;

                shardObject.transform.localScale =
                    Vector3.one *
                    (0.13f +
                     (i % 3) * 0.025f);

                shards[i] = shard;
            }

            StartCoroutine(
                PopRoutine(
                    root,
                    ghost,
                    flash,
                    shards,
                    directions,
                    speeds,
                    originalScale,
                    rating));
        }

        private IEnumerator PopRoutine(
            GameObject root,
            SpriteRenderer ghost,
            SpriteRenderer flash,
            SpriteRenderer[] shards,
            Vector2[] directions,
            float[] speeds,
            Vector3 originalScale,
            TimingRating rating)
        {
            const float swellDuration = 0.055f;
            const float burstDuration = 0.165f;

            float strength =
                rating == TimingRating.Perfect
                    ? 1.22f
                    : rating == TimingRating.Great
                        ? 1.12f
                        : 1f;

            float elapsed = 0f;

            while (elapsed < swellDuration &&
                   root != null)
            {
                elapsed += Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        swellDuration);

                float scale =
                    Mathf.Lerp(
                        1f,
                        1.32f * strength,
                        t);

                if (ghost != null)
                {
                    ghost.transform.localScale =
                        new Vector3(
                            originalScale.x * scale,
                            originalScale.y * scale,
                            originalScale.z);
                }

                if (flash != null)
                {
                    Color c = flash.color;
                    c.a =
                        Mathf.Lerp(
                            0.78f,
                            0.20f,
                            t);
                    flash.color = c;

                    flash.transform.localScale =
                        originalScale *
                        Mathf.Lerp(
                            1.03f,
                            1.38f,
                            t);
                }

                yield return null;
            }

            elapsed = 0f;

            while (elapsed < burstDuration &&
                   root != null)
            {
                elapsed += Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        burstDuration);

                if (ghost != null)
                {
                    float x =
                        Mathf.Lerp(
                            1.32f * strength,
                            1.62f * strength,
                            t);

                    float y =
                        Mathf.Lerp(
                            1.32f * strength,
                            0.035f,
                            t);

                    ghost.transform.localScale =
                        new Vector3(
                            originalScale.x * x,
                            originalScale.y * y,
                            originalScale.z);

                    ghost.transform.localRotation =
                        Quaternion.Euler(
                            0f,
                            0f,
                            Mathf.Lerp(
                                0f,
                                12f,
                                t));

                    Color c = ghost.color;
                    c.a = 1f - t;
                    ghost.color = c;
                }

                if (flash != null)
                {
                    Color c = flash.color;
                    c.a =
                        Mathf.Max(
                            0f,
                            0.20f *
                            (1f - t * 2.2f));

                    flash.color = c;
                }

                for (int i = 0; i < shards.Length; i++)
                {
                    SpriteRenderer shard =
                        shards[i];

                    if (shard == null)
                        continue;

                    Vector2 direction =
                        directions[i];

                    float distance =
                        speeds[i] *
                        strength *
                        Mathf.Lerp(
                            0f,
                            1.20f,
                            t);

                    shard.transform.localPosition =
                        new Vector3(
                            direction.x * distance,
                            direction.y * distance +
                            0.12f * t,
                            0f);

                    float shardScale =
                        Mathf.Lerp(
                            0.16f,
                            0.025f,
                            t);

                    shard.transform.localScale =
                        Vector3.one *
                        shardScale;

                    Color sc = shard.color;
                    sc.a = 1f - t;
                    shard.color = sc;
                }

                yield return null;
            }

            if (root != null)
                Destroy(root);
        }

        private static SpriteRenderer FindBodyRenderer(
            Balloon balloon)
        {
            SpriteRenderer[] renderers =
                balloon.GetComponentsInChildren<
                    SpriteRenderer>(true);

            SpriteRenderer fallback = null;

            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null ||
                    renderer.sprite == null)
                {
                    continue;
                }

                string name =
                    renderer.name.ToLowerInvariant();

                if (name.Contains("body"))
                    return renderer;

                if (name.Contains("glow") ||
                    name.Contains("gloss") ||
                    name.Contains("shadow") ||
                    name.Contains("aura"))
                {
                    continue;
                }

                if (fallback == null)
                    fallback = renderer;
            }

            return fallback;
        }

        private static void CopyRenderer(
            SpriteRenderer source,
            SpriteRenderer target)
        {
            target.sprite =
                source.sprite;

            target.color =
                source.color;

            target.sortingLayerID =
                source.sortingLayerID;

            target.sortingOrder =
                source.sortingOrder;

            target.flipX =
                source.flipX;

            target.flipY =
                source.flipY;
        }

        private static Sprite CreateSoftCircleSprite()
        {
            const int size = 64;

            float center =
                (size - 1) * 0.5f;

            float radius =
                size * 0.46f;

            Texture2D texture =
                new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false);

            texture.name =
                "BR187_PopShard";

            texture.filterMode =
                FilterMode.Bilinear;

            texture.wrapMode =
                TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;

                    float distance =
                        Mathf.Sqrt(
                            dx * dx +
                            dy * dy);

                    float normalized =
                        Mathf.Clamp01(
                            distance /
                            radius);

                    float alpha =
                        normalized >= 1f
                            ? 0f
                            : 1f -
                              Mathf.SmoothStep(
                                  0.62f,
                                  1f,
                                  normalized);

                    texture.SetPixel(
                        x,
                        y,
                        new Color(
                            1f,
                            1f,
                            1f,
                            alpha));
                }
            }

            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(
                    0,
                    0,
                    size,
                    size),
                new Vector2(
                    0.5f,
                    0.5f),
                64f);
        }
    }
}
