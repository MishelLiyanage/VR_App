using UnityEngine;

namespace CSIVR.Interface
{
    /// <summary>Procedurally generated UI shapes (rounded cards, circles, icons). No image assets, nothing to credit.</summary>
    public static class UISprites
    {
        const int Size = 96;
        const int Radius = 28;

        static Sprite s_Rounded, s_Circle, s_Check, s_Cross, s_Dot;

        /// <summary>9-sliced rounded rectangle. Use with Image.Type.Sliced.</summary>
        public static Sprite Rounded => s_Rounded != null ? s_Rounded : (s_Rounded = MakeRounded());
        public static Sprite Circle => s_Circle != null ? s_Circle : (s_Circle = MakeCircle());
        public static Sprite Check => s_Check != null ? s_Check : (s_Check = MakeCheck());
        public static Sprite Cross => s_Cross != null ? s_Cross : (s_Cross = MakeCross());

        static Texture2D NewTexture()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            return tex;
        }

        static float Smooth(float edgeDistance) => Mathf.Clamp01(edgeDistance + 0.5f);

        static Sprite MakeRounded()
        {
            var tex = NewTexture();
            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, Radius, Size - Radius);
                    float cy = Mathf.Clamp(y + 0.5f, Radius, Size - Radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    px[y * Size + x] = new Color32(255, 255, 255, (byte)(Smooth(Radius - d) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(Radius, Radius, Radius, Radius));
        }

        static Sprite MakeCircle()
        {
            var tex = NewTexture();
            var px = new Color32[Size * Size];
            float r = Size * 0.5f - 1f;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(Size * 0.5f, Size * 0.5f));
                    px[y * Size + x] = new Color32(255, 255, 255, (byte)(Smooth(r - d) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeCheck()
        {
            // Two thick strokes forming a tick mark.
            return MakeStrokes(new[]
            {
                (new Vector2(0.24f, 0.52f), new Vector2(0.43f, 0.32f)),
                (new Vector2(0.43f, 0.32f), new Vector2(0.78f, 0.72f)),
            }, 0.085f);
        }

        static Sprite MakeCross()
        {
            return MakeStrokes(new[]
            {
                (new Vector2(0.28f, 0.28f), new Vector2(0.72f, 0.72f)),
                (new Vector2(0.28f, 0.72f), new Vector2(0.72f, 0.28f)),
            }, 0.08f);
        }

        static Sprite MakeStrokes((Vector2 a, Vector2 b)[] strokes, float halfWidth)
        {
            var tex = NewTexture();
            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size, (y + 0.5f) / Size);
                    float best = float.MaxValue;
                    foreach (var (a, b) in strokes)
                        best = Mathf.Min(best, DistanceToSegment(p, a, b));
                    px[y * Size + x] = new Color32(255, 255, 255, (byte)(Smooth((halfWidth - best) * Size) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        }

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
