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
        // Indexed by (int)AlertType — None(0) and Overjoyed(1) are unused placeholders
        private static readonly AlertEffect[] effects = new AlertEffect[]
        {
            default,                                                                                                    // 0  None
            default,                                                                                                    // 1  Overjoyed (rainbow)
            new AlertEffect(H(ColorUtil.AlertDiseased),      2.0f, AlertPattern.Pulse,     GradientShape.Radial,     0.30f, 0.55f), // 2  Diseased
            new AlertEffect(H(ColorUtil.AlertBladder),       1.5f, AlertPattern.Pulse,     GradientShape.FromBottom,  0.25f, 0.60f), // 3  BladderUrgent
            new AlertEffect(H(ColorUtil.AlertOverstressed),  1.5f, AlertPattern.Pulse,     GradientShape.Radial,     0.30f, 0.60f), // 4  Overstressed
            new AlertEffect(H(ColorUtil.AlertIrradiated),    2.0f, AlertPattern.Pulse,     GradientShape.Radial,     0.35f, 0.60f), // 5  Irradiated
            new AlertEffect(H(ColorUtil.AlertStarving),      2.5f, AlertPattern.Pulse,     GradientShape.FromBottom,  0.25f, 0.55f), // 6  Starving
            new AlertEffect(H(ColorUtil.AlertHypothermia),   3.0f, AlertPattern.Pulse,     GradientShape.Radial,     0.30f, 0.60f), // 7  Hypothermia
            new AlertEffect(H(ColorUtil.AlertScalding),      0.8f, AlertPattern.Flicker,   GradientShape.FromBottom,  0.40f, 0.70f), // 8  Scalding
            new AlertEffect(H(ColorUtil.AlertLowHP),         1.2f, AlertPattern.Heartbeat, GradientShape.Radial,     0.30f, 0.70f), // 9  LowHP
            new AlertEffect(H(ColorUtil.AlertSuffocating),   2.0f, AlertPattern.Pulse,     GradientShape.FromBottom,  0.40f, 0.75f), // 10 Suffocating
            new AlertEffect(H(ColorUtil.AlertStuck),         2.0f, AlertPattern.Pulse,     GradientShape.FullWash,   0.40f, 0.55f), // 11 Stuck
            new AlertEffect(H(ColorUtil.AlertIdle),          3.0f, AlertPattern.Pulse,     GradientShape.FullWash,   0.15f, 0.35f), // 12 Idle
            new AlertEffect(H(ColorUtil.AlertIncapacitated), 0.6f, AlertPattern.Pulse,     GradientShape.Radial,     0.45f, 0.85f), // 13 Incapacitated
        };

        public static AlertEffect Get(AlertType type)
        {
            int i = (int)type;
            return (i >= 0 && i < effects.Length) ? effects[i] : default;
        }

        /// <summary>Returns the canonical color for an alert type (single source of truth).</summary>
        public static Color GetColor(AlertType type)
        {
            var fx = Get(type);
            return fx.BaseColor != default ? fx.BaseColor : Color.clear;
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

        private static Color H(int rgb) => ColorUtil.Hex(rgb);
    }
}
