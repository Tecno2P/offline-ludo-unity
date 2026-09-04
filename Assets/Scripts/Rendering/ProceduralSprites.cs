using UnityEngine;

namespace LudoGame.Rendering
{
    // Draws real textures pixel-by-pixel at runtime. No external art files - every sprite the
    // game uses is generated here with actual math (distance fields, not placeholders).
    public static class ProceduralSprites
    {
        private const float PixelsPerUnit = 100f;

        public static Sprite Circle(int size, Color fill, Color outline, float outlineWidth = 0.06f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float radius = size / 2f;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / radius;
                    Color pixel;
                    if (dist > 1f) pixel = new Color(0, 0, 0, 0);
                    else if (dist > 1f - outlineWidth) pixel = outline;
                    else pixel = fill;

                    // Cheap anti-aliasing on the outer edge so circles don't look jagged.
                    if (dist > 1f - 0.02f && dist <= 1f)
                        pixel.a *= Mathf.Clamp01((1f - dist) / 0.02f);

                    tex.SetPixel(x, y, pixel);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        public static Sprite RoundedSquare(int size, Color fill, Color outline, float cornerRadiusFrac = 0.15f, float outlineWidth = 0.04f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = size * cornerRadiusFrac;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float sd = RoundedBoxSDF(x + 0.5f, y + 0.5f, size, size, r);
                    Color pixel;
                    if (sd > 0f) pixel = new Color(0, 0, 0, 0);
                    else if (sd > -size * outlineWidth) pixel = outline;
                    else pixel = fill;

                    if (sd > -1.5f && sd <= 0f) pixel.a *= Mathf.Clamp01(-sd / 1.5f);
                    tex.SetPixel(x, y, pixel);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        public static Sprite Star(int size, Color fill, Color outline)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float outerR = size * 0.48f, innerR = size * 0.2f;
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float rad = (i % 2 == 0) ? outerR : innerR;
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * rad;
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    bool inside = PointInPolygon(p, points);
                    tex.SetPixel(x, y, inside ? fill : new Color(0, 0, 0, 0));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        // Classic 1-6 pip arrangement for a dice face, rendered as real dots on a rounded square.
        public static Sprite DiceFace(int size, int value, Color faceColor, Color pipColor)
        {
            var baseSprite = RoundedSquare(size, faceColor, new Color(0, 0, 0, 0.25f), 0.2f, 0.03f);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            tex.SetPixels(baseSprite.texture.GetPixels());

            float pipRadius = size * 0.09f;
            Vector2 c = new Vector2(size / 2f, size / 2f);
            float off = size * 0.24f;

            Vector2[] positions;
            switch (value)
            {
                case 1: positions = new[] { c }; break;
                case 2: positions = new[] { c + new Vector2(-off, off), c + new Vector2(off, -off) }; break;
                case 3: positions = new[] { c + new Vector2(-off, off), c, c + new Vector2(off, -off) }; break;
                case 4: positions = new[] { c + new Vector2(-off, off), c + new Vector2(off, off), c + new Vector2(-off, -off), c + new Vector2(off, -off) }; break;
                case 5: positions = new[] { c + new Vector2(-off, off), c + new Vector2(off, off), c, c + new Vector2(-off, -off), c + new Vector2(off, -off) }; break;
                case 6:
                default:
                    positions = new[] {
                        c + new Vector2(-off, off), c + new Vector2(off, off),
                        c + new Vector2(-off, 0), c + new Vector2(off, 0),
                        c + new Vector2(-off, -off), c + new Vector2(off, -off)
                    };
                    break;
            }

            foreach (var pos in positions)
                StampCircle(tex, pos, pipRadius, pipColor);

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        }

        private static void StampCircle(Texture2D tex, Vector2 center, float radius, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius - 1));
            int maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(center.x + radius + 1));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius - 1));
            int maxY = Mathf.Min(tex.height - 1, Mathf.CeilToInt(center.y + radius + 1));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01((radius - dist) / 1.2f);
                        tex.SetPixel(x, y, Color.Lerp(tex.GetPixel(x, y), color, alpha));
                    }
                }
            }
        }

        private static float RoundedBoxSDF(float x, float y, float w, float h, float r)
        {
            float dx = Mathf.Abs(x - w / 2f) - (w / 2f - r);
            float dy = Mathf.Abs(y - h / 2f) - (h / 2f - r);
            float ax = Mathf.Max(dx, 0f), ay = Mathf.Max(dy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;
        }

        private static bool PointInPolygon(Vector2 p, Vector2[] poly)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                    inside = !inside;
            }
            return inside;
        }
    }
}
