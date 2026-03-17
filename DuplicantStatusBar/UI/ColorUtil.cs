using UnityEngine;

namespace DuplicantStatusBar.UI
{
    /// <summary>
    /// Single source of truth for all mod colors. Every hardcoded color in the mod
    /// should be defined here so palette changes and future theming are centralized.
    /// </summary>
    internal static class ColorUtil
    {
        // ── Helpers ────────────────────────────────────────

        public static Color Hex(int rgb) =>
            new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f);

        /// <summary>Convert int RGB to HTML hex string for rich text tags.</summary>
        public static string ToHtml(int rgb) =>
            $"{(rgb >> 16) & 0xFF:X2}{(rgb >> 8) & 0xFF:X2}{rgb & 0xFF:X2}";

        public static Color WithAlpha(Color c, float a) =>
            new Color(c.r, c.g, c.b, a);

        // ── Stat Value Gradient (tooltip rich text) ────────

        public const int Green  = 0x4ADE80;
        public const int Lime   = 0x84CC16;
        public const int Amber  = 0xFBBF24;
        public const int Gold   = 0xFCD34D;
        public const int Orange = 0xF97316;
        public const int Red    = 0xEF4444;
        public const int Blue   = 0x60A5FA;
        public const int Yellow = 0xFFEB3B;

        // ── Alert Badge ────────────────────────────────────

        public const int AlertDiseased      = 0xAB47BC;
        public const int AlertBladder       = 0xFFEB3B;
        public const int AlertOverstressed  = 0xEC407A;
        public const int AlertIrradiated    = 0x00E676;
        public const int AlertStarving      = 0xFF9800;
        public const int AlertHypothermia   = 0x00ACC1;
        public const int AlertScalding      = 0xFF5722;
        public const int AlertLowHP         = 0xD50000;
        public const int AlertSuffocating   = 0x2979FF;
        public const int AlertStuck         = 0x7E57C2;
        public const int AlertIdle          = 0x9CA3AF;
        public const int AlertIncapacitated = 0xFF00DD;
        public const int AlertLowBattery    = 0x00E5FF;
        public const int AlertLowGearOil    = 0xBCAAA4;
        public const int AlertGrindingGears = 0xFFAB00;

        // ── UI Chrome ──────────────────────────────────────

        public static readonly Color CardBg      = Hex(0x1E2A38);
        public static readonly Color PanelBg     = Hex(0x2A3545);
        public static readonly Color HeaderBg    = Hex(0x6B3350);
        public static readonly Color TextPrimary = Hex(0xE8EDF2);
        public static readonly Color TextMuted   = new Color(0.9f, 0.9f, 0.9f);
        public static readonly Color DarkBase    = new Color(0.08f, 0.10f, 0.14f);
        public static readonly Color DamageBase  = new Color(0.15f, 0f, 0f);
        public static readonly Color ScrollHandle = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        public static readonly Color BevelLight  = new Color(1f, 1f, 1f, 0.15f);
        public static readonly Color BevelShadow = new Color(0f, 0f, 0f, 0.25f);

        // ── Health Bar Fill (portrait widget) ──────────────

        public static readonly Color HealthFill   = new Color(0.298f, 0.686f, 0.314f, 0.55f);
        public static readonly Color HealthGreen  = new Color(0.298f, 0.780f, 0.314f, 0.78f);
        public static readonly Color HealthYellow = new Color(1.000f, 0.835f, 0.180f, 0.84f);
        public static readonly Color HealthOrange = new Color(1.000f, 0.400f, 0.120f, 0.90f);
        public static readonly Color HealthRed    = new Color(0.960f, 0.180f, 0.180f, 0.95f);
    }
}
