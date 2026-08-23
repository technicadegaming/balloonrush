using UnityEngine;

namespace BalloonRush.UI
{
    public static class RoundedSpriteFactory
    {
        public static Sprite CreateRoundedPanelSprite(
            Color fillColor,
            Color borderColor,
            int textureSize = 64,
            int radius = 14,
            int border = 4,
            int slice = 16)
        {
            var tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            tex.name = "RoundedPanel_" + fillColor;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            int w = textureSize;
            int h = textureSize;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float outer = DistanceToRoundedRectEdge(x, y, w, h, radius);
                    Color c;

                    if (outer > 0.5f)
                    {
                        c = new Color(0f, 0f, 0f, 0f);
                    }
                    else
                    {
                        float inner = DistanceToRoundedRectEdge(
                            x, y,
                            w - border * 2,
                            h - border * 2,
                            Mathf.Max(1, radius - border),
                            border, border);

                        c = inner > 0.5f ? borderColor : fillColor;
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();

            return Sprite.Create(
                tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(slice, slice, slice, slice)
            );
        }

        private static float DistanceToRoundedRectEdge(
            int px,
            int py,
            int w,
            int h,
            int radius,
            int offsetX = 0,
            int offsetY = 0)
        {
            float x = px - offsetX;
            float y = py - offsetY;
            float rw = w;
            float rh = h;

            if (x < 0f || y < 0f || x > rw || y > rh)
                return 999f;

            float left = radius;
            float right = rw - radius;
            float bottom = radius;
            float top = rh - radius;

            if (x >= left && x <= right) return 0f;
            if (y >= bottom && y <= top) return 0f;

            Vector2 p = new Vector2(x, y);
            Vector2 closest;

            if (x < left && y > top) closest = new Vector2(left, top);
            else if (x > right && y > top) closest = new Vector2(right, top);
            else if (x < left && y < bottom) closest = new Vector2(left, bottom);
            else if (x > right && y < bottom) closest = new Vector2(right, bottom);
            else if (x < left) closest = new Vector2(left, Mathf.Clamp(y, bottom, top));
            else if (x > right) closest = new Vector2(right, Mathf.Clamp(y, bottom, top));
            else if (y < bottom) closest = new Vector2(Mathf.Clamp(x, left, right), bottom);
            else closest = new Vector2(Mathf.Clamp(x, left, right), top);

            return Vector2.Distance(p, closest) - radius;
        }
    }
}
