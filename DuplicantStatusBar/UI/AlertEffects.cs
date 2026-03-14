using UnityEngine;
using DuplicantStatusBar.Data;

namespace DuplicantStatusBar.UI
{
    enum AlertPattern { Pulse, Heartbeat, Flicker }
    enum GradientShape { Radial, FromBottom, FullWash }

    readonly struct AlertEffect
    {
        public readonly Color BaseColor;
        public readonly float CycleDuration;
        public readonly AlertPattern Pattern;
        public readonly GradientShape Shape;
        public readonly float MinAlpha;
        public readonly float MaxAlpha;

        public AlertEffect(Color baseColor, float cycle, AlertPattern pattern,
            GradientShape shape, float minAlpha, float maxAlpha)
        {
            BaseColor = baseColor;
            CycleDuration = cycle;
            Pattern = pattern;
            Shape = shape;
            MinAlpha = minAlpha;
            MaxAlpha = maxAlpha;
        }
    }

    static class AlertEffects
    {
        private const int TEX_SIZE = 32;

        // Indexed by (int)AlertType — None(0) and Overjoyed(1) are unused placeholders
        private static readonly AlertEffect[] effects = new AlertEffect[]
        {
            default,                                                                                        // 0  None
            default,                                                                                        // 1  Overjoyed (rainbow)
            new AlertEffect(Hex(0xCDDC39), 2.0f, AlertPattern.Pulse,     GradientShape.Radial,     0.30f, 0.55f), // 2  Diseased
            new AlertEffect(Hex(0xFFEB3B), 1.5f, AlertPattern.Pulse,     GradientShape.FromBottom,  0.25f, 0.60f), // 3  BladderUrgent
            new AlertEffect(Hex(0xAA00FF), 1.5f, AlertPattern.Pulse,     GradientShape.Radial,     0.30f, 0.60f), // 4  Overstressed
            new AlertEffect(Hex(0x76FF03), 2.0f, AlertPattern.Pulse,     GradientShape.Radial,     0.35f, 0.60f), // 5  Irradiated
            new AlertEffect(Hex(0xFFA000), 2.5f, AlertPattern.Pulse,     GradientShape.FromBottom,  0.25f, 0.55f), // 6  Starving
            new AlertEffect(Hex(0x90CAF9), 3.0f, AlertPattern.Pulse,     GradientShape.Radial,     0.30f, 0.60f), // 7  Hypothermia
            new AlertEffect(Hex(0xFF6400), 0.8f, AlertPattern.Flicker,   GradientShape.FromBottom,  0.40f, 0.70f), // 8  Scalding
            new AlertEffect(Hex(0xD50000), 1.2f, AlertPattern.Heartbeat, GradientShape.Radial,     0.30f, 0.70f), // 9  LowHP
            new AlertEffect(Hex(0x2979FF), 2.0f, AlertPattern.Pulse,     GradientShape.FromBottom,  0.40f, 0.75f), // 10 Suffocating
            new AlertEffect(Hex(0x795548), 2.0f, AlertPattern.Pulse,     GradientShape.FullWash,   0.40f, 0.55f), // 11 Stuck
            new AlertEffect(Hex(0x9E9E9E), 3.0f, AlertPattern.Pulse,     GradientShape.FullWash,   0.15f, 0.35f), // 12 Idle
            new AlertEffect(Hex(0xE91E63), 0.6f, AlertPattern.Pulse,     GradientShape.Radial,     0.45f, 0.85f), // 13 Incapacitated
        };

        private static readonly Sprite[] spriteCache = new Sprite[14];
        private static readonly Texture2D[] textureCache = new Texture2D[14];

        public static AlertEffect Get(AlertType type)
        {
            int i = (int)type;
            return (i >= 0 && i < effects.Length) ? effects[i] : default;
        }

        public static Sprite GetOverlaySprite(AlertType type)
        {
            int i = (int)type;
            if (i < 0 || i >= spriteCache.Length) return null;
            if (spriteCache[i] != null) return spriteCache[i];

            var fx = effects[i];
            var tex = new Texture2D(TEX_SIZE, TEX_SIZE, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[TEX_SIZE * TEX_SIZE];

            byte r = (byte)(fx.BaseColor.r * 255f);
            byte g = (byte)(fx.BaseColor.g * 255f);
            byte b = (byte)(fx.BaseColor.b * 255f);
            float center = (TEX_SIZE - 1) * 0.5f;

            for (int y = 0; y < TEX_SIZE; y++)
            {
                for (int x = 0; x < TEX_SIZE; x++)
                {
                    float a;
                    switch (fx.Shape)
                    {
                        case GradientShape.Radial:
                            float dx = x - center, dy = y - center;
                            float dist = Mathf.Sqrt(dx * dx + dy * dy) / center;
                            a = Mathf.Clamp01(1f - dist);
                            break;
                        case GradientShape.FromBottom:
                            a = 1f - (float)y / (TEX_SIZE - 1);
                            break;
                        default: // FullWash
                            a = 1f;
                            break;
                    }

                    pixels[y * TEX_SIZE + x] = new Color32(r, g, b, (byte)(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            var sprite = Sprite.Create(tex, new Rect(0, 0, TEX_SIZE, TEX_SIZE),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);

            textureCache[i] = tex;
            spriteCache[i] = sprite;
            return sprite;
        }

        public static float EvaluateAlpha(in AlertEffect fx, float t)
        {
            switch (fx.Pattern)
            {
                case AlertPattern.Pulse:
                    float sin = (Mathf.Sin(t * 2f * Mathf.PI / fx.CycleDuration) + 1f) * 0.5f;
                    return Mathf.Lerp(fx.MinAlpha, fx.MaxAlpha, sin);

                case AlertPattern.Heartbeat:
                    float phase = (t % fx.CycleDuration) / fx.CycleDuration;
                    float v;
                    if (phase < 0.30f)
                        v = Mathf.Sin(phase / 0.30f * Mathf.PI);
                    else if (phase < 0.60f)
                        v = Mathf.Sin((phase - 0.30f) / 0.30f * Mathf.PI) * 0.75f;
                    else
                        v = 0f;
                    return Mathf.Lerp(fx.MinAlpha, fx.MaxAlpha, v);

                case AlertPattern.Flicker:
                    float pulse = (Mathf.Sin(t * 2f * Mathf.PI / fx.CycleDuration) + 1f) * 0.5f;
                    int hash = (Time.frameCount * 7919) & 0xFF;
                    float noise = 0.7f + 0.3f * (hash / 255f);
                    return Mathf.Lerp(fx.MinAlpha, fx.MaxAlpha, pulse * noise);

                default:
                    return fx.MinAlpha;
            }
        }

        public static void Cleanup()
        {
            for (int i = 0; i < spriteCache.Length; i++)
            {
                if (spriteCache[i] != null)
                {
                    Object.Destroy(spriteCache[i]);
                    spriteCache[i] = null;
                }
                if (textureCache[i] != null)
                {
                    Object.Destroy(textureCache[i]);
                    textureCache[i] = null;
                }
            }
        }

        private static Color Hex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f);
        }
    }
}
