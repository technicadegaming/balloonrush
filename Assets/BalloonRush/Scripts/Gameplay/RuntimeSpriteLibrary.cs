using UnityEngine;

namespace BalloonRush.Gameplay
{
    public static class RuntimeSpriteLibrary
    {
        private static Sprite balloonSprite;
        private static Sprite solidSprite;
        private static Sprite radialGlowSprite;

        public static Sprite BalloonSprite => balloonSprite != null ? balloonSprite : (balloonSprite = CreateBalloonSprite());
        public static Sprite SolidSprite => solidSprite != null ? solidSprite : (solidSprite = CreateSolidSprite());
        public static Sprite RadialGlowSprite => radialGlowSprite != null ? radialGlowSprite : (radialGlowSprite = CreateRadialGlowSprite());

        private static Sprite CreateBalloonSprite()
        {
            const int width = 128;
            const int height = 160;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Runtime Balloon Sprite",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            Color[] pixels = new Color[width * height];
            Vector2 center = new Vector2(width * 0.5f, height * 0.46f);
            Vector2 radius = new Vector2(width * 0.43f, height * 0.40f);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float nx = (x - center.x) / radius.x;
                    float ny = (y - center.y) / radius.y;
                    float distance = nx * nx + ny * ny;
                    Color color = Color.clear;
                    if (distance <= 1f)
                    {
                        float edge = Mathf.Clamp01((1f - distance) * 8f);
                        float highlightDistance = Vector2.Distance(new Vector2(nx, ny), new Vector2(-0.35f, 0.35f));
                        float highlight = Mathf.Clamp01(1f - highlightDistance * 3.8f);
                        float shade = Mathf.Lerp(0.68f, 1f, edge);
                        color = new Color(shade + highlight * 0.35f, shade + highlight * 0.35f, shade + highlight * 0.35f, Mathf.Clamp01(edge * 2f));
                    }

                    bool knot = y < 14 && Mathf.Abs(x - width * 0.5f) < (14 - y * 0.55f);
                    if (knot)
                    {
                        color = Color.white;
                    }

                    pixels[y * width + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.45f), 100f);
        }

        private static Sprite CreateSolidSprite()
        {
            Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                name = "Runtime Solid Sprite",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private static Sprite CreateRadialGlowSprite()
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Runtime Radial Glow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            Color[] pixels = new Color[size * size];
            Vector2 center = Vector2.one * (size - 1) * 0.5f;
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.4f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
