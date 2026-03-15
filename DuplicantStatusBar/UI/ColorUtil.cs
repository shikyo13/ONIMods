using UnityEngine;

namespace DuplicantStatusBar.UI
{
    /// <summary>Shared color helpers used by AlertEffects, DupePortraitWidget, and DupeTooltip.</summary>
    internal static class ColorUtil
    {
        public static Color Hex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f);
        }

        /// <summary>Convert int RGB to HTML hex string for rich text tags.</summary>
        public static string ToHtml(int rgb) =>
            $"{(rgb >> 16) & 0xFF:X2}{(rgb >> 8) & 0xFF:X2}{rgb & 0xFF:X2}";

        // Tooltip stat gradient palette (single source of truth)
        public const int Green  = 0x4ADE80;
        public const int Amber  = 0xFBBF24;
        public const int Gold   = 0xFCD34D;
        public const int Orange = 0xF97316;
        public const int Red    = 0xEF4444;
        public const int Blue   = 0x60A5FA;
        public const int Yellow = 0xFFEB3B;
    }
}
